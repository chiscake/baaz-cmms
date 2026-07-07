using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IMaintenanceNormRepository
{
    Task<DataResult<IReadOnlyList<MaintenanceNormModel>>> ListByAssetAsync(
        Guid assetId, CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<MaintenanceNormModel>>> ListAllAsync(CancellationToken ct = default);

    Task<DataResult<MaintenanceNormModel>> InsertAsync(MaintenanceNormModel model, CancellationToken ct = default);

    Task<DataResult<MaintenanceNormModel>> UpdateAsync(MaintenanceNormModel model, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
