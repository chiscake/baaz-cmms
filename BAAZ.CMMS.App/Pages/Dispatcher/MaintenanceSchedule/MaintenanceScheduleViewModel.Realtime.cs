using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Realtime;

using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceScheduleViewModel
{
    private readonly List<RealtimeEvent> _realtimeEventBuffer = [];
    private readonly object _realtimeBufferGate = new();
    private readonly SemaphoreSlim _dataRefreshGate = new(1, 1);
    private CancellationTokenSource? _dataRefreshCts;

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        if (_realtimeReloadSuppressCount > 0)
            return;

        if (!string.Equals(e.Table, "maintenance_schedule", StringComparison.Ordinal)
            && !string.Equals(e.Table, "work_reports", StringComparison.Ordinal))
            return;

        lock (_realtimeBufferGate)
            _realtimeEventBuffer.Add(e);

        RealtimeUiRefresh.EnqueueDebounced(
            "MaintenanceSchedule",
            ApplyBufferedRealtimeAsync,
            delayMs: 800);
    }

    private Task ApplyBufferedRealtimeAsync() =>
        RunDataRefreshAsync(ApplyBufferedRealtimeCoreAsync, showPageLoading: false);

    private async Task ApplyBufferedRealtimeCoreAsync(CancellationToken cancellationToken)
    {
        List<RealtimeEvent> batch;
        lock (_realtimeBufferGate)
        {
            batch = [.._realtimeEventBuffer];
            _realtimeEventBuffer.Clear();
        }

        if (batch.Count == 0)
            return;

        var items = _allItems.ToList();
        var locallyMutated = false;
        var needsLightReload = false;

        foreach (var e in batch)
        {
            if (TryApplyRealtimeEvent(items, e, out var requiresReload))
            {
                locallyMutated = true;
                if (requiresReload)
                    needsLightReload = true;
            }
            else
            {
                needsLightReload = true;
            }
        }

        if (needsLightReload)
            _allItems = await _maintenanceService.RefreshScheduleItemsAsync(cancellationToken);
        else if (locallyMutated)
            _allItems = items;
        else
            return;

        RebuildAssetFilterOptions();
        RebuildDepartmentFilterOptions();
        ApplyFiltersAndSort(rebuildChart: false);
        SyncNavBadgeFromSchedule();
        OnPropertyChanged(nameof(ShowEmpty));

        if (IsChartView)
            await RebuildChartStateAsync();
    }

    private static bool TryApplyRealtimeEvent(
        List<MaintenanceScheduleItem> items,
        RealtimeEvent e,
        out bool requiresReload)
    {
        requiresReload = false;

        if (string.Equals(e.Table, "work_reports", StringComparison.Ordinal)
            && e.Payload is WorkReportModel report
            && report.ScheduleId is Guid scheduleId)
        {
            var index = items.FindIndex(i => i.Id == scheduleId);
            if (index < 0)
            {
                requiresReload = true;
                return true;
            }

            var reported = items[index].ReportedDepartmentIds.ToList();
            if (e.EventType == RealtimeEventType.Delete)
                reported.Remove(report.RepairDepartmentId);
            else
            {
                if (!reported.Contains(report.RepairDepartmentId))
                    reported.Add(report.RepairDepartmentId);
            }

            items[index] = CopyItem(items[index], reportedDepartmentIds: reported);
            return true;
        }

        if (!string.Equals(e.Table, "maintenance_schedule", StringComparison.Ordinal)
            || e.Payload is not MaintenanceScheduleModel schedule)
        {
            requiresReload = true;
            return false;
        }

        switch (e.EventType)
        {
            case RealtimeEventType.Delete:
                items.RemoveAll(i => i.Id == schedule.Id);
                return true;

            case RealtimeEventType.Update:
            {
                var index = items.FindIndex(i => i.Id == schedule.Id);
                if (index < 0)
                {
                    requiresReload = true;
                    return true;
                }

                items[index] = CopyItem(
                    items[index],
                    status: schedule.Status,
                    plannedDate: schedule.PlannedDate);
                return true;
            }

            case RealtimeEventType.Insert:
                requiresReload = true;
                return true;

            default:
                requiresReload = true;
                return false;
        }
    }

    private static MaintenanceScheduleItem CopyItem(
        MaintenanceScheduleItem source,
        string? status = null,
        DateOnly? plannedDate = null,
        IReadOnlyList<Guid>? reportedDepartmentIds = null) =>
        new()
        {
            Id = source.Id,
            AssetId = source.AssetId,
            LocationId = source.LocationId,
            AssetName = source.AssetName,
            AssetNumber = source.AssetNumber,
            MaintenanceType = source.MaintenanceType,
            PlannedDate = plannedDate ?? source.PlannedDate,
            Status = status ?? source.Status,
            LastMaintenanceDate = source.LastMaintenanceDate,
            NextMaintenanceDate = source.NextMaintenanceDate,
            DepartmentNames = source.DepartmentNames,
            DepartmentIds = source.DepartmentIds,
            ReportedDepartmentIds = reportedDepartmentIds ?? source.ReportedDepartmentIds,
        };

    private async Task RunDataRefreshAsync(
        Func<CancellationToken, Task> coreAsync,
        bool showPageLoading = true)
    {
        _dataRefreshCts?.Cancel();
        _dataRefreshCts?.Dispose();
        _dataRefreshCts = new CancellationTokenSource();
        var cancellationToken = _dataRefreshCts.Token;

        await _dataRefreshGate.WaitAsync(CancellationToken.None);
        try
        {
            if (showPageLoading)
            {
                IsLoading = true;
                InfoBanner.Report(string.Empty);
            }

            await coreAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            if (showPageLoading)
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            if (showPageLoading)
                IsLoading = false;

            _dataRefreshGate.Release();
        }
    }
}
