namespace BAAZ.CMMS.Core.Models;

public sealed class AssetListItem
{
    public required Guid Id { get; init; }

    public required string AssetNumber { get; init; }

    public required string Name { get; init; }

    public string? LocationName { get; init; }

    public Guid? LocationId { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public required string Status { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public string? SerialNumber { get; init; }

    public DateOnly? CommissioningDate { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
