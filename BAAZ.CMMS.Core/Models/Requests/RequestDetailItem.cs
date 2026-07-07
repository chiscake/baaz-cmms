namespace BAAZ.CMMS.Core.Models;

public sealed class RequestDetailItem
{
    public Guid Id { get; init; }

    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Type { get; init; }

    public required string Priority { get; init; }

    public required string RepairZone { get; init; }

    public string? ContractorName { get; init; }

    public required string Status { get; init; }

    public required string LocationDescription { get; init; }

    public Guid? AssetId { get; init; }

    public string? AssetNumber { get; init; }

    public string? AssetName { get; init; }

    public Guid? InventoryId { get; init; }

    public string? InventoryKind { get; init; }

    public string? InventoryName { get; init; }

    public string? InventorySerial { get; init; }

    public string? InventoryTypeName { get; init; }

    public string? InventoryHandoffMode { get; init; }

    public string? InventoryWarehouseName { get; init; }

    public DateTimeOffset? InventoryReceivedAt { get; init; }

    public bool IsInventoryRequest => InventoryId is not null;

    public bool IsPickupHandoff =>
        string.Equals(InventoryHandoffMode, "pickup_at_warehouse", StringComparison.Ordinal);

    public bool IsDeliverHandoff =>
        string.Equals(InventoryHandoffMode, "deliver_to_department", StringComparison.Ordinal);

    public string? RequesterName { get; init; }

    /// <summary>Отделы, задействованные в заявке, со своими исполнителями (request_repair_departments).</summary>
    public IReadOnlyList<RequestDepartmentItem> Departments { get; init; } = [];

    /// <summary>Имена исполнителей всех задействованных отделов, объединённые через запятую.</summary>
    public string? AssigneeName { get; init; }

    /// <summary>Отдел, выбранный заявителем при создании (до accept).</summary>
    public Guid? TargetRepairDepartmentId { get; init; }

    public string? TargetRepairDepartmentName { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
