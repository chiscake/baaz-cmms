using System.Diagnostics;
using BAAZ.CMMS.Core.Contracts.Integrations;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class MockToolTrackerIntegration : IToolTrackerIntegration
{
    public Task<bool> HasActiveTaskForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task NotifyWorkReportCreatedAsync(
        Guid workReportId,
        Guid? requestId,
        Guid? scheduleId,
        decimal actualDurationHours,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"[MockToolTracker] WorkReport {workReportId} duration={actualDurationHours}");
        return Task.CompletedTask;
    }

    public Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        DateTimeOffset changedAt,
        ToolTrackerRequestStatusContext? context = null,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"[MockToolTracker] Request {requestId} -> {newStatus}" +
            (context?.InventoryId is { } id ? $" inventory={id}" : string.Empty));
        return Task.CompletedTask;
    }
}
