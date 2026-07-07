namespace BAAZ.CMMS.Core.Models;

public sealed class CreateRequestInput
{
    public required Guid RequesterId { get; init; }

    public required string Type { get; init; }

    public required string Priority { get; init; }

    public required string Title { get; init; }

    public string Description { get; init; } = string.Empty;

    public required string LocationDescription { get; init; }

    public Guid? AssetId { get; init; }

    public required Guid TargetRepairDepartmentId { get; init; }

    public string? RepairZone { get; init; }

    public string? ContractorName { get; init; }
}
