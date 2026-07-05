using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed class MaintenanceScheduleAssetFilterOption
{
    public Guid? AssetId { get; init; }

    public required string Label { get; init; }
}

public sealed class MaintenanceScheduleTypeFilterOption
{
    public string? MaintenanceType { get; init; }

    public required string Label { get; init; }
}

public sealed class MaintenanceScheduleDepartmentFilterOption
{
    public Guid? DepartmentId { get; init; }

    public required string Label { get; init; }
}

public sealed class MaintenanceScheduleStatusFilterOption
{
    public string? Status { get; init; }

    public required string Label { get; init; }
}

public enum MaintenanceScheduleSortOption
{
    PlannedDateAsc,
    PlannedDateDesc,
    AssetName,
    AssetNumber,
    Status,
    MaintenanceType,
}
