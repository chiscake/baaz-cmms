using System;
using System.Collections.Generic;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

/// <summary>
/// Снимок UI-состояния «График ППР» для перезагрузки страницы (например, при смене темы).
/// </summary>
public sealed record MaintenanceScheduleNavigationArgs(
    int SelectedViewIndex,
    int SelectedStatusFilterIndex,
    string FilterText,
    Guid? SelectedAssetId,
    string? SelectedMaintenanceType,
    Guid? SelectedDepartmentId,
    int SelectedSortIndex,
    ScheduleZoomPreset SelectedZoomPreset,
    DateOnly VisibleRangeStart,
    DateOnly VisibleRangeEnd,
    double SplitPaneStarWeight,
    IReadOnlyList<Guid> CollapsedLocationIds,
    Guid? HighlightedRowId = null);
