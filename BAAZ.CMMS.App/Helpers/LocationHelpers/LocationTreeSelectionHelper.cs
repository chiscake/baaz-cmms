using System;
using System.Collections.Generic;

using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Выбор узла в иерархическом TreeView локаций.</summary>
public static class LocationTreeSelectionHelper
{
    public static void ApplySelection(
        TreeView tree,
        IReadOnlyList<LocationTreeItem>? items,
        Guid? selectedId)
    {
        if (selectedId is Guid id)
        {
            var found = FindItem(items, id, 0);
            if (found is not null)
            {
                ExpandAncestorsOf(tree, items, id);
                tree.SelectedItem = found;
                return;
            }
        }

        tree.SelectedItem = null;
    }

    public static LocationTreeItem? FindItem(IEnumerable<LocationTreeItem>? items, Guid id, int depth)
    {
        if (items is null)
            return null;

        if (depth > 64)
            return null;

        foreach (var item in items)
        {
            if (item.Id == id)
                return item;

            var found = FindItem(item.Children, id, depth + 1);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static void ExpandAncestorsOf(
        TreeView tree,
        IReadOnlyList<LocationTreeItem>? items,
        Guid targetId)
    {
        if (items is null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.Id == targetId)
                return;

            if (ContainsDescendant(item.Children, targetId, 0) && i < tree.RootNodes.Count)
            {
                tree.RootNodes[i].IsExpanded = true;
                return;
            }
        }
    }

    private static bool ContainsDescendant(IList<LocationTreeItem> items, Guid id, int depth)
    {
        if (depth > 64)
            return false;

        foreach (var item in items)
        {
            if (item.Id == id)
                return true;

            if (ContainsDescendant(item.Children, id, depth + 1))
                return true;
        }

        return false;
    }
}
