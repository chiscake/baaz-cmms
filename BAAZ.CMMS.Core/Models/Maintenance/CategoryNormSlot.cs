namespace BAAZ.CMMS.Core.Models;

/// <summary>Один из 3 фиксированных слотов ТО (to1/to2/kr) пресета категории.</summary>
public sealed class CategoryNormSlot
{
    public required string MaintenanceType { get; init; }

    /// <summary>Слот включён (есть строка в category_maintenance_norms).</summary>
    public bool IsEnabled { get; init; }

    public Guid? NormId { get; init; }

    public int? IntervalDays { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<Guid> DepartmentIds { get; init; } = [];
}

/// <summary>Input для сохранения одного слота пресета категории.</summary>
public sealed class CategoryNormSlotInput
{
    public required string MaintenanceType { get; init; }

    public required bool IsEnabled { get; init; }

    public int? IntervalDays { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<Guid> DepartmentIds { get; init; } = [];
}
