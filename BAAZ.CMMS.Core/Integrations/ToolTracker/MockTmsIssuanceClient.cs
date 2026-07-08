using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class MockTmsIssuanceClient(ITmsIssuanceOutboundSender outboundSender) : ITmsIssuanceClient
{
    private readonly ITmsIssuanceOutboundSender _outboundSender = outboundSender;

    public Task<DataResult<ToolRequisitionCreateResult>> CreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
        => _outboundSender.SendCreateRequisitionAsync(input, cancellationToken);

    public Task<DataResult<TmsWarehouseListResult>> GetWarehousesAsync(
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixture = TmsFixtureLoader.Load<TmsWarehouseListResult>("warehouses.json");
        return Task.FromResult(DataResult<TmsWarehouseListResult>.Ok(fixture));
    }

    public Task<DataResult<TmsToolCatalogResult>> GetToolsAsync(
        Guid warehouseId,
        TmsToolAvailability availability = TmsToolAvailability.Available,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixture = TmsFixtureLoader.Load<TmsToolCatalogResult>("warehouse_catalog.json");
        var items = fixture.Items;
        if (availability == TmsToolAvailability.Available)
            items = items.Where(i => i.QuantityAvailable > 0).ToList();

        return Task.FromResult(DataResult<TmsToolCatalogResult>.Ok(new TmsToolCatalogResult
        {
            WarehouseId = warehouseId,
            Availability = availability,
            CatalogVersion = fixture.CatalogVersion,
            ETag = fixture.ETag,
            Items = items,
        }));
    }

    public Task<DataResult<TmsRequisitionListResult>> GetRequisitionsByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        TmsRequisitionFields fields = TmsRequisitionFields.Summary,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixture = TmsFixtureLoader.Load<TmsRequisitionListResult>("tool_requisition_status.json");
        return Task.FromResult(DataResult<TmsRequisitionListResult>.Ok(new TmsRequisitionListResult
        {
            WorkOrder = workOrder,
            Requisitions = fixture.Requisitions,
            ETag = fixture.ETag,
            NotModified = false,
        }));
    }

    public Task<DataResult<TmsRequisitionDetailResult>> GetRequisitionAsync(
        Guid requisitionId,
        TmsRequisitionFields fields = TmsRequisitionFields.Full,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsRequisitionDetailResult>.Fail(
            DataError.Unknown("Mock: requisition detail not implemented")));

    public Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionsAsync(
        TmsCancelRequisitionsInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (input.RequisitionIds is { Count: > 0 })
        {
            return Task.FromResult(DataResult<TmsCancelRequisitionsResult>.Ok(new TmsCancelRequisitionsResult
            {
                Cancelled = input.RequisitionIds
                    .Select(id => new TmsCancelRequisitionOutcome
                    {
                        RequisitionId = id,
                        Status = TmsRequisitionStatuses.Cancelled,
                    })
                    .ToList(),
                Skipped = [],
            }));
        }

        var fixture = TmsFixtureLoader.Load<TmsCancelRequisitionsResult>("cancel_tool_requisitions_response.json");
        return Task.FromResult(DataResult<TmsCancelRequisitionsResult>.Ok(fixture));
    }
}
