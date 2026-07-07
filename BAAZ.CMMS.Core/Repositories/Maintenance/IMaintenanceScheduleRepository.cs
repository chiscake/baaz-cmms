using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IMaintenanceScheduleRepository
{
    Task<DataResult<IReadOnlyList<MaintenanceScheduleModel>>> ListAsync(CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<MaintenanceScheduleModel>>> ListByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<MaintenanceScheduleDepartmentModel>>> ListDepartmentsAsync(
        CancellationToken ct = default);

    Task<DataResult> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default);
}
