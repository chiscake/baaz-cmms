namespace BAAZ.CMMS.Core.Models;

public sealed record LocationListItem
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Code { get; init; }

    public Guid? ParentId { get; init; }

    public bool IsActive { get; init; } = true;

    public string? FullPath { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
