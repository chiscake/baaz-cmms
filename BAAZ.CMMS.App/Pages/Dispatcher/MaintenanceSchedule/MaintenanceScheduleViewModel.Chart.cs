using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Admin.AssetRegistry;
using BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;
using BAAZ.CMMS.Core.Models;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceScheduleViewModel
{
    private readonly HashSet<Guid> _collapsedLocationIds = [];
    private ScheduleTimelineScale _timelineScale;
    private LocationTreeSnapshot? _locationSnapshot;

    public event EventHandler<Guid>? ScrollToRowRequested;

    public event EventHandler<DateOnly>? ScrollToDateRequested;

    public string ChartNavPrevLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Nav_Prev");

    public string ChartNavNextLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Nav_Next");

    public string ChartNavTodayLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Nav_Today");

    public string ChartZoomWeekLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Zoom_Week");

    public string ChartZoomMonthLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Zoom_Month");

    public string ChartZoomQuarterLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Zoom_Quarter");

    public string ChartEmptyRangeText => ResourceStrings.Get("MaintenanceSchedule_Chart_EmptyRange");

    public string ChartEmptyDbText => ResourceStrings.Get("MaintenanceSchedule_Chart_EmptyDb");

    public string ChartLoadErrorText => ResourceStrings.Get("MaintenanceSchedule_Chart_LoadError");

    public string ChartTodayLineLabel => ResourceStrings.Get("MaintenanceSchedule_Chart_Today");

    public string ChartLaneObjectHeader => ResourceStrings.Get("MaintenanceSchedule_Chart_LaneHeader");

    public ObservableCollection<ChartLaneRowVm> SwimlaneRows { get; } = [];

    public ObservableCollection<MaintenanceScheduleRow> PanelRows { get; } = [];

    public ObservableCollection<ChartDayHeaderVm> DayHeaders { get; } = [];

    public ObservableCollection<ChartHeatSegmentVm> HeatSegments { get; } = [];

    [ObservableProperty]
    public partial DateOnly VisibleRangeStart { get; private set; }

    [ObservableProperty]
    public partial DateOnly VisibleRangeEnd { get; private set; }

    [ObservableProperty]
    public partial string VisibleRangeText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial ScheduleZoomPreset SelectedZoomPreset { get; private set; } = ScheduleZoomPreset.Month;

    [ObservableProperty]
    public partial double TimelineDayWidth { get; private set; } = 32;

    [ObservableProperty]
    public partial double TimelineTotalWidth { get; private set; }

    [ObservableProperty]
    public partial Guid? HighlightedRowId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChartPanelList))]
    public partial bool ShowChartEmptyRange { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChartPanelList))]
    public partial bool ShowChartEmptyDb { get; private set; }

    [ObservableProperty]
    public partial bool IsChartLoading { get; private set; }

    [ObservableProperty]
    public partial double TodayLineLeft { get; private set; } = double.NaN;

    public double SplitPaneStarWeight { get; private set; } = 1.5;

    public bool ShowChartPanelList => IsChartView && !ShowChartEmptyRange && !ShowChartEmptyDb && !IsLoading;

    public double MarkerSize => SelectedZoomPreset switch
    {
        ScheduleZoomPreset.Week => 16,
        ScheduleZoomPreset.Quarter => 8,
        _ => 12,
    };

    public double DayHeaderNumberFontSize => SelectedZoomPreset switch
    {
        ScheduleZoomPreset.Quarter => 10,
        _ => 12,
    };

    partial void OnSelectedViewIndexChanged(int value)
    {
        MaintenanceSchedulePrefsStore.Update(p => p.SelectedViewIndex = value);
        if (IsChartView)
            _ = RebuildChartStateAsync();
    }

    partial void OnSelectedZoomPresetChanged(ScheduleZoomPreset value)
    {
        _timelineScale.SetPreset(value);
        SyncTimelineFromScale();
        MaintenanceSchedulePrefsStore.Update(p => p.ZoomPreset = (int)value);
        _ = RebuildChartStateAsync();
    }

    public void RestorePrefs()
    {
        var prefs = MaintenanceSchedulePrefsStore.Load();
        SelectedViewIndex = prefs.SelectedViewIndex is >= 0 and <= 1 ? prefs.SelectedViewIndex : 0;
        SelectedZoomPreset = Enum.IsDefined(typeof(ScheduleZoomPreset), prefs.ZoomPreset)
            ? (ScheduleZoomPreset)prefs.ZoomPreset
            : ScheduleZoomPreset.Month;
        SplitPaneStarWeight = prefs.SplitPaneStarWeight > 0 ? prefs.SplitPaneStarWeight : 1.5;
        _collapsedLocationIds.Clear();
        foreach (var id in prefs.CollapsedLocationIds)
            _collapsedLocationIds.Add(id);

        _timelineScale = new ScheduleTimelineScale(SelectedZoomPreset, DateOnly.FromDateTime(DateTime.Today));
        SyncTimelineFromScale();
    }

    public void SaveSplitPaneWeight(double starWeight)
    {
        SplitPaneStarWeight = starWeight;
        MaintenanceSchedulePrefsStore.Update(p => p.SplitPaneStarWeight = starWeight);
    }

    [RelayCommand]
    private void GoToToday()
    {
        _timelineScale = new ScheduleTimelineScale(SelectedZoomPreset, DateOnly.FromDateTime(DateTime.Today));
        SyncTimelineFromScale();
        _ = RebuildChartStateAsync();
    }

    [RelayCommand]
    private void NavigatePrev()
    {
        _timelineScale.NavigatePrevious();
        SyncTimelineFromScale();
        _ = RebuildChartStateAsync();
    }

    [RelayCommand]
    private void NavigateNext()
    {
        _timelineScale.NavigateNext();
        SyncTimelineFromScale();
        _ = RebuildChartStateAsync();
    }

    [RelayCommand]
    private void SetZoomWeek() => SelectedZoomPreset = ScheduleZoomPreset.Week;

    [RelayCommand]
    private void SetZoomMonth() => SelectedZoomPreset = ScheduleZoomPreset.Month;

    [RelayCommand]
    private void SetZoomQuarter() => SelectedZoomPreset = ScheduleZoomPreset.Quarter;

    [RelayCommand]
    private void ToggleLocationCollapse(Guid locationId)
    {
        if (!_collapsedLocationIds.Add(locationId))
            _collapsedLocationIds.Remove(locationId);

        MaintenanceSchedulePrefsStore.Update(p =>
        {
            p.CollapsedLocationIds = _collapsedLocationIds.ToList();
        });
        _ = RebuildChartStateAsync();
    }

    [RelayCommand]
    private void HighlightAndScrollToRow(Guid scheduleId)
    {
        HighlightedRowId = scheduleId;
        ScrollToRowRequested?.Invoke(this, scheduleId);
    }

    [RelayCommand]
    private void ScrollPanelToDate(DateOnly date)
    {
        ScrollToDateRequested?.Invoke(this, date);
    }

    [RelayCommand]
    private async Task OpenAssetAsync(Guid assetId)
    {
        var row = Rows.FirstOrDefault(r => r.AssetId == assetId);
        if (row is null)
            return;

        if (IsAdmin)
        {
            _navigationService.NavigateTo(
                "AssetRegistry",
                new AssetRegistryNavigationArgs(AssetId: assetId));
            return;
        }

        await ShowDetailsAsync(row);
    }

    [RelayCommand]
    private void OnMarkerClicked(ChartMarkerVm? marker)
    {
        if (marker is null)
            return;

        if (marker.EventCount > 1 && marker.SameDayEvents.Count > 1)
            return;

        HighlightAndScrollToRow(marker.ScheduleId);
    }

    private async Task EnsureLocationTreeAsync()
    {
        if (_locationSnapshot is not null)
            return;

        _locationSnapshot = await _locationTreeCache.EnsureLoadedAsync();
    }

    private void SyncTimelineFromScale()
    {
        VisibleRangeStart = _timelineScale.RangeStart;
        VisibleRangeEnd = _timelineScale.RangeEnd;
        TimelineDayWidth = _timelineScale.DayWidth;
        TimelineTotalWidth = _timelineScale.TotalWidth;
        VisibleRangeText = string.Format(
            CultureInfo.CurrentCulture,
            ResourceStrings.Get("MaintenanceSchedule_Chart_Range_Format"),
            VisibleRangeStart.ToString("d MMMM yyyy", CultureInfo.CurrentCulture),
            VisibleRangeEnd.ToString("d MMMM yyyy", CultureInfo.CurrentCulture));

        var today = DateOnly.FromDateTime(DateTime.Today);
        TodayLineLeft = _timelineScale.Contains(today)
            ? _timelineScale.ToX(today)
            : double.NaN;

        DayHeaders.Clear();
        foreach (var header in _timelineScale.BuildDayHeaders())
            DayHeaders.Add(header);
    }

    private async Task RebuildChartStateAsync()
    {
        if (!IsChartView)
            return;

        IsChartLoading = true;
        try
        {
            await EnsureLocationTreeAsync();
            if (_locationSnapshot is null)
                return;

            var filteredItems = GetFilteredItems().ToList();
            ShowChartEmptyDb = _allItems.Count == 0 && !IsLoading;

            var buildInput = new ScheduleSwimlaneBuilder.BuildInput
            {
                LocationRoots = _locationSnapshot.ActiveRoots,
                LocationsById = _locationSnapshot.ById,
                ScheduleItems = filteredItems,
                Scale = _timelineScale,
                CollapsedLocationIds = _collapsedLocationIds,
                Today = DateOnly.FromDateTime(DateTime.Today),
            };

            SwimlaneRows.Clear();
            foreach (var row in ScheduleSwimlaneBuilder.Build(buildInput))
                SwimlaneRows.Add(row);

            HeatSegments.Clear();
            foreach (var segment in ScheduleSwimlaneBuilder.BuildHeatMap(
                         filteredItems, _timelineScale, DateOnly.FromDateTime(DateTime.Today)))
                HeatSegments.Add(segment);

            PanelRows.Clear();
            foreach (var row in Rows
                         .Where(r => r.PlannedDate >= VisibleRangeStart && r.PlannedDate <= VisibleRangeEnd))
                PanelRows.Add(row);

            ShowChartEmptyRange = PanelRows.Count == 0 && !ShowChartEmptyDb && !IsLoading;
        }
        finally
        {
            IsChartLoading = false;
        }
    }

    private IEnumerable<MaintenanceScheduleItem> GetFilteredItems()
    {
        IEnumerable<MaintenanceScheduleItem> filtered = _allItems;

        if (GetSelectedStatus() is { } status)
            filtered = filtered.Where(i => string.Equals(i.Status, status, StringComparison.Ordinal));

        var needle = FilterText?.Trim();
        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(i =>
                Contains(i.AssetName, needle)
                || Contains(i.AssetNumber, needle));
        }

        if (GetSelectedAssetId() is Guid assetId)
            filtered = filtered.Where(i => i.AssetId == assetId);

        if (GetSelectedMaintenanceType() is { } maintenanceType)
        {
            filtered = filtered.Where(i =>
                string.Equals(i.MaintenanceType, maintenanceType, StringComparison.Ordinal));
        }

        if (IsAdmin && GetSelectedDepartmentId() is Guid departmentId)
            filtered = filtered.Where(i => i.DepartmentIds.Contains(departmentId));

        return filtered;
    }

    public IReadOnlyList<MaintenanceScheduleItem> GetItemsForExport()
    {
        var filtered = GetFilteredItems().ToList();
        if (!IsChartView)
            return filtered;

        return filtered
            .Where(i => i.PlannedDate >= VisibleRangeStart && i.PlannedDate <= VisibleRangeEnd)
            .ToList();
    }
}
