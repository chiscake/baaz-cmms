using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class CatalogLocationEnricher : ICatalogLocationEnricher
{
    private readonly ILocationRepository _locationRepo;

    public CatalogLocationEnricher(ILocationRepository locationRepo)
    {
        _locationRepo = locationRepo;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetFullPathMapAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationRepo.ListAsync(includeInactive, cancellationToken);
        if (!locations.IsSuccess)
            return new Dictionary<Guid, string>();

        var items = locations.Value!.Select(MapLocationModel).ToList();
        return LocationHierarchyHelper.BuildFullPaths(items);
    }

    public IReadOnlyList<LocationListItem> EnrichLocations(IReadOnlyList<LocationModel> models)
    {
        var items = models.Select(MapLocationModel).ToList();
        var paths = LocationHierarchyHelper.BuildFullPaths(items);

        return items
            .Select(item => item with { FullPath = paths.GetValueOrDefault(item.Id) ?? item.Name })
            .ToList();
    }

    public LocationListItem MapLocationModel(LocationModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Code = model.Code,
        ParentId = model.ParentId,
        IsActive = model.IsActive,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt,
    };
}
