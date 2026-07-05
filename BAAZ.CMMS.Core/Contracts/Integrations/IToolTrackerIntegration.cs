namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Контракт интеграции с системой учёта инструмента ToolTracker.
/// Реализация — в отдельном репозитории ToolTracker или в адаптере BAAZ.CMMS.Core при необходимости.
/// UC-TT1…TT4 (см. docs/use-cases/tool-tracker.md). Выдача на наряд — <see cref="ITmsIssuanceClient"/>.
/// </summary>
public interface IToolTrackerIntegration
{
    /// <summary>UC-TT1 — Проверить наличие активной задачи ТОиР для исполнителя (входящий запрос от ToolTracker).</summary>
    Task<bool> HasActiveTaskForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default);

    /// <summary>UC-TT2 — Уведомить ToolTracker о создании отчёта (исходящее событие BAAZ CMMS).</summary>
    Task NotifyWorkReportCreatedAsync(Guid workReportId, Guid? requestId, Guid? scheduleId, decimal actualDurationHours, CancellationToken cancellationToken = default);

    /// <summary>UC-TT4 — Уведомить ToolTracker об изменении статуса заявки (исходящее событие BAAZ CMMS).</summary>
    Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        DateTimeOffset changedAt,
        ToolTrackerRequestStatusContext? context = null,
        CancellationToken cancellationToken = default);
}
