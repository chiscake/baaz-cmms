using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>No-op заглушка — пока реальный адаптер TMS не реализован.</summary>
public sealed class NullTmsIssuanceClient : ITmsIssuanceClient
{
    public Task<DataResult<ToolRequisitionCreateResult>> CreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<ToolRequisitionCreateResult>.Fail(
            DataError.Unknown("TMS issuance client is not configured")));

    public Task<DataResult<TmsWarehouseListResult>> GetWarehousesAsync(
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsWarehouseListResult>.Ok(new TmsWarehouseListResult
        {
            Warehouses = Array.Empty<TmsWarehouseListItem>(),
        }));

    public Task<DataResult<TmsToolCatalogResult>> GetToolsAsync(
        Guid warehouseId,
        TmsToolAvailability availability = TmsToolAvailability.Available,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsToolCatalogResult>.Ok(new TmsToolCatalogResult
        {
            WarehouseId = warehouseId,
            Availability = availability,
            Items = Array.Empty<TmsToolCatalogItem>(),
        }));

    public Task<DataResult<TmsRequisitionListResult>> GetRequisitionsByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        TmsRequisitionFields fields = TmsRequisitionFields.Summary,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsRequisitionListResult>.Ok(new TmsRequisitionListResult
        {
            WorkOrder = workOrder,
            Requisitions = Array.Empty<TmsRequisitionSummaryItem>(),
        }));

    public Task<DataResult<TmsRequisitionDetailResult>> GetRequisitionAsync(
        Guid requisitionId,
        TmsRequisitionFields fields = TmsRequisitionFields.Full,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsRequisitionDetailResult>.Fail(
            DataError.Unknown("TMS issuance client is not configured")));

    public Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionsAsync(
        TmsCancelRequisitionsInput input,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsCancelRequisitionsResult>.Ok(new TmsCancelRequisitionsResult
        {
            Cancelled = Array.Empty<TmsCancelRequisitionOutcome>(),
            Skipped = Array.Empty<TmsCancelRequisitionOutcome>(),
        }));
}
