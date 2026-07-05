using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IAssetRepository
{
    Task<DataResult<IReadOnlyList<AssetModel>>> ListAsync(
        bool includeDecommissioned = false,
        CancellationToken ct = default);

    Task<DataResult<AssetModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<AssetModel>> InsertAsync(AssetModel model, CancellationToken ct = default);

    Task<DataResult<AssetModel>> UpdateAsync(AssetModel model, CancellationToken ct = default);

    Task<DataResult> SetStatusAsync(Guid id, string status, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
