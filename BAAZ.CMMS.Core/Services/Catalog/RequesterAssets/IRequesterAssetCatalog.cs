using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class RequesterScopedAssetItem
{
    public required AssetListItem Asset { get; init; }

    public string? LocationFullPath { get; init; }
}

public interface IRequesterAssetCatalog
{
    Task<DataResult<IReadOnlyList<RequesterScopedAssetItem>>> GetActiveScopedAssetsAsync(
        CancellationToken cancellationToken = default);
}
