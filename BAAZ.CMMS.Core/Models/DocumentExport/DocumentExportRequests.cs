using System.Text.Json;

namespace BAAZ.CMMS.Core.Models.DocumentExport;

public sealed class WorkReportDocumentRequest
{
    public required Guid ReportId { get; init; }

    public required string RepairDepartmentName { get; init; }

    public required string TechnicianName { get; init; }

    public required string AuthorName { get; init; }

    public required string WorkPerformed { get; init; }

    public decimal ActualDurationHours { get; init; }

    public string? DefectsFound { get; init; }

    public string? Notes { get; init; }

    public string? PartsUsed { get; init; }

    public string? MaintenanceTypesText { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string? RequestNumber { get; init; }

    public string? RequestTitle { get; init; }

    public string? AssetName { get; init; }

    public string? AssetNumber { get; init; }

    public string? ScheduleMaintenanceType { get; init; }

    public DateOnly? SchedulePlannedDate { get; init; }

    public bool IsRequestSource => RequestNumber is not null;
}

public sealed class RepairRequestDocumentRequest
{
    public required Guid RequestId { get; init; }

    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string TypeLabel { get; init; }

    public required string PriorityLabel { get; init; }

    public required string RepairZoneLabel { get; init; }

    public required string StatusLabel { get; init; }

    public required string LocationDescription { get; init; }

    public string? AssetDisplay { get; init; }

    public string? InventoryDisplay { get; init; }

    public string? RequesterName { get; init; }

    public string? ContractorName { get; init; }

    public string? TargetDepartmentName { get; init; }

    public IReadOnlyList<RepairRequestDepartmentLine> Departments { get; init; } = [];

    public required string AuthorFullName { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class RepairRequestDepartmentLine
{
    public required string DepartmentName { get; init; }

    public string? AssigneeName { get; init; }
}

public sealed class RequestCardDocumentRequest
{
    public required RepairRequestDocumentRequest Request { get; init; }

    public IReadOnlyList<RequestCardHistoryLine> History { get; init; } = [];

    public IReadOnlyList<RequestCardWorkReportSummary> WorkReports { get; init; } = [];
}

public sealed class RequestCardHistoryLine
{
    public required string ChangedAtText { get; init; }

    public required string ChangedByName { get; init; }

    public required string OldStatusLabel { get; init; }

    public required string NewStatusLabel { get; init; }

    public string? Comment { get; init; }
}

public sealed class RequestCardWorkReportSummary
{
    public required string DepartmentName { get; init; }

    public required string TechnicianName { get; init; }

    public required string CreatedAtText { get; init; }
}

public sealed class PprWorkOrderDocumentRequest
{
    public required Guid ScheduleId { get; init; }

    public required string AssetName { get; init; }

    public required string AssetNumber { get; init; }

    public required string MaintenanceTypeLabel { get; init; }

    public DateOnly PlannedDate { get; init; }

    public required string StatusLabel { get; init; }

    public string DepartmentNames { get; init; } = string.Empty;

    public string? WorkDescription { get; init; }

    public DateOnly? LastMaintenanceDate { get; init; }

    public DateOnly? NextMaintenanceDate { get; init; }

    public required string AuthorFullName { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class MaintenanceScheduleExcelRequest
{
    public required IReadOnlyList<MaintenanceScheduleExcelRow> Rows { get; init; }

    public required string PeriodLabel { get; init; }

    public string? FiltersSummary { get; init; }

    public DateTime GeneratedAt { get; init; }
}

public sealed class MaintenanceScheduleExcelRow
{
    public required string AssetNumber { get; init; }

    public required string AssetName { get; init; }

    public required string MaintenanceTypeLabel { get; init; }

    public DateOnly PlannedDate { get; init; }

    public required string StatusLabel { get; init; }

    public required string Status { get; init; }

    public string DepartmentNames { get; init; } = string.Empty;

    public string? LastMaintenanceDate { get; init; }

    public string? NextMaintenanceDate { get; init; }
}

public static class WorkReportPartsUsedFormatter
{
    public static string? Format(JsonElement? element)
    {
        if (element is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
            return null;

        return element.Value.ValueKind switch
        {
            JsonValueKind.String => element.Value.GetString(),
            _ => element.Value.GetRawText(),
        };
    }
}
