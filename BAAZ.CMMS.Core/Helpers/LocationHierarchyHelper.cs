using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Helpers;

/// <summary>Утилиты для древовидного справочника локаций.</summary>
public static class LocationHierarchyHelper
{
    public const string DefaultPathSeparator = " → ";

    public static IReadOnlyDictionary<Guid, string> BuildFullPaths(
        IReadOnlyList<LocationListItem> locations,
        string separator = DefaultPathSeparator)
    {
        var byId = locations.ToDictionary(l => l.Id);
        var cache = new Dictionary<Guid, string>();

        string Resolve(Guid id)
        {
            if (cache.TryGetValue(id, out var cached))
                return cached;

            if (!byId.TryGetValue(id, out var node))
                return id.ToString();

            if (node.ParentId is null || !byId.ContainsKey(node.ParentId.Value))
            {
                cache[id] = node.Name;
                return node.Name;
            }

            var path = $"{Resolve(node.ParentId.Value)}{separator}{node.Name}";
            cache[id] = path;
            return path;
        }

        foreach (var loc in locations)
            Resolve(loc.Id);

        return cache;
    }

    public static HashSet<Guid> GetSubtreeIds(Guid root, IReadOnlyList<LocationListItem> locations)
    {
        var childrenByParent = locations
            .Where(l => l.ParentId.HasValue)
            .GroupBy(l => l.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new HashSet<Guid> { root };
        var queue = new Queue<Guid>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!childrenByParent.TryGetValue(current, out var children))
                continue;

            foreach (var childId in children)
            {
                if (result.Add(childId))
                    queue.Enqueue(childId);
            }
        }

        return result;
    }

    public static bool WouldCreateCycle(
        Guid nodeId,
        Guid? newParentId,
        IReadOnlyList<LocationListItem> locations)
    {
        if (newParentId is null)
            return false;

        if (newParentId.Value == nodeId)
            return true;

        var subtree = GetSubtreeIds(nodeId, locations);
        return subtree.Contains(newParentId.Value);
    }

    public static IReadOnlyList<LocationTreeItem> BuildTree(
        IReadOnlyList<LocationListItem> locations,
        Func<LocationListItem, bool>? includeNode = null)
    {
        includeNode ??= _ => true;

        var included = locations.Where(includeNode).ToList();
        var includedIds = included.Select(l => l.Id).ToHashSet();
        var childrenByParent = included
            .Where(l => l.ParentId.HasValue && includedIds.Contains(l.ParentId.Value))
            .GroupBy(l => l.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());

        LocationTreeItem MapNode(LocationListItem item) => new()
        {
            Id = item.Id,
            Name = item.Name,
            IsEnabled = item.IsActive,
            Children = childrenByParent.TryGetValue(item.Id, out var children)
                ? children.Select(MapNode).ToList()
                : [],
        };

        return included
            .Where(l => l.ParentId is null || !includedIds.Contains(l.ParentId.Value))
            .OrderBy(l => l.Name)
            .Select(MapNode)
            .ToList();
    }

    /// <summary>
    /// Сжимает набор id (возможно с «залипшими» id родителей после частичного снятия потомка)
    /// в минимальный набор якорей для БД. Учитывает только фактическую принадлежность листовых
    /// узлов набору — если у узла выбраны не все потомки, узел «дробится»: в результат попадают
    /// только те его потомки (рекурсивно), чьи поддеревья выбраны полностью, а не сам узел целиком.
    /// </summary>
    public static IReadOnlyList<Guid> NormalizeScopeAnchors(
        IEnumerable<Guid> anchors,
        IReadOnlyList<LocationListItem> locations)
    {
        var selected = new HashSet<Guid>(anchors);
        if (selected.Count == 0)
            return [];

        var byId = locations.ToDictionary(l => l.Id);
        var childrenByParent = locations
            .Where(l => l.ParentId.HasValue && byId.ContainsKey(l.ParentId.Value))
            .GroupBy(l => l.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new List<Guid>();

        bool Collapse(Guid id)
        {
            if (!childrenByParent.TryGetValue(id, out var children) || children.Count == 0)
                return selected.Contains(id);

            var childResults = new List<(Guid Id, bool Full)>(children.Count);
            var allFull = true;
            foreach (var childId in children)
            {
                var full = Collapse(childId);
                childResults.Add((childId, full));
                if (!full)
                    allFull = false;
            }

            if (allFull)
                return true;

            foreach (var (childId, full) in childResults)
            {
                if (full)
                    result.Add(childId);
            }

            return false;
        }

        var roots = locations
            .Where(l => l.ParentId is null || !byId.ContainsKey(l.ParentId.Value))
            .Select(l => l.Id);

        foreach (var rootId in roots)
        {
            if (Collapse(rootId))
                result.Add(rootId);
        }

        return result.Distinct().ToList();
    }
}
