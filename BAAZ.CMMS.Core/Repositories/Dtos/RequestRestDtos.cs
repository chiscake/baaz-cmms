using System.Text.Json.Serialization;

namespace BAAZ.CMMS.Core.Repositories.Dtos;

public sealed class RequestInsertDto
{
    [JsonPropertyName("request_number")]
    public required string RequestNumber { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("priority")]
    public required string Priority { get; init; }

    [JsonPropertyName("repair_zone")]
    public required string RepairZone { get; init; }

    [JsonPropertyName("contractor_name")]
    public string? ContractorName { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("location_description")]
    public required string LocationDescription { get; init; }

    [JsonPropertyName("requester_id")]
    public required Guid RequesterId { get; init; }

    [JsonPropertyName("asset_id")]
    public Guid? AssetId { get; init; }
}

public sealed class RequestCreatedDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("request_number")]
    public string? RequestNumber { get; init; }
}

public sealed class RequestListRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("request_number")]
    public string? RequestNumber { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("priority")]
    public string? Priority { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("profiles")]
    public ProfileEmbedDto? Profiles { get; init; }

    [JsonPropertyName("target_repair_department")]
    public RepairDepartmentEmbedDto? TargetRepairDepartment { get; init; }

    [JsonPropertyName("request_repair_departments")]
    public List<RequestDepartmentEmbedDto>? RequestRepairDepartments { get; init; }
}

/// <summary>
/// Расширенный набор полей заявки — используется и для карточки (GetDetailByIdAsync),
/// и для очереди диспетчера (входящие/активные), т.к. набор embed-ов одинаковый.
/// </summary>
public sealed class RequestDetailRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("request_number")]
    public string? RequestNumber { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("priority")]
    public string? Priority { get; init; }

    [JsonPropertyName("repair_zone")]
    public string? RepairZone { get; init; }

    [JsonPropertyName("contractor_name")]
    public string? ContractorName { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("location_description")]
    public string? LocationDescription { get; init; }

    [JsonPropertyName("asset_id")]
    public Guid? AssetId { get; init; }

    [JsonPropertyName("requester_id")]
    public Guid RequesterId { get; init; }

    [JsonPropertyName("target_repair_department_id")]
    public Guid? TargetRepairDepartmentId { get; init; }

    [JsonPropertyName("target_repair_department")]
    public RepairDepartmentEmbedDto? TargetRepairDepartment { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("assets")]
    public AssetEmbedDto? Assets { get; init; }

    [JsonPropertyName("profiles")]
    public ProfileEmbedDto? Profiles { get; init; }

    [JsonPropertyName("request_repair_departments")]
    public List<RequestDepartmentEmbedDto>? RequestRepairDepartments { get; init; }
}

public sealed class RequestDepartmentEmbedDto
{
    [JsonPropertyName("repair_department_id")]
    public Guid RepairDepartmentId { get; init; }

    [JsonPropertyName("assignee_id")]
    public Guid? AssigneeId { get; init; }

    [JsonPropertyName("added_at")]
    public DateTimeOffset AddedAt { get; init; }

    [JsonPropertyName("repair_departments")]
    public RepairDepartmentEmbedDto? RepairDepartments { get; init; }

    [JsonPropertyName("technicians")]
    public TechnicianEmbedDto? Technicians { get; init; }
}

public sealed class RepairDepartmentEmbedDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed class RequestStatusRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public sealed class StatusHistoryRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("old_status")]
    public string? OldStatus { get; init; }

    [JsonPropertyName("new_status")]
    public string? NewStatus { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("profiles")]
    public ProfileEmbedDto? Profiles { get; init; }
}

public sealed class StatusHistoryInsertDto
{
    [JsonPropertyName("request_id")]
    public required Guid RequestId { get; init; }

    [JsonPropertyName("changed_by")]
    public required Guid ChangedBy { get; init; }

    [JsonPropertyName("old_status")]
    public required string OldStatus { get; init; }

    [JsonPropertyName("new_status")]
    public required string NewStatus { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

public sealed class TechnicianEmbedDto
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }
}

public sealed class AssetEmbedDto
{
    [JsonPropertyName("asset_number")]
    public string? AssetNumber { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed class ProfileEmbedDto
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }
}

public sealed class WorkReportInsertDto
{
    [JsonPropertyName("request_id")]
    public Guid? RequestId { get; init; }

    [JsonPropertyName("schedule_id")]
    public Guid? ScheduleId { get; init; }

    [JsonPropertyName("repair_department_id")]
    public required Guid RepairDepartmentId { get; init; }

    [JsonPropertyName("maintenance_type")]
    public string? MaintenanceType { get; init; }

    [JsonPropertyName("maintenance_types")]
    public IReadOnlyList<string>? MaintenanceTypes { get; init; }

    [JsonPropertyName("author_id")]
    public required Guid AuthorId { get; init; }

    [JsonPropertyName("technician_id")]
    public required Guid TechnicianId { get; init; }

    [JsonPropertyName("work_performed")]
    public required string WorkPerformed { get; init; }

    [JsonPropertyName("actual_duration_hours")]
    public decimal ActualDurationHours { get; init; }

    [JsonPropertyName("parts_used")]
    public string? PartsUsed { get; init; }

    [JsonPropertyName("defects_found")]
    public string? DefectsFound { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed class WorkReportRowDto
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
}

public sealed class RequestPatchDto
{
    [JsonPropertyName("request_number")]
    public required string RequestNumber { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("priority")]
    public required string Priority { get; init; }
}
