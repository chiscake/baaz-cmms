using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface ITechnicianRepository
{
    Task<DataResult<IReadOnlyList<TechnicianModel>>> ListAsync(CancellationToken ct = default);

    Task<DataResult<TechnicianModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<TechnicianModel>> InsertAsync(TechnicianModel model, CancellationToken ct = default);

    Task<DataResult<TechnicianModel>> UpdateAsync(TechnicianModel model, CancellationToken ct = default);

    Task<DataResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
