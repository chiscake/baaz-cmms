using System;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Dispatcher.WorkReports;

public sealed class WorkReportsRow
{
    public required Guid Id { get; init; }

    public Guid? RequestId { get; init; }

    public Guid? ScheduleId { get; init; }

    public required string RepairDepartmentName { get; init; }

    public required string TechnicianName { get; init; }

    public required string WorkPerformed { get; init; }

    public required string DurationText { get; init; }

    public required string SourceText { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string CreatedAtText => DateTimeDisplayHelper.Format(CreatedAt);

    public bool IsRequestSource => RequestId is not null;

    public bool IsScheduleSource => ScheduleId is not null;

    public required WorkReportsViewModel Page { get; init; }

    public static WorkReportsRow FromItem(WorkReportItem item, WorkReportsViewModel page)
    {
        var sourceText = item.RequestId is not null
            ? string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                ResourceStrings.Get("WorkReports_Source_Request"),
                item.RequestNumber ?? item.RequestId.Value.ToString())
            : string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                ResourceStrings.Get("WorkReports_Source_Schedule"),
                item.ScheduleAssetName ?? ResourceStrings.Get("Common_None"),
                item.ScheduleAssetNumber ?? string.Empty,
                MaintenanceTypeLabels.Get(item.ScheduleMaintenanceType ?? string.Empty));

        return new WorkReportsRow
        {
            Id = item.Id,
            RequestId = item.RequestId,
            ScheduleId = item.ScheduleId,
            RepairDepartmentName = item.RepairDepartmentName ?? string.Empty,
            TechnicianName = item.TechnicianName,
            WorkPerformed = item.WorkPerformed,
            DurationText = $"{item.ActualDurationHours} ч",
            SourceText = sourceText,
            CreatedAt = item.CreatedAt,
            Page = page,
        };
    }
}
