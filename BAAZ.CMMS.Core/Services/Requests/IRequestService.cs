using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

/// <summary>
/// Управление заявками на ремонт и историей изменений статусов.
/// Покрывает UC-R1…R4, UC-D1, UC-D2, UC-D6.
/// </summary>
public interface IRequestService
{
    // UC-R1
    Task<CreateRequestResult?> CreateRequestAsync(CreateRequestInput input, CancellationToken cancellationToken = default);

    // UC-R2
    Task<IReadOnlyList<RequestListItem>> GetMyRequestsAsync(
        Guid requesterId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<RequestDetailItem?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>Все заявки (admin).</summary>
    Task<IReadOnlyList<RequestListItem>> GetAllRequestsAsync(
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>Редактирование полей заявки (admin).</summary>
    Task<DataResult> UpdateRequestFieldsAsync(
        Guid requestId,
        RequestEditInput input,
        CancellationToken cancellationToken = default);

    // UC-R3
    Task<bool> CancelRequestAsync(Guid requestId, Guid actorId, string? comment = null, CancellationToken cancellationToken = default);

    // UC-R4 (requester приёмка)
    Task<bool> CloseRequestAsync(Guid requestId, Guid actorId, CancellationToken cancellationToken = default);

    // UC-D1: очередь новых заявок
    Task<IReadOnlyList<RequestListItem>> GetIncomingRequestsAsync(CancellationToken cancellationToken = default);

    // Диспетчерская очередь по статусам (активные/завершённые заявки своего отдела)
    Task<IReadOnlyList<RequestListItem>> GetRequestsByStatusesAsync(
        IReadOnlyCollection<string> statuses,
        CancellationToken cancellationToken = default);

    // UC-D1: принять заявку в свой отдел, опционально назначить техника
    Task<DataResult> AcceptRequestAsync(Guid requestId, Guid? technicianId = null, string? comment = null, CancellationToken cancellationToken = default);

    // UC-D1 (reject)
    Task<DataResult> RejectRequestAsync(Guid requestId, Guid actorId, string? comment = null, CancellationToken cancellationToken = default);

    // UC-D2: назначить/сменить исполнителя в рамках своего отдела (dispatcher) или указанного отдела заявки (admin)
    Task<DataResult> AssignRequestAsync(
        Guid requestId,
        Guid technicianId,
        Guid actorId,
        Guid? repairDepartmentId = null,
        CancellationToken cancellationToken = default);

    // После осмотра: передать заявку целиком в другой отдел
    Task<DataResult> TransferDepartmentAsync(Guid requestId, Guid newDepartmentId, string? comment = null, CancellationToken cancellationToken = default);

    // После осмотра: подключить дополнительный отдел для совместной работы
    Task<DataResult> AddDepartmentAsync(Guid requestId, Guid departmentId, Guid? technicianId = null, string? comment = null, CancellationToken cancellationToken = default);

    // После осмотра: сменить зону ремонта (не меняет status)
    Task<DataResult> UpdateRepairZoneAsync(Guid requestId, string repairZone, string? contractorName = null, string? comment = null, CancellationToken cancellationToken = default);

    // UC-D2: начать работы (accepted → in_progress) — только для заявок на ОС/локацию
    Task<DataResult> StartWorkAsync(Guid requestId, string? comment = null, CancellationToken cancellationToken = default);

    // Контур А: подтверждение получения inventory-инструмента (accepted → in_progress)
    Task<DataResult> ConfirmInventoryReceivedAsync(Guid requestId, string? comment = null, CancellationToken cancellationToken = default);

    // UC-R4 делегирование: закрытие заявки диспетчером/admin (completed → closed)
    Task<DataResult> CloseRequestAsStaffAsync(Guid requestId, string? comment = null, CancellationToken cancellationToken = default);

    // UC-D6
    Task<IReadOnlyList<RequestStatusHistoryItem>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken = default);

    // UC-D4: отчёт о выполненных работах своего отдела по заявке
    Task<bool> CreateWorkReportAsync(
        Guid requestId,
        Guid repairDepartmentId,
        Guid authorId,
        WorkReportInput input,
        CancellationToken cancellationToken = default);

    // UC-D4: список отчётов по заявке (все отделы)
    Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}
