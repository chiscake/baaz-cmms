using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavMenuBuilder
{
    public static void ApplyRoleMenu(NavigationView navigationView, UserRole role)
    {
        navigationView.MenuItems.Clear();

        foreach (var node in NavMenuRegistry.GetMenuTree(role))
        {
            navigationView.MenuItems.Add(CreateItem(node));
        }

        var settings = NavMenuRegistry.Settings;
        if (navigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = ResourceStrings.Get(settings.TitleResourceKey);
            settingsItem.Tag = NavMenuTags.Settings;
            settingsItem.Icon = new SymbolIcon(settings.Icon);
        }
    }

    private static NavigationViewItem CreateItem(NavMenuNode node) => node switch
    {
        NavMenuLeafNode leaf => new NavigationViewItem
        {
            Tag = leaf.Id,
            Content = ResourceStrings.Get(leaf.Leaf.TitleResourceKey),
            Icon = new SymbolIcon(leaf.Leaf.Icon),
        },
        NavMenuGroupNode group => CreateGroupItem(group),
        _ => throw new System.InvalidOperationException("Unknown navigation menu node."),
    };

    private static NavigationViewItem CreateGroupItem(NavMenuGroupNode group)
    {
        var item = new NavigationViewItem
        {
            Tag = group.Id,
            Content = ResourceStrings.Get(group.TitleResourceKey),
            Icon = new SymbolIcon(group.Icon),
            SelectsOnInvoked = group.SelectsOnInvoked,
        };

        foreach (var child in group.Children)
        {
            item.MenuItems.Add(CreateItem(child));
        }

        return item;
    }
}
