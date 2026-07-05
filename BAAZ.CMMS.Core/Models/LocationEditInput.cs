namespace BAAZ.CMMS.Core.Models;

public sealed class LocationEditInput
{
    public required string Name { get; init; }
    public string? Code { get; init; }
    public Guid? ParentId { get; init; }
}
