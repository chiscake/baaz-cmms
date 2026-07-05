namespace BAAZ.CMMS.Core.Models;

public sealed class MaintenanceScheduleItem
{
    public Guid Id { get; init; }
    public Guid AssetId { get; init; }
    public Guid? LocationId { get; init; }
    public required string AssetName { get; init; }
    public required string AssetNumber { get; init; }
    public required string MaintenanceType { get; init; }
    public DateOnly PlannedDate { get; init; }
    public required string Status { get; init; }
    public DateOnly? LastMaintenanceDate { get; init; }
    public DateOnly? NextMaintenanceDate { get; init; }
    public IReadOnlyList<string> DepartmentNames { get; init; } = [];
    public IReadOnlyList<Guid> DepartmentIds { get; init; } = [];
    public IReadOnlyList<Guid> ReportedDepartmentIds { get; init; } = [];
}
