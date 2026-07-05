namespace BAAZ.CMMS.Core.Models;

public sealed class ToolRequisitionDocxResult
{
    public required Guid RequisitionId { get; init; }

    public required string RequisitionNumber { get; init; }

    public required string SavedFilePath { get; init; }
}

public sealed class ToolRequisitionTmsResult
{
    public required Guid RequisitionId { get; init; }

    public required Guid ClientReferenceId { get; init; }

    public required string Status { get; init; }

    public string? WarehouseName { get; init; }
}
