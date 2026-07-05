namespace BAAZ.CMMS.Core.Models.TmsIssuance;

/// <summary>Ответ TMS-API-1 (единый формат, всегда <see cref="TmsRequisitionStatuses.New"/>).</summary>
public sealed class ToolRequisitionCreateResult
{
    public required Guid RequisitionId { get; init; }

    /// <summary>Человекочитаемый номер заявки TMS (из ответа API или производный от <see cref="RequisitionId"/>).</summary>
    public string? RequisitionNumber { get; init; }

    public required Guid ClientReferenceId { get; init; }

    public required Guid WarehouseId { get; init; }

    public string? WarehouseName { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required IReadOnlyList<ToolRequisitionLineResult> Lines { get; init; }
}

public sealed class ToolRequisitionLineResult
{
    public required Guid LineId { get; init; }

    public required Guid LineClientId { get; init; }

    public required string LineStatus { get; init; }

    public required ToolRequisitionLineKind Kind { get; init; }

    public Guid? ToolId { get; init; }

    public string? Description { get; init; }
}
