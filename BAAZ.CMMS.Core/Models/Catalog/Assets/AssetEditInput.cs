namespace BAAZ.CMMS.Core.Models;

public sealed class AssetEditInput
{
    public required string AssetNumber { get; init; }

    public required string Name { get; init; }

    public required Guid LocationId { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public string? SerialNumber { get; init; }

    public DateOnly? CommissioningDate { get; init; }

    public string? Description { get; init; }

    /// <summary>Только при update; create всегда active.</summary>
    public string? Status { get; init; }
}
