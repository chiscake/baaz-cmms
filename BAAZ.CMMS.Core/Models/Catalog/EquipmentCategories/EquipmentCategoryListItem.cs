namespace BAAZ.CMMS.Core.Models;

public sealed class EquipmentCategoryListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
