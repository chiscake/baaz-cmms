using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Фильтрация иерархии локаций по имени и полному пути.</summary>
public static class LocationTreeFilterHelper
{
    public static IReadOnlyList<LocationTreeItem> FilterTree(
        IReadOnlyList<LocationTreeItem>? roots,
        string? query,
        IReadOnlyDictionary<Guid, string>? paths = null)
    {
        if (roots is null || roots.Count == 0)
            return [];

        var normalized = query?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return roots;

        return roots
            .Select(r => FilterNode(r, normalized, paths))
            .Where(n => n is not null)
            .Cast<LocationTreeItem>()
            .ToList();
    }

    private static LocationTreeItem? FilterNode(
        LocationTreeItem node,
        string query,
        IReadOnlyDictionary<Guid, string>? paths)
    {
        var matchesSelf = NodeMatches(node, query, paths);
        var filteredChildren = node.Children
            .Select(c => FilterNode(c, query, paths))
            .Where(c => c is not null)
            .Cast<LocationTreeItem>()
            .ToList();

        if (!matchesSelf && filteredChildren.Count == 0)
            return null;

        return new LocationTreeItem
        {
            Id = node.Id,
            Name = node.Name,
            IsEnabled = node.IsEnabled,
            Children = filteredChildren,
        };
    }

    private static bool NodeMatches(
        LocationTreeItem node,
        string query,
        IReadOnlyDictionary<Guid, string>? paths)
    {
        if (node.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            return true;

        if (paths is not null
            && paths.TryGetValue(node.Id, out var path)
            && path.Contains(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
