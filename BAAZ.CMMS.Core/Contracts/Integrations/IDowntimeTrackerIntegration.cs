using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Контракт интеграции с системой учёта простоев DowntimeTracker.
/// DowntimeTracker читает данные BAAZ CMMS напрямую через Supabase Realtime/REST.
/// Этот интерфейс покрывает исходящие события и read-only запросы, инициируемые со стороны BAAZ CMMS.
/// UC-DT1…DT4 (см. docs/use-cases/downtime-tracker.md).
/// </summary>
public interface IDowntimeTrackerIntegration
{
    /// <summary>UC-DT1/DT3 — Уведомить DowntimeTracker об изменении статуса заявки (in_progress / closed).</summary>
    Task NotifyRequestStatusChangedAsync(Guid requestId, Guid? assetId, string newStatus, DateTimeOffset changedAt, CancellationToken cancellationToken = default);

    /// <summary>UC-DT3 — Уведомить DowntimeTracker о создании отчёта о работах.</summary>
    Task NotifyWorkReportCreatedAsync(Guid workReportId, Guid? requestId, Guid? assetId, decimal actualDurationHours, CancellationToken cancellationToken = default);

    /// <summary>UC-DT2 — Получить плановые события ТО для DowntimeTracker (read-only адаптер).</summary>
    Task<IReadOnlyList<MaintenanceScheduleItem>> GetScheduledMaintenanceAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>UC-DT4 — Получить отчёты о работах за период для аналитики OEE/MTTR.</summary>
    Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForPeriodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
