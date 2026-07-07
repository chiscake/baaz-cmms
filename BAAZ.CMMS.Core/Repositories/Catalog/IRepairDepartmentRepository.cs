using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IRepairDepartmentRepository
{
    Task<DataResult<IReadOnlyList<RepairDepartmentModel>>> ListAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<DataResult<RepairDepartmentModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<RepairDepartmentModel>> InsertAsync(RepairDepartmentModel model, CancellationToken ct = default);

    Task<DataResult<RepairDepartmentModel>> UpdateAsync(RepairDepartmentModel model, CancellationToken ct = default);

    Task<DataResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<bool>> HasDispatchersAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<bool>> HasActiveRequestsAsync(Guid id, CancellationToken ct = default);
}
