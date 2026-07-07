using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;

namespace BAAZ.CMMS.Core.Repositories;

public interface IWorkReportRepository
{
    Task<DataResult> InsertAsync(WorkReportInsertDto row, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByRequestAsync(
        Guid requestId, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByScheduleAsync(
        Guid scheduleId, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByScheduleIdsAsync(
        IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<WorkReportListRowDto>>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task<DataResult<WorkReportRowDto>> GetByIdAsync(
        Guid workReportId,
        CancellationToken cancellationToken = default);
}
