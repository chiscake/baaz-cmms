using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;

namespace BAAZ.CMMS.Core.Repositories;

public interface IRequestRepository
{
    Task<DataResult<RequestCreatedDto>> CreateViaRpcAsync(
        RequestInsertDto row,
        Guid targetRepairDepartmentId,
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<RequestListRowDto>>> ListByRequesterAsync(
        Guid requesterId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>Все заявки (admin — RLS без фильтра по requester).</summary>
    Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>UC-D1: очередь новых заявок (status = new).</summary>
    Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListIncomingAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Заявки диспетчера с указанными статусами (видимость обеспечивает RLS).</summary>
    Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListByStatusesAsync(
        IReadOnlyCollection<string> statuses,
        CancellationToken cancellationToken = default);

    Task<DataResult<RequestDetailRowDto?>> GetDetailByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DataResult> UpdateFieldsAsync(
        Guid requestId,
        RequestPatchDto patch,
        CancellationToken cancellationToken = default);

    Task<DataResult<RequestStatusRowDto?>> GetStatusForRequesterAsync(
        Guid requestId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    Task<DataResult> UpdateStatusAsync(
        Guid requestId,
        string status,
        CancellationToken cancellationToken = default);

    Task<DataResult> InsertStatusHistoryAsync(
        StatusHistoryInsertDto row,
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<StatusHistoryRowDto>>> ListStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<DataResult> CallWorkflowRpcAsync(
        string functionName,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    Task<DataResult> InsertWorkReportAsync(
        WorkReportInsertDto row,
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListWorkReportsByRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}
