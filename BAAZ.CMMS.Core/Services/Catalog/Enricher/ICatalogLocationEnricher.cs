using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

public interface ICatalogLocationEnricher
{
    Task<IReadOnlyDictionary<Guid, string>> GetFullPathMapAsync(
        bool includeInactive = true,
        CancellationToken cancellationToken = default);

    IReadOnlyList<LocationListItem> EnrichLocations(IReadOnlyList<LocationModel> models);

    LocationListItem MapLocationModel(LocationModel model);
}
