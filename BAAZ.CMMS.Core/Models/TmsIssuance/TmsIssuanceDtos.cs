namespace BAAZ.CMMS.Core.Models.TmsIssuance;

public sealed class TmsWorkOrderRef
{
    public required TmsWorkOrderKind Kind { get; init; }

    public required Guid Id { get; init; }
}

public enum TmsToolAvailability
{
    All,
    Available,
}

public enum TmsRequisitionFields
{
    Summary,
    Full,
}

public sealed class TmsWarehouseListItem
{
    public required Guid WarehouseId { get; init; }

    public required string Name { get; init; }
}

public sealed class TmsWarehouseListResult
{
    public required IReadOnlyList<TmsWarehouseListItem> Warehouses { get; init; }

    public string? CatalogVersion { get; init; }

    public string? ETag { get; init; }
}

public sealed class TmsToolCatalogItem
{
    public required Guid ToolId { get; init; }

    public required string Name { get; init; }

    public string? ToolTypeName { get; init; }

    public int? QuantityAvailable { get; init; }

    public int? QuantityTotal { get; init; }
}

public sealed class TmsToolCatalogResult
{
    public required Guid WarehouseId { get; init; }

    public required TmsToolAvailability Availability { get; init; }

    public string? CatalogVersion { get; init; }

    public string? ETag { get; init; }

    public required IReadOnlyList<TmsToolCatalogItem> Items { get; init; }
}

public sealed class TmsRequisitionLinesSummary
{
    public int Total { get; init; }

    public int Pending { get; init; }

    public int Reserved { get; init; }

    public int Issued { get; init; }

    public int Returned { get; init; }
}

public sealed class TmsRequisitionSummaryItem
{
    public required Guid RequisitionId { get; init; }

    public required Guid WarehouseId { get; init; }

    public string? WarehouseName { get; init; }

    public required string Status { get; init; }

    public string? CancelledBy { get; init; }

    public DateTimeOffset? ReadyAt { get; init; }

    public DateTimeOffset? IssuedAt { get; init; }

    public DateTimeOffset? ReturnedAt { get; init; }

    public TmsRequisitionLinesSummary? LinesSummary { get; init; }
}

public sealed class TmsRequisitionListResult
{
    public TmsWorkOrderRef? WorkOrder { get; init; }

    public required IReadOnlyList<TmsRequisitionSummaryItem> Requisitions { get; init; }

    public string? ETag { get; init; }

    public bool NotModified { get; init; }
}

public sealed class TmsRequisitionDetailResult
{
    public required TmsRequisitionSummaryItem Requisition { get; init; }

    public IReadOnlyList<ToolRequisitionLineResult>? Lines { get; init; }

    public ToolRequisitionWorkOrderSnapshot? WorkOrder { get; init; }

    public ToolRequisitionTechnicianSnapshot? Technician { get; init; }

    public string? ETag { get; init; }

    public bool NotModified { get; init; }
}

public sealed class TmsCancelRequisitionsInput
{
    public Guid? CmmsRequestId { get; init; }

    public Guid? CmmsScheduleId { get; init; }

    public IReadOnlyList<Guid>? RequisitionIds { get; init; }

    public string? Reason { get; init; }
}

public sealed class TmsCancelRequisitionOutcome
{
    public required Guid RequisitionId { get; init; }

    public required string Status { get; init; }

    public string? SkipReason { get; init; }
}

public sealed class TmsCancelRequisitionsResult
{
    public required IReadOnlyList<TmsCancelRequisitionOutcome> Cancelled { get; init; }

    public required IReadOnlyList<TmsCancelRequisitionOutcome> Skipped { get; init; }
}
