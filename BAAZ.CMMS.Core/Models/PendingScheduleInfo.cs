namespace BAAZ.CMMS.Core.Models;

/// <summary>Ближайшая открытая позиция графика ППР — для предупреждающего бейджа в UI (до save).</summary>
public sealed class PendingScheduleInfo
{
    public required DateOnly PlannedDate { get; init; }

    /// <summary>"scheduled" | "overdue" | "in_progress".</summary>
    public required string Status { get; init; }
}
