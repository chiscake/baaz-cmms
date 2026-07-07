using System.Text.Json;
using System.Text.Json.Serialization;

namespace BAAZ.CMMS.Core.Repositories.Dtos;

public sealed class WorkReportScheduleEmbedDto
{
    [JsonPropertyName("maintenance_type")]
    public string? MaintenanceType { get; init; }

    [JsonPropertyName("assets")]
    public WorkReportAssetEmbedDto? Assets { get; init; }
}

public sealed class WorkReportAssetEmbedDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("asset_number")]
    public string? AssetNumber { get; init; }
}

public sealed class WorkReportRequestEmbedDto
{
    [JsonPropertyName("request_number")]
    public string? RequestNumber { get; init; }
}

public sealed class WorkReportListRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("request_id")]
    public Guid? RequestId { get; init; }

    [JsonPropertyName("schedule_id")]
    public Guid? ScheduleId { get; init; }

    [JsonPropertyName("repair_department_id")]
    public Guid RepairDepartmentId { get; init; }

    [JsonPropertyName("maintenance_type")]
    public string? MaintenanceType { get; init; }

    [JsonPropertyName("maintenance_types")]
    public IReadOnlyList<string>? MaintenanceTypes { get; init; }

    [JsonPropertyName("work_performed")]
    public string? WorkPerformed { get; init; }

    [JsonPropertyName("actual_duration_hours")]
    public decimal ActualDurationHours { get; init; }

    [JsonPropertyName("parts_used")]
    public JsonElement? PartsUsed { get; init; }

    [JsonPropertyName("defects_found")]
    public string? DefectsFound { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("technicians")]
    public TechnicianEmbedDto? Technicians { get; init; }

    [JsonPropertyName("repair_departments")]
    public RepairDepartmentEmbedDto? RepairDepartments { get; init; }

    [JsonPropertyName("requests")]
    public WorkReportRequestEmbedDto? Requests { get; init; }

    [JsonPropertyName("maintenance_schedule")]
    public WorkReportScheduleEmbedDto? MaintenanceSchedule { get; init; }
}
