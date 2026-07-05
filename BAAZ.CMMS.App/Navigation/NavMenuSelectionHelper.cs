using System.Collections.Generic;

using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavMenuSelectionHelper
{
    public static NavigationViewItem? FindItemById(IList<object> items, string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (var obj in items)
        {
            if (obj is not NavigationViewItem item)
            {
                continue;
            }

            if (item.Tag as string == id)
            {
                return item;
            }

            var nested = FindItemById(item.MenuItems, id);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    public static void ExpandGroups(IList<object> items, IReadOnlyList<string> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return;
        }

        ExpandGroupsRecursive(items, groupIds, 0);
    }

    public static void CollapseAllGroups(IList<object> items)
    {
        foreach (var obj in items)
        {
            if (obj is not NavigationViewItem item || item.MenuItems.Count == 0)
            {
                continue;
            }

            item.IsExpanded = false;
            CollapseAllGroups(item.MenuItems);
        }
    }

    private static void ExpandGroupsRecursive(
        IList<object> items,
        IReadOnlyList<string> groupIds,
        int depth)
    {
        if (depth >= groupIds.Count)
        {
            return;
        }

        var targetGroupId = groupIds[depth];

        foreach (var obj in items)
        {
            if (obj is not NavigationViewItem item)
            {
                continue;
            }

            if (item.Tag as string != targetGroupId)
            {
                continue;
            }

            item.IsExpanded = true;
            ExpandGroupsRecursive(item.MenuItems, groupIds, depth + 1);
            return;
        }
    }
}
