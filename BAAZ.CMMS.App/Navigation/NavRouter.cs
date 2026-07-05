using System;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavRouter
{
    public static void Navigate(
        INavigationService navigation,
        NavMenuLeafNode node,
        string roleHomePageKey)
    {
        if (node.Leaf.SectionTag == NavMenuTags.Home)
        {
            navigation.NavigateTo(node.HomePageKeyOverride ?? roleHomePageKey);
            return;
        }

        if (!string.IsNullOrEmpty(node.Leaf.PageKey))
        {
            navigation.NavigateTo(node.Leaf.PageKey);
            return;
        }

        // Все leaf в NavLeafCatalog имеют PageKey; этот путь не должен достигаться.
        throw new InvalidOperationException($"NavLeaf '{node.Leaf.SectionTag}' has no PageKey.");
    }

    public static bool MatchesNavigation(
        NavMenuLeafNode node,
        string? pageKey,
        object? parameter,
        string roleHomePageKey)
    {
        if (string.IsNullOrEmpty(pageKey))
        {
            return false;
        }

        if (node.Leaf.SectionTag == NavMenuTags.Home)
        {
            var target = node.HomePageKeyOverride ?? roleHomePageKey;
            return pageKey == target;
        }

        return node.Leaf.PageKey is not null && pageKey == node.Leaf.PageKey;
    }
}
