using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Models;

public sealed class WorkReportDisplayItem
{
    public required string RepairDepartmentName { get; init; }

    public required string TechnicianName { get; init; }

    public required string WorkPerformed { get; init; }

    public required string DurationText { get; init; }

    public string? DefectsFound { get; init; }

    public string? Notes { get; init; }

    public string? MaintenanceTypesText { get; init; }

    public required string CreatedAtText { get; init; }

    public static WorkReportDisplayItem From(WorkReportItem item) => new()
    {
        RepairDepartmentName = item.RepairDepartmentName ?? string.Empty,
        TechnicianName = item.TechnicianName,
        WorkPerformed = item.WorkPerformed,
        DurationText = $"{item.ActualDurationHours} ч",
        DefectsFound = item.DefectsFound,
        Notes = item.Notes,
        MaintenanceTypesText = FormatMaintenanceTypes(item),
        CreatedAtText = DateTimeDisplayHelper.Format(item.CreatedAt),
    };

    private static string? FormatMaintenanceTypes(WorkReportItem item)
    {
        IReadOnlyList<string>? types = item.MaintenanceTypes is { Count: > 0 }
            ? item.MaintenanceTypes
            : string.IsNullOrWhiteSpace(item.MaintenanceType)
                ? null
                : (IReadOnlyList<string>)[item.MaintenanceType!];

        if (types is null or { Count: 0 })
            return null;

        return string.Join(", ", types.Select(MaintenanceTypeLabels.Get));
    }
}
