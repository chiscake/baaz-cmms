namespace BAAZ.CMMS.Core.Models;

/// <summary>Отдел, задействованный в заявке (request_repair_departments), со своим исполнителем.</summary>
public sealed class RequestDepartmentItem
{
    public Guid RepairDepartmentId { get; init; }
    public required string DepartmentName { get; init; }
    public Guid? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public DateTimeOffset AddedAt { get; init; }
}
