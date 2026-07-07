namespace BAAZ.CMMS.Core.Models;

public sealed class RequestListItem
{
    public Guid Id { get; init; }
    public required string RequestNumber { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string Status { get; init; }
    public required string Priority { get; init; }
    public required string Type { get; init; }
    public string LocationDescription { get; init; } = string.Empty;
    public string? AssetNumber { get; init; }
    public string? AssetName { get; init; }
    public string RepairZone { get; init; } = string.Empty;
    public string? ContractorName { get; init; }
    public string? TargetDepartmentName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Имена исполнителей всех задействованных отделов, объединённые через запятую.</summary>
    public string? AssigneeName { get; init; }

    public string? RequesterName { get; init; }

    /// <summary>Задействованные отделы (request_repair_departments).</summary>
    public IReadOnlyList<RequestDepartmentItem> Departments { get; init; } = [];
}
