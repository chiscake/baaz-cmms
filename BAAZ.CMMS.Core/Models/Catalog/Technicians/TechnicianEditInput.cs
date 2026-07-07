namespace BAAZ.CMMS.Core.Models;

public sealed class TechnicianEditInput
{
    public required string FullName { get; init; }

    public string? Specialty { get; init; }

    public Guid? RepairDepartmentId { get; init; }
}
