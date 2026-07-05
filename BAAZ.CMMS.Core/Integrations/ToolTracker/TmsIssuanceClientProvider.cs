using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

/// <summary>
/// Делегирует вызовы текущему Mock/Live-клиенту; пересоздаётся при смене настроек TMS без перезапуска приложения.
/// </summary>
public sealed class TmsIssuanceClientProvider : ITmsIssuanceClient
{
    private readonly object _gate = new();
    private ITmsIssuanceClient _inner;

    public TmsIssuanceClientProvider()
    {
        _inner = CreateInner();
    }

    public void Refresh()
    {
        lock (_gate)
            _inner = CreateInner();
    }

    public Task<DataResult<ToolRequisitionCreateResult>> CreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
        => Current().CreateRequisitionAsync(input, cancellationToken);

    public Task<DataResult<TmsWarehouseListResult>> GetWarehousesAsync(
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Current().GetWarehousesAsync(ifNoneMatch, cancellationToken);

    public Task<DataResult<TmsToolCatalogResult>> GetToolsAsync(
        Guid warehouseId,
        TmsToolAvailability availability = TmsToolAvailability.Available,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Current().GetToolsAsync(warehouseId, availability, ifNoneMatch, cancellationToken);

    public Task<DataResult<TmsRequisitionListResult>> GetRequisitionsByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        TmsRequisitionFields fields = TmsRequisitionFields.Summary,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Current().GetRequisitionsByWorkOrderAsync(workOrder, fields, ifNoneMatch, cancellationToken);

    public Task<DataResult<TmsRequisitionDetailResult>> GetRequisitionAsync(
        Guid requisitionId,
        TmsRequisitionFields fields = TmsRequisitionFields.Full,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Current().GetRequisitionAsync(requisitionId, fields, ifNoneMatch, cancellationToken);

    public Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionsAsync(
        TmsCancelRequisitionsInput input,
        CancellationToken cancellationToken = default)
        => Current().CancelRequisitionsAsync(input, cancellationToken);

    private ITmsIssuanceClient Current()
    {
        lock (_gate)
            return _inner;
    }

    private static ITmsIssuanceClient CreateInner()
    {
        var sender = TmsIntegrationSettings.CreateOutboundSender();
        return TmsIntegrationSettings.CreateIssuanceClient(sender);
    }
}
