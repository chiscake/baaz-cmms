namespace BAAZ.CMMS.Core.Models;

public sealed class RepairDepartmentEditInput
{
    public required string Name { get; init; }

    public string? Code { get; init; }
}
