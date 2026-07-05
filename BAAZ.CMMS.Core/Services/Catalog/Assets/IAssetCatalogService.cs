using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

/// <summary>UC-A4 — реестр объектов.</summary>
public interface IAssetCatalogService
{
    Task<IReadOnlyList<AssetListItem>> GetAssetsAsync(CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<AssetListItem>>> GetAssetsAdminAsync(
        bool includeDecommissioned = false,
        CancellationToken cancellationToken = default);

    Task<DataResult<AssetListItem>> CreateAssetAsync(
        AssetEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult<AssetListItem>> UpdateAssetAsync(
        Guid assetId,
        AssetEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult> DecommissionAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<DataResult> RestoreAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<DataResult> DeleteAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
}
