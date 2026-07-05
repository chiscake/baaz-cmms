using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.WorkReports;

public sealed partial class WorkReportsViewModel : PageViewModelBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly INavigationService _navigationService;
    private readonly IRealtimeNotificationService _realtimeService;

    private IReadOnlyList<WorkReportItem> _allItems = [];
    private bool _realtimeSubscribed;

    public WorkReportsViewModel(
        IMaintenanceService maintenanceService,
        INavigationService navigationService,
        IRealtimeNotificationService realtimeService)
    {
        _maintenanceService = maintenanceService;
        _navigationService = navigationService;
        _realtimeService = realtimeService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_WorkReports");

    public IReadOnlyList<string> TabLabels { get; } =
    [
        ResourceStrings.Get("WorkReports_Tab_All"),
        ResourceStrings.Get("WorkReports_Tab_Requests"),
        ResourceStrings.Get("WorkReports_Tab_Schedule"),
    ];

    public string ActionOpen => ResourceStrings.Get("WorkReports_Action_Open");
    public string FilterPlaceholder => ResourceStrings.Get("WorkReports_Filter_Placeholder");
    public string EmptyText => ResourceStrings.Get("WorkReports_Empty");

    public ObservableCollection<WorkReportsRow> Rows { get; } = [];

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasRows { get; set; }

    public bool ShowEmpty => !IsLoading && !HasRows;

    partial void OnSelectedTabIndexChanged(int value) => ApplyFilter();

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    public async Task OnPageLoadedAsync()
    {
        SubscribeRealtime();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenSource(WorkReportsRow? row)
    {
        if (row is null)
            return;

        if (row.RequestId is Guid requestId)
        {
            _navigationService.NavigateTo(
                "RequestDetail",
                new RequestDetailNavigationArgs { RequestId = requestId });
            return;
        }

        if (row.ScheduleId is not null)
            _navigationService.NavigateTo("MaintenanceSchedule");
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
            _allItems = await _maintenanceService.GetWorkReportsAsync();
            ApplyFilter();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmpty));
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<WorkReportItem> filtered = _allItems;

        filtered = SelectedTabIndex switch
        {
            1 => filtered.Where(i => i.RequestId is not null),
            2 => filtered.Where(i => i.ScheduleId is not null),
            _ => filtered,
        };

        var needle = FilterText?.Trim();
        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(i =>
                Contains(i.WorkPerformed, needle)
                || Contains(i.TechnicianName, needle)
                || Contains(i.RepairDepartmentName, needle)
                || Contains(i.RequestNumber, needle)
                || Contains(i.ScheduleAssetName, needle)
                || Contains(i.ScheduleAssetNumber, needle));
        }

        Rows.Clear();
        foreach (var item in filtered)
            Rows.Add(WorkReportsRow.FromItem(item, this));

        HasRows = Rows.Count > 0;
        OnPropertyChanged(nameof(ShowEmpty));
    }

    private static bool Contains(string? haystack, string needle) =>
        haystack?.Contains(needle, StringComparison.CurrentCultureIgnoreCase) == true;

    private void SubscribeRealtime()
    {
        if (_realtimeSubscribed)
            return;

        _realtimeService.EventReceived += OnRealtimeEvent;
        _realtimeSubscribed = true;
    }

    public void UnsubscribeRealtime()
    {
        if (!_realtimeSubscribed)
            return;

        _realtimeService.EventReceived -= OnRealtimeEvent;
        _realtimeSubscribed = false;
    }

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        if (!string.Equals(e.Table, "work_reports", StringComparison.Ordinal))
            return;

        RealtimeUiRefresh.Enqueue(LoadAsync);
    }
}
