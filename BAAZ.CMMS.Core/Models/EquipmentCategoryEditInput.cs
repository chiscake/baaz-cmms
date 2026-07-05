namespace BAAZ.CMMS.Core.Models;

public sealed class EquipmentCategoryEditInput
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}
