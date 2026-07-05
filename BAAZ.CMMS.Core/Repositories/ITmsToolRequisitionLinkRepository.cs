using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Repositories;

public interface ITmsToolRequisitionLinkRepository
{
    Task<DataResult<TmsToolRequisitionLinkModel>> InsertAsync(TmsToolRequisitionLinkModel model, CancellationToken ct = default);

    Task<DataResult<TmsToolRequisitionLinkModel?>> GetByClientReferenceIdAsync(Guid clientReferenceId, CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListByWorkOrderAsync(TmsWorkOrderRef workOrder, CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListAllAsync(int limit = 500, CancellationToken ct = default);

    Task<DataResult<TmsToolRequisitionLinkModel?>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<TmsToolRequisitionLinkModel?>> FindBlockingDuplicateAsync(
        TmsWorkOrderRef workOrder,
        Guid warehouseId,
        CancellationToken ct = default);

    Task<DataResult<TmsToolRequisitionLinkModel>> UpdateSyncStateAsync(
        Guid id,
        string lastKnownStatus,
        string? syncEtag,
        DateTimeOffset syncedAt,
        CancellationToken ct = default);

    Task<DataResult> UpdateStatusByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        string lastKnownStatus,
        CancellationToken ct = default);
}
