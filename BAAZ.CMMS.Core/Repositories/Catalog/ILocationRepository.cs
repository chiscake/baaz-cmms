using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface ILocationRepository
{
    Task<DataResult<IReadOnlyList<LocationModel>>> ListAsync(
        bool includeInactive = true,
        CancellationToken ct = default);

    Task<DataResult<LocationModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<LocationModel>> InsertAsync(LocationModel model, CancellationToken ct = default);

    Task<DataResult<LocationModel>> UpdateAsync(LocationModel model, CancellationToken ct = default);

    Task<DataResult> ArchiveBranchAsync(Guid locationId, CancellationToken ct = default);

    Task<DataResult> RestoreBranchAsync(Guid locationId, CancellationToken ct = default);

    Task<DataResult> HardDeleteBranchAsync(Guid locationId, CancellationToken ct = default);
}
