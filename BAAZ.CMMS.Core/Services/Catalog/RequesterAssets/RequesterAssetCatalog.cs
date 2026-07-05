using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class RequesterAssetCatalog : IRequesterAssetCatalog
{
    private readonly ILocationTreeCache _locationTreeCache;
    private readonly IAssetCatalogService _assetCatalog;
    private readonly ILocationCatalogService _locationCatalog;
    private readonly IAuthService _authService;

    public RequesterAssetCatalog(
        ILocationTreeCache locationTreeCache,
        IAssetCatalogService assetCatalog,
        ILocationCatalogService locationCatalog,
        IAuthService authService)
    {
        _locationTreeCache = locationTreeCache;
        _assetCatalog = assetCatalog;
        _locationCatalog = locationCatalog;
        _authService = authService;
    }

    public async Task<DataResult<IReadOnlyList<RequesterScopedAssetItem>>> GetActiveScopedAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var locationSnapshot = await _locationTreeCache.EnsureLoadedAsync(cancellationToken);
            var fullPaths = locationSnapshot.FullPaths;

            var assets = await _assetCatalog.GetAssetsAsync(cancellationToken);
            if (_authService.CurrentProfile?.Role == UserRole.Dispatcher)
            {
                var accessibleIds = await _locationCatalog.GetAccessibleLocationIdsAsync(cancellationToken);
                if (accessibleIds.Count > 0)
                {
                    var accessibleSet = accessibleIds.ToHashSet();
                    assets = assets
                        .Where(a => !a.LocationId.HasValue || accessibleSet.Contains(a.LocationId.Value))
                        .ToList();
                }
            }

            var items = assets
                .Where(a => string.Equals(a.Status, "active", StringComparison.Ordinal))
                .Select(a =>
                {
                    string? locationFullPath = null;
                    if (a.LocationId.HasValue
                        && fullPaths.TryGetValue(a.LocationId.Value, out var path))
                    {
                        locationFullPath = path;
                    }

                    return new RequesterScopedAssetItem
                    {
                        Asset = a,
                        LocationFullPath = locationFullPath,
                    };
                })
                .ToList();

            return DataResult<IReadOnlyList<RequesterScopedAssetItem>>.Ok(items);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<RequesterScopedAssetItem>>.Fail(DataError.Network(ex.Message));
        }
    }
}
