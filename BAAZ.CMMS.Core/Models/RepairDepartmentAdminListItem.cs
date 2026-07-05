namespace BAAZ.CMMS.Core.Models;

public sealed class RepairDepartmentAdminListItem
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Code { get; init; }

    public required bool IsActive { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
