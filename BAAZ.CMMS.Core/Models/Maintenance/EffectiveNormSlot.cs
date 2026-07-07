namespace BAAZ.CMMS.Core.Models;

/// <summary>
/// Effective-норматив по виду ТО для конкретного asset (UC-A5): merge пресета
/// категории и индивидуального override + статус цикла + pending schedule.
/// </summary>
public sealed class EffectiveNormSlot
{
    public required string MaintenanceType { get; init; }

    /// <summary>Слот активен — есть effective interval (из пресета или override).</summary>
    public bool IsEnabled { get; init; }

    // Пресет категории (read-only в UI, если категория не задана — всегда null).
    public int? PresetIntervalDays { get; init; }
    public string? PresetDescription { get; init; }
    public IReadOnlyList<Guid> PresetDepartmentIds { get; init; } = [];

    // Индивидуальный override (редактируемая часть UI).
    public Guid? OverrideNormId { get; init; }
    public int? OverrideIntervalDays { get; init; }
    public string? OverrideDescription { get; init; }
    public bool OverrideDepartments { get; init; }
    public IReadOnlyList<Guid> OverrideDepartmentIds { get; init; } = [];

    // Effective (то, что реально действует).
    public int? EffectiveIntervalDays { get; init; }
    public string? EffectiveDescription { get; init; }
    public IReadOnlyList<Guid> EffectiveDepartmentIds { get; init; } = [];

    public bool IsIntervalOverridden { get; init; }
    public bool IsDescriptionOverridden { get; init; }

    // Статус цикла (asset_maintenance_status).
    public DateOnly? LastMaintenanceDate { get; init; }
    public DateOnly? NextMaintenanceDate { get; init; }

    // Pending schedule — для предупреждающего бейджа (до save).
    public PendingScheduleInfo? PendingSchedule { get; init; }
}
