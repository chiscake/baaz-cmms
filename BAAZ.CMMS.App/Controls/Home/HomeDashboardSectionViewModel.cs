using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Controls.Home;

public abstract partial class HomeDashboardSectionViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    protected HomeDashboardSectionViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public abstract string RoleLabel { get; }

    public abstract string SectionHeading { get; }

    public ObservableCollection<HomeStatRow> StatRows { get; } = [];

    public ObservableCollection<HomeQuickAction> Actions { get; } = [];

    public ObservableCollection<HomeNavLink> NavLinks { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? LoadError { get; set; }

    public abstract Task LoadAsync(CancellationToken cancellationToken = default);

    protected void ClearStats() => StatRows.Clear();

    protected HomeStatRow BeginStatRow(
        int columns,
        string? headingResourceKey = null,
        string? secondaryHeadingResourceKey = null)
    {
        var row = new HomeStatRow
        {
            Columns = columns,
            Heading = headingResourceKey is null ? null : ResourceStrings.Get(headingResourceKey),
            SecondaryHeading = secondaryHeadingResourceKey is null
                ? null
                : ResourceStrings.Get(secondaryHeadingResourceKey),
        };
        StatRows.Add(row);
        return row;
    }

    protected void AddStat(
        HomeStatRow row,
        string labelResourceKey,
        int value,
        string glyph,
        StatusBadgeColorToken colorToken,
        string? pageKey = null,
        object? navigationParameter = null)
    {
        row.Items.Add(new HomeStatItem
        {
            Label = ResourceStrings.Get(labelResourceKey),
            Value = value.ToString(),
            Glyph = glyph,
            ValueColorToken = colorToken,
            IconColorToken = colorToken,
            PageKey = pageKey,
            NavigationParameter = navigationParameter,
            NavigateCommand = string.IsNullOrWhiteSpace(pageKey) ? null : NavigateStatCommand,
        });
    }

    protected static StatusBadgeColorToken RequestColor(string status)
        => StatusBadgeFactory.ForRequest(status).Background;

    protected static StatusBadgeColorToken AssetColor(string status)
        => StatusBadgeFactory.ForAsset(status).Background;

    protected static StatusBadgeColorToken ScheduleColor(string status)
        => StatusBadgeFactory.ForSchedule(status).Background;

    internal void AddAction(string titleResourceKey, string glyph, string pageKey, bool isPrimary = false)
    {
        Actions.Add(new HomeQuickAction
        {
            Title = ResourceStrings.Get(titleResourceKey),
            Glyph = glyph,
            PageKey = pageKey,
            IsPrimary = isPrimary,
            NavigateCommand = NavigateActionCommand,
        });
    }

    internal void AddNavLink(string titleResourceKey, string pageKey)
    {
        NavLinks.Add(new HomeNavLink
        {
            Title = ResourceStrings.Get(titleResourceKey),
            PageKey = pageKey,
            ShowSeparator = NavLinks.Count > 0,
            NavigateCommand = NavigateLinkCommand,
        });
    }

    protected static void AddStatSpacer(HomeStatRow row) =>
        row.Items.Add(new HomeStatItem
        {
            IsSpacer = true,
            Label = string.Empty,
            Value = string.Empty,
            Glyph = string.Empty,
        });

    [RelayCommand]
    private void NavigateStat(HomeStatItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.PageKey))
        {
            return;
        }

        _navigationService.NavigateTo(item.PageKey, item.NavigationParameter);
    }

    [RelayCommand]
    private void NavigateAction(HomeQuickAction? action)
    {
        if (action is null || string.IsNullOrWhiteSpace(action.PageKey))
        {
            return;
        }

        _navigationService.NavigateTo(action.PageKey);
    }

    [RelayCommand]
    private void NavigateLink(HomeNavLink? link)
    {
        if (link is null || string.IsNullOrWhiteSpace(link.PageKey))
        {
            return;
        }

        _navigationService.NavigateTo(link.PageKey);
    }
}
