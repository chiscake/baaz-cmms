namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// No-op заглушка для DI — используется пока реальный адаптер ToolTracker не реализован.
/// </summary>
public sealed class NullToolTrackerIntegration : IToolTrackerIntegration
{
    public Task<bool> HasActiveTaskForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task NotifyWorkReportCreatedAsync(Guid workReportId, Guid? requestId, Guid? scheduleId, decimal actualDurationHours, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyRequestStatusChangedAsync(Guid requestId, string newStatus, DateTimeOffset changedAt, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
