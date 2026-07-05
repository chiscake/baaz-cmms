namespace BAAZ.CMMS.Core.Models;

public sealed class RepairDepartmentListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Code { get; init; }
}
