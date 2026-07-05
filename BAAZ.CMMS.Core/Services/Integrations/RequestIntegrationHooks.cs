using System.Diagnostics;
using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Services.TmsIssuance;

namespace BAAZ.CMMS.Core.Services.Integrations;

public sealed class RequestIntegrationHooks(
    IRequestRepository requestRepository,
    ITmsToolRequisitionService tmsToolRequisitionService,
    IToolTrackerIntegration toolTrackerIntegration) : IRequestIntegrationHooks
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.Ordinal)
    {
        "closed", "rejected", "cancelled",
    };

    private readonly IRequestRepository _requestRepository = requestRepository;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService = tmsToolRequisitionService;
    private readonly IToolTrackerIntegration _toolTrackerIntegration = toolTrackerIntegration;

    public async Task AfterRequestStatusChangedAsync(
        Guid requestId,
        string previousStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(newStatus, "cancelled", StringComparison.Ordinal))
        {
            var workOrder = new TmsWorkOrderRef { Kind = TmsWorkOrderKind.Request, Id = requestId };
            _ = await _tmsToolRequisitionService.CancelForWorkOrderAsync(
                workOrder, "request_cancelled", cancellationToken);
        }

        if (string.Equals(newStatus, "in_progress", StringComparison.Ordinal))
        {
            await NotifyInventoryStatusAsync(requestId, previousStatus, newStatus, cancellationToken);
            return;
        }

        if (!TerminalStatuses.Contains(newStatus))
            return;

        await NotifyInventoryStatusAsync(requestId, previousStatus, newStatus, cancellationToken);
    }

    public async Task AfterScheduleCancelledAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var workOrder = new TmsWorkOrderRef { Kind = TmsWorkOrderKind.Schedule, Id = scheduleId };
        _ = await _tmsToolRequisitionService.CancelForWorkOrderAsync(
            workOrder, "schedule_cancelled", cancellationToken);
    }

    private async Task NotifyInventoryStatusAsync(
        Guid requestId,
        string previousStatus,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var detail = await _requestRepository.GetDetailByIdAsync(requestId, cancellationToken);
        if (!detail.IsSuccess || detail.Value?.InventoryId is null)
            return;

        var row = detail.Value;
        var context = new ToolTrackerRequestStatusContext(
            row.RequestNumber ?? string.Empty,
            previousStatus,
            row.InventoryId.Value,
            row.InventoryKind ?? "tool");

        try
        {
            await _toolTrackerIntegration.NotifyRequestStatusChangedAsync(
                requestId,
                newStatus,
                DateTimeOffset.UtcNow,
                context,
                cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RequestIntegrationHooks] REP-EVT-1 failed: {ex.Message}");
        }
    }
}
