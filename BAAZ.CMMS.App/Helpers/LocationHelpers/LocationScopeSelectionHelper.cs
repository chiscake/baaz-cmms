using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Controls.LocationScopePicker;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Операции выбора зон в иерархии локаций.</summary>
public static class LocationScopeSelectionHelper
{
    public static HashSet<Guid> CollectSubtreeIds(LocationScopeNode node)
    {
        var result = new HashSet<Guid> { node.Id };
        foreach (var child in node.Children)
            result.UnionWith(CollectSubtreeIds(child));

        return result;
    }

    public static HashSet<Guid> CollectSubtreeIds(LocationTreeItem node)
    {
        var result = new HashSet<Guid> { node.Id };
        foreach (var child in node.Children)
            result.UnionWith(CollectSubtreeIds(child));

        return result;
    }

    public static bool IsSubtreeFullySelected(LocationScopeNode node, IReadOnlySet<Guid> selected)
    {
        if (node.Children.Count == 0)
            return selected.Contains(node.Id);

        return node.Children.All(child => IsSubtreeFullySelected(child, selected));
    }

    public static bool HasSelectedDescendant(LocationScopeNode node, IReadOnlySet<Guid> selected)
    {
        foreach (var child in node.Children)
        {
            if (selected.Contains(child.Id) || HasSelectedDescendant(child, selected))
                return true;
        }

        return false;
    }

    public static bool HasPartialSelection(LocationScopeNode node, IReadOnlySet<Guid> selected)
    {
        if (IsSubtreeFullySelected(node, selected))
            return false;

        if (node.Children.Count == 0)
            return false;

        // Намеренно НЕ проверяем selected.Contains(node.Id) —
        // при «выделить всё» Id родителя попадает в selected, но при поштучном
        // снятии выделения с потомков Id родителя остаётся и даёт ложный partial.
        // Частичное состояние определяется только наличием выделенных потомков.
        return HasSelectedDescendant(node, selected);
    }

    /// <summary>
    /// Сводка для UI: если выбраны все потомки — имя родителя;
    /// иначе только выбранные потомки без родителя.
    /// </summary>
    public static IReadOnlyList<string> BuildCollapsedDisplayLabels(
        IEnumerable<LocationScopeNode> roots,
        IReadOnlySet<Guid> selected,
        Func<Guid, string> labelForId)
    {
        var labels = new List<string>();
        foreach (var root in roots)
            CollectCollapsedLabels(root, selected, labelForId, labels);

        return labels
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> BuildCollapsedDisplayLabels(
        IEnumerable<LocationTreeItem> roots,
        IReadOnlySet<Guid> selected,
        Func<Guid, string> labelForId)
    {
        var scopeRoots = roots.Select(MapTreeItemToScopeNode).ToList();
        return BuildCollapsedDisplayLabels(scopeRoots, selected, labelForId);
    }

    public static string FormatMultiline(IReadOnlyList<string> labels, string emptyText = "—") =>
        labels.Count == 0 ? emptyText : string.Join(Environment.NewLine, labels);

    /// <summary>
    /// Разворачивает якорные id зон (как в БД) в полный набор id поддеревьев для picker.
    /// Компактный якорь (только id узла, без id потомков рядом) разворачивается в целое поддерево.
    /// Если рядом с id узла уже присутствуют id некоторых его потомков — это уже детализированный
    /// (частично изменённый пользователем) набор, а не якорь БД; раздувать его в целое поддерево
    /// нельзя, иначе снятие одного потомка «схлопывается» обратно в полный родительский узел.
    /// </summary>
    public static HashSet<Guid> ExpandAnchorsToSelection(
        IEnumerable<Guid> anchors,
        IReadOnlyDictionary<Guid, LocationScopeNode> nodesById)
    {
        var anchorSet = new HashSet<Guid>(anchors);
        var result = new HashSet<Guid>();

        foreach (var anchor in anchorSet)
        {
            if (nodesById.TryGetValue(anchor, out var node) && node.Children.Count > 0)
            {
                var subtree = CollectSubtreeIds(node);
                var isCompactAnchor = !subtree.Any(id => id != anchor && anchorSet.Contains(id));

                if (isCompactAnchor)
                {
                    result.UnionWith(subtree);
                    continue;
                }
            }

            result.Add(anchor);
        }

        return result;
    }

    private static void CollectCollapsedLabels(
        LocationScopeNode node,
        IReadOnlySet<Guid> selected,
        Func<Guid, string> labelForId,
        ICollection<string> labels)
    {
        if (selected.Contains(node.Id))
        {
            if (node.Children.Count == 0)
            {
                labels.Add(labelForId(node.Id));
                return;
            }

            // Expanded «выделить всё» — все потомки в selected.
            if (IsSubtreeFullySelected(node, selected))
            {
                labels.Add(labelForId(node.Id));
                return;
            }

            // Якорь из БД: только id узла, без id потомков — поддерево целиком.
            if (!HasSelectedDescendant(node, selected))
            {
                labels.Add(labelForId(node.Id));
                return;
            }

            // Id родителя остался после частичного снятия с потомка — не сворачиваем.
            foreach (var child in node.Children)
                CollectCollapsedLabels(child, selected, labelForId, labels);
            return;
        }

        // Всё поддерево выбрано через потомков (expanded без якоря на самом узле).
        if (node.Children.Count > 0 && IsSubtreeFullySelected(node, selected))
        {
            labels.Add(labelForId(node.Id));
            return;
        }

        foreach (var child in node.Children)
            CollectCollapsedLabels(child, selected, labelForId, labels);
    }

    private static LocationScopeNode MapTreeItemToScopeNode(LocationTreeItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        IsEnabled = item.IsEnabled,
        Children = item.Children.Select(MapTreeItemToScopeNode).ToList(),
    };
}
