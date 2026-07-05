namespace BAAZ.CMMS.Core.Models;

using System.Collections.Generic;

public sealed class WorkReportInput
{
    public Guid? RequestId { get; init; }
    public Guid? ScheduleId { get; init; }
    /// <summary>Для admin при сдаче отчёта по ППР — выбранный отдел из назначенных.</summary>
    public Guid? RepairDepartmentId { get; init; }
    public required Guid TechnicianId { get; init; }
    public required string WorkPerformed { get; init; }
    public decimal ActualDurationHours { get; init; }
    public string? PartsUsed { get; init; }
    public string? DefectsFound { get; init; }
    public string? Notes { get; init; }
    public string? MaintenanceType { get; init; }
    public IReadOnlyList<string>? MaintenanceTypes { get; init; }
}
