namespace BAAZ.CMMS.Core.Models.TmsIssuance;

/// <summary>Тело TMS-API-1. См. docs/use-cases/tms-tool-issuance-proposal.md.</summary>
public sealed class ToolRequisitionInput
{
    public required Guid ClientReferenceId { get; init; }

    public required Guid WarehouseId { get; init; }

    public required ToolRequisitionWorkOrderSnapshot WorkOrder { get; init; }

    public required ToolRequisitionTechnicianSnapshot Technician { get; init; }

    public required IReadOnlyList<ToolRequisitionLineInput> Lines { get; init; }

    public string? Notes { get; init; }
}
