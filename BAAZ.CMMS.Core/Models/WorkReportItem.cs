namespace BAAZ.CMMS.Core.Models;

using System.Collections.Generic;

public sealed class WorkReportItem
{
    public Guid Id { get; init; }
    public Guid? RequestId { get; init; }
    public Guid? ScheduleId { get; init; }
    public Guid RepairDepartmentId { get; init; }
    public string? RepairDepartmentName { get; init; }
    public required string TechnicianName { get; init; }
    public required string WorkPerformed { get; init; }
    public decimal ActualDurationHours { get; init; }
    public string? DefectsFound { get; init; }
    public string? Notes { get; init; }
    public string? MaintenanceType { get; init; }
    public IReadOnlyList<string>? MaintenanceTypes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? RequestNumber { get; init; }
    public string? ScheduleAssetName { get; init; }
    public string? ScheduleAssetNumber { get; init; }
    public string? ScheduleMaintenanceType { get; init; }
}
