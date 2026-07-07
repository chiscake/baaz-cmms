namespace BAAZ.CMMS.Core.Models;

/// <summary>Строка аудита «Все нормативы» (UC-A5, плоский список effective-нормативов).</summary>
public sealed class MaintenanceNormItem
{
    public Guid AssetId { get; init; }

    public required string AssetNumber { get; init; }

    public required string AssetName { get; init; }

    public string? CategoryName { get; init; }

    public required string MaintenanceType { get; init; }

    public int IntervalDays { get; init; }

    public bool IsIntervalOverridden { get; init; }

    public DateOnly? NextMaintenanceDate { get; init; }
}
