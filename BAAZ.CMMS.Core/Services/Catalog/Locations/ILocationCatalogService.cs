using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

/// <summary>UC-A1 — подразделения (локации).</summary>
public interface ILocationCatalogService
{
    Task<IReadOnlyList<LocationListItem>> GetLocationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>Поддеревья локаций, доступных текущему пользователю как заявителю (RPC).</summary>
    Task<IReadOnlyList<Guid>> GetAccessibleLocationIdsAsync(CancellationToken cancellationToken = default);

    Task<DataResult<LocationListItem>> CreateLocationAsync(
        LocationEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult<LocationListItem>> UpdateLocationAsync(
        Guid locationId,
        LocationEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult> ArchiveLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task<DataResult> RestoreLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task<DataResult> HardDeleteLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default);
}
