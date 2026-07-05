namespace BAAZ.CMMS.Core.Models;

/// <summary>Детальная карточка нормативов ТО объекта (UC-A5, вкладка «По оборудованию»).</summary>
public sealed class AssetNormsDetail
{
    public required Guid AssetId { get; init; }

    public required string AssetNumber { get; init; }

    public required string AssetName { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public DateOnly? CommissioningDate { get; init; }

    public required IReadOnlyList<EffectiveNormSlot> Slots { get; init; }
}

public enum NormChangePolicy
{
    /// <summary>Пересчитать дату открытой позиции графика.</summary>
    RecalculatePending,

    /// <summary>Не менять открытые позиции; новый интервал — со следующего цикла.</summary>
    NextCycleOnly,

    /// <summary>Только сохранить норматив, график не менять.</summary>
    NormOnly,
}

/// <summary>Input для сохранения одного слота-override норматива объекта.</summary>
public sealed class AssetNormSlotInput
{
    public required string MaintenanceType { get; init; }

    /// <summary>true — есть индивидуальный override (строка в maintenance_norms); false — удалить override (сброс к пресету).</summary>
    public required bool HasOverride { get; init; }

    public int? IntervalDays { get; init; }

    public string? Description { get; init; }

    public bool OverrideDepartments { get; init; }

    public IReadOnlyList<Guid> DepartmentIds { get; init; } = [];

    /// <summary>Политика синхронизации графика — задаётся только если менялся interval и есть pending schedule.</summary>
    public NormChangePolicy? Policy { get; init; }
}

public sealed class AssetNormOverridesInput
{
    public required Guid AssetId { get; init; }

    public required IReadOnlyList<AssetNormSlotInput> Slots { get; init; }
}
