namespace BAAZ.CMMS.Core.Models;

public sealed class MaterialRequisitionResult
{
    public required Guid RequisitionId { get; init; }

    public required string RequisitionNumber { get; init; }

    public required string SavedFilePath { get; init; }
}
