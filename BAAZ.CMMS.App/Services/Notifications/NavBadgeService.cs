using System;
using System.Collections.Generic;

using BAAZ.CMMS.App.Navigation;

using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Services.Notifications;

public sealed class NavBadgeService : INavBadgeService
{
    private static readonly HashSet<string> AlwaysVisibleBadges =
    [
        NavItemIds.DispatcherIncomingRequests,
        NavItemIds.DispatcherMaintenanceSchedule,
    ];

    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
    private NavigationView? _navigationView;

    public event EventHandler? BadgesChanged;

    public void Attach(NavigationView navigationView)
    {
        _navigationView = navigationView;

        foreach (var navItemId in AlwaysVisibleBadges)
        {
            if (!_counts.ContainsKey(navItemId))
                _counts[navItemId] = 0;
        }

        ApplyToNavigationView();
    }

    public int GetCount(string navItemId)
        => _counts.TryGetValue(navItemId, out var count) ? count : 0;

    public void SetCount(string navItemId, int count)
    {
        count = Math.Max(0, count);
        if (_counts.TryGetValue(navItemId, out var current) && current == count)
            return;

        if (count == 0 && !AlwaysVisibleBadges.Contains(navItemId))
            _counts.Remove(navItemId);
        else
            _counts[navItemId] = count;

        BadgesChanged?.Invoke(this, EventArgs.Empty);
        ApplyToNavigationView();
    }

    public void Increment(string navItemId, int delta = 1)
    {
        if (delta == 0)
            return;

        var next = Math.Max(0, GetCount(navItemId) + delta);
        SetCount(navItemId, next);
    }

    public void Clear(string navItemId) => SetCount(navItemId, 0);

    public void ApplyToNavigationView()
    {
        if (_navigationView is null)
            return;

        _navigationView.DispatcherQueue.TryEnqueue(() =>
        {
            ApplyToItems(_navigationView.MenuItems);
            if (_navigationView.SettingsItem is NavigationViewItem settingsItem)
                ApplyBadge(settingsItem, NavMenuTags.Settings);
        });
    }

    private void ApplyToItems(IList<object> items)
    {
        foreach (var item in items)
        {
            if (item is not NavigationViewItem navItem)
                continue;

            if (navItem.Tag is string tag)
                ApplyBadge(navItem, tag);

            if (navItem.MenuItems.Count > 0)
                ApplyToItems(navItem.MenuItems);
        }
    }

    private void ApplyBadge(NavigationViewItem item, string navItemId)
    {
        var count = GetCount(navItemId);
        var alwaysVisible = AlwaysVisibleBadges.Contains(navItemId);

        if (count <= 0 && !alwaysVisible)
        {
            item.InfoBadge = null;
            return;
        }

        item.InfoBadge = new InfoBadge
        {
            Value = count > 99 ? 99 : count,
        };
    }
}
