using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// No-op заглушка для DI — используется пока реальный адаптер DowntimeTracker не реализован.
/// </summary>
public sealed class NullDowntimeTrackerIntegration : IDowntimeTrackerIntegration
{
    public Task NotifyRequestStatusChangedAsync(Guid requestId, Guid? assetId, string newStatus, DateTimeOffset changedAt, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyWorkReportCreatedAsync(Guid workReportId, Guid? requestId, Guid? assetId, decimal actualDurationHours, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<MaintenanceScheduleItem>> GetScheduledMaintenanceAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MaintenanceScheduleItem>>([]);

    public Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForPeriodAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WorkReportItem>>([]);
}
