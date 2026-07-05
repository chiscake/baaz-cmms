namespace BAAZ.CMMS.Core.Models;

public sealed class CreateScheduleInput
{
    public Guid AssetId { get; init; }

    public required string MaintenanceType { get; init; }

    public DateOnly PlannedDate { get; init; }

    public IReadOnlyList<Guid>? DepartmentIds { get; init; }
}
