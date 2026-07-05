using System;
using System.Collections.Generic;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

public enum ChartLaneRowKind
{
    Location,
    Asset,
}

public sealed class ChartLaneRowVm
{
    public required ChartLaneRowKind Kind { get; init; }

    public required Guid Id { get; init; }

    public Guid? ParentLocationId { get; init; }

    public Guid? AssetId { get; init; }

    public required string Label { get; init; }

    public int IndentLevel { get; init; }

    public bool HasChildren { get; init; }

    public bool IsCollapsed { get; init; }

    public IReadOnlyList<ChartMarkerVm> Markers { get; init; } = [];
}

public sealed class ChartMarkerVm
{
    public required Guid ScheduleId { get; init; }

    public required Guid AssetId { get; init; }

    public required DateOnly PlannedDate { get; init; }

    public required string Status { get; init; }

    public required string StatusBrushKey { get; init; }

    public required string MaintenanceTypeLabel { get; init; }

    public required string AssetName { get; init; }

    public required string StatusLabel { get; init; }

    public int EventCount { get; init; } = 1;

    public IReadOnlyList<ChartMarkerVm> SameDayEvents { get; init; } = [];
}

public sealed class ChartDayHeaderVm
{
    public required DateOnly Date { get; init; }

    public required string DayLabel { get; init; }

    public string? MonthLabel { get; init; }

    public bool IsWeekend { get; init; }

    public bool IsToday { get; init; }

    public double Left { get; init; }

    public double Width { get; init; }
}

public sealed class ChartHeatSegmentVm
{
    public required DateOnly Date { get; init; }

    public string? StatusBrushKey { get; init; }

    public double Left { get; init; }

    public double Width { get; init; }
}
