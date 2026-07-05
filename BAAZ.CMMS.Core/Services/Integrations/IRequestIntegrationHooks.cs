namespace BAAZ.CMMS.Core.Services.Integrations;

public interface IRequestIntegrationHooks
{
    Task AfterRequestStatusChangedAsync(
        Guid requestId,
        string previousStatus,
        string newStatus,
        CancellationToken cancellationToken = default);

    Task AfterScheduleCancelledAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}
