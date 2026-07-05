using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services.Requisitions;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed class MaintenanceScheduleRow
{
    public required Guid Id { get; init; }

    public required Guid AssetId { get; init; }

    public required string AssetNumber { get; init; }

    public required string AssetName { get; init; }

    public required string MaintenanceType { get; init; }

    public required DateOnly PlannedDate { get; init; }

    public required string Status { get; init; }

    public DateOnly? LastMaintenanceDate { get; init; }

    public DateOnly? NextMaintenanceDate { get; init; }

    public required IReadOnlyList<string> DepartmentNames { get; init; }

    public required IReadOnlyList<Guid> DepartmentIds { get; init; }

    public required IReadOnlyList<Guid> ReportedDepartmentIds { get; init; }

    public string MaintenanceTypeLabel => MaintenanceTypeLabels.Get(MaintenanceType);

    public string StatusLabel => MaintenanceTypeLabels.ScheduleStatus(Status);

    public string StatusBadgeBackgroundKey => StatusBadgeFactory.ForSchedule(Status).BackgroundKey;

    public string StatusBadgeForegroundKey => StatusBadgeFactory.ForSchedule(Status).ForegroundKey;

    public string PlannedDateText => DateDisplayHelper.Format(PlannedDate);

    public string LastMaintenanceDateText => DateDisplayHelper.Format(LastMaintenanceDate);

    public string NextMaintenanceDateText => DateDisplayHelper.Format(NextMaintenanceDate);

    public string DepartmentsText => DepartmentNames.Count == 0
        ? ResourceStrings.Get("Common_None")
        : string.Join(", ", DepartmentNames);

    public string MaintenanceDatesLine =>
        $"{ResourceStrings.Get("MaintenanceSchedule_Column_LastMaintenance")} {LastMaintenanceDateText} • " +
        $"{ResourceStrings.Get("MaintenanceSchedule_Column_NextMaintenance")} {NextMaintenanceDateText}";

    public string DepartmentsLine =>
        $"{ResourceStrings.Get("MaintenanceSchedule_Column_Departments")} {DepartmentsText}";

    public string? ReportsProgressText => DepartmentIds.Count == 0
        ? null
        : string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            ResourceStrings.Get("MaintenanceSchedule_Reports_Progress"),
            ReportedDepartmentIds.Count,
            DepartmentIds.Count);

    public bool IsScheduled => Status == "scheduled";

    public bool IsOverdueStatus => Status == "overdue";

    public bool CanCancel => Status is "scheduled" or "overdue";

    public bool CanStartWork => Status is "scheduled" or "overdue";

    public bool CanCreateMaterialRequisition =>
        WorkOrderRequisitionPolicy.AllowsMaterialRequisitionSchedule(Status);

    public bool CanCreateToolRequisition =>
        WorkOrderRequisitionPolicy.AllowsToolRequisitionSchedule(Status);

    public bool CanMarkOverdue =>
        Status == "scheduled" && PlannedDate < DateOnly.FromDateTime(DateTime.Today);

    public bool CanSubmitWorkReport { get; init; }

    public bool ShowReportsProgress => DepartmentIds.Count > 0;

    public required MaintenanceScheduleViewModel Page { get; init; }

    public static MaintenanceScheduleRow FromItem(
        MaintenanceScheduleItem item,
        MaintenanceScheduleViewModel page,
        Guid? ownDepartmentId,
        bool isAdmin)
    {
        var hasDepartments = item.DepartmentIds.Count > 0;

        var isInProgress = item.Status == "in_progress";
        var canSubmit = isInProgress && hasDepartments && (
            isAdmin
                ? item.DepartmentIds.Any(d => !item.ReportedDepartmentIds.Contains(d))
                : ownDepartmentId is not null
                    && item.DepartmentIds.Contains(ownDepartmentId.Value)
                    && !item.ReportedDepartmentIds.Contains(ownDepartmentId.Value));

        return new MaintenanceScheduleRow
        {
            Id = item.Id,
            AssetId = item.AssetId,
            AssetNumber = item.AssetNumber,
            AssetName = item.AssetName,
            MaintenanceType = item.MaintenanceType,
            PlannedDate = item.PlannedDate,
            Status = item.Status,
            LastMaintenanceDate = item.LastMaintenanceDate,
            NextMaintenanceDate = item.NextMaintenanceDate,
            DepartmentNames = item.DepartmentNames,
            DepartmentIds = item.DepartmentIds,
            ReportedDepartmentIds = item.ReportedDepartmentIds,
            CanSubmitWorkReport = canSubmit,
            Page = page,
        };
    }
}
