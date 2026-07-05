using System;

namespace BAAZ.CMMS.Core.Models;

public sealed class TechnicianListItem
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public required string Specialty { get; init; }

    public bool IsActive { get; init; }

    public Guid? RepairDepartmentId { get; init; }

    public string? RepairDepartmentName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
