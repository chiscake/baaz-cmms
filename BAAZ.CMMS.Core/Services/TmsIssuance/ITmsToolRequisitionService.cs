using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Services.TmsIssuance;

public interface ITmsToolRequisitionService
{
    /// <summary>TMS-API-1 + запись в <c>tms_tool_requisition_links</c>.</summary>
    Task<DataResult<ToolRequisitionCreateResult>> CreateAndPersistAsync(
        ToolRequisitionInput input,
        Guid createdByProfileId,
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListLocalByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListAllLocalAsync(
        int limit = 500,
        CancellationToken cancellationToken = default);

    Task<DataResult<TmsToolRequisitionLinkModel?>> GetLocalByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>TMS-API-5 list-by-work-order + обновление локальных <c>last_known_status</c> / <c>sync_etag</c>.</summary>
    Task<DataResult<TmsRequisitionListResult>> RefreshWorkOrderStatusAsync(
        TmsWorkOrderRef workOrder,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default);

    /// <summary>TMS-API-2 batch + локальное обновление отменённых заявок.</summary>
    Task<DataResult<TmsCancelRequisitionsResult>> CancelForWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
