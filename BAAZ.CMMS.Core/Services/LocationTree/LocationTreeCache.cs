using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services.Catalog;

namespace BAAZ.CMMS.Core.Services;

public sealed class LocationTreeCache : ILocationTreeCache
{
    private static readonly LocationTreeSnapshot EmptySnapshot = new()
    {
        Version = 0,
        AllItems = [],
        ActiveRoots = [],
        FullPaths = new Dictionary<Guid, string>(),
        ById = new Dictionary<Guid, LocationListItem>(),
    };

    private readonly ILocationCatalogService _locationCatalog;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private LocationTreeSnapshot _current = EmptySnapshot;

    public LocationTreeCache(ILocationCatalogService locationCatalog)
    {
        _locationCatalog = locationCatalog;
    }

    public LocationTreeSnapshot Current => _current;

    public Task<LocationTreeSnapshot> EnsureLoadedAsync(CancellationToken cancellationToken = default) =>
        _current.Version == 0
            ? ReloadCoreAsync(cancellationToken)
            : Task.FromResult(_current);

    public Task<LocationTreeSnapshot> InvalidateAndReloadAsync(CancellationToken cancellationToken = default) =>
        ReloadCoreAsync(cancellationToken);

    public LocationTreeSnapshot LoadFromItems(IReadOnlyList<LocationListItem> items)
    {
        using var step = PerfDebug.Step(
            "LocationTreeCache.LoadFromItems",
            $"items={items.Count}");
        _gate.Wait();
        try
        {
            using (PerfDebug.Step("LocationTreeCache.BuildSnapshot"))
                _current = BuildSnapshot(items, _current.Version + 1);
            PerfDebug.Mark("LocationTreeCache.LoadFromItems", $"version={_current.Version}");
            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<LocationTreeSnapshot> ReloadCoreAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await _locationCatalog.GetLocationsAsync(includeInactive: true, cancellationToken);
            var list = items as IReadOnlyList<LocationListItem> ?? items.ToList();
            _current = BuildSnapshot(list, _current.Version + 1);
            return _current;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static LocationTreeSnapshot BuildSnapshot(IReadOnlyList<LocationListItem> list, int version)
    {
        var fullPaths = LocationHierarchyHelper.BuildFullPaths(list);
        var activeRoots = LocationHierarchyHelper.BuildTree(list, item => item.IsActive);
        var byId = list.ToDictionary(l => l.Id);

        return new LocationTreeSnapshot
        {
            Version = version,
            AllItems = list,
            ActiveRoots = activeRoots,
            FullPaths = fullPaths,
            ById = byId,
        };
    }
}
