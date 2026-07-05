using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IAssetMaintenanceStatusRepository
{
    Task<DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>> ListByAssetAsync(
        Guid assetId, CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>> ListAllAsync(CancellationToken ct = default);
}
