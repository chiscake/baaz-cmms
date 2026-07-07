using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

/// <summary>
/// Фасад справочников: делегирует в доменные catalog-сервисы.
/// Покрывает UC-A1, UC-A4, UC-A6, UC-D9.
/// </summary>
public sealed class CatalogService : ICatalogService
{
    private readonly IAssetCatalogService _assets;
    private readonly ILocationCatalogService _locations;
    private readonly ITechnicianCatalogService _technicians;
    private readonly IRepairDepartmentCatalogService _departments;

    public CatalogService(
        IAssetCatalogService assets,
        ILocationCatalogService locations,
        ITechnicianCatalogService technicians,
        IRepairDepartmentCatalogService departments)
    {
        _assets = assets;
        _locations = locations;
        _technicians = technicians;
        _departments = departments;
    }

    public Task<IReadOnlyList<AssetListItem>> GetAssetsAsync(CancellationToken cancellationToken = default)
        => _assets.GetAssetsAsync(cancellationToken);

    public Task<DataResult<IReadOnlyList<AssetListItem>>> GetAssetsAdminAsync(
        bool includeDecommissioned = false,
        CancellationToken cancellationToken = default)
        => _assets.GetAssetsAdminAsync(includeDecommissioned, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetAccessibleLocationIdsAsync(CancellationToken cancellationToken = default)
        => _locations.GetAccessibleLocationIdsAsync(cancellationToken);

    public Task<DataResult<AssetListItem>> CreateAssetAsync(
        AssetEditInput input,
        CancellationToken cancellationToken = default)
        => _assets.CreateAssetAsync(input, cancellationToken);

    public Task<DataResult<AssetListItem>> UpdateAssetAsync(
        Guid assetId,
        AssetEditInput input,
        CancellationToken cancellationToken = default)
        => _assets.UpdateAssetAsync(assetId, input, cancellationToken);

    public Task<DataResult> DecommissionAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
        => _assets.DecommissionAssetAsync(assetId, cancellationToken);

    public Task<DataResult> RestoreAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
        => _assets.RestoreAssetAsync(assetId, cancellationToken);

    public Task<DataResult> DeleteAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
        => _assets.DeleteAssetAsync(assetId, cancellationToken);

    public Task<DataResult<IReadOnlyList<TechnicianListItem>>> GetTechniciansAsync(
        CancellationToken cancellationToken = default)
        => _technicians.GetTechniciansAsync(cancellationToken);

    public Task<DataResult<TechnicianListItem>> CreateTechnicianAsync(
        TechnicianEditInput input,
        CancellationToken cancellationToken = default)
        => _technicians.CreateTechnicianAsync(input, cancellationToken);

    public Task<DataResult<TechnicianListItem>> UpdateTechnicianAsync(
        Guid technicianId,
        TechnicianEditInput input,
        CancellationToken cancellationToken = default)
        => _technicians.UpdateTechnicianAsync(technicianId, input, cancellationToken);

    public Task<DataResult> SetTechnicianActiveAsync(
        Guid technicianId,
        bool isActive,
        CancellationToken cancellationToken = default)
        => _technicians.SetTechnicianActiveAsync(technicianId, isActive, cancellationToken);

    public Task<DataResult> DeleteTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default)
        => _technicians.DeleteTechnicianAsync(technicianId, cancellationToken);

    public Task<DataResult<IReadOnlyList<RepairDepartmentListItem>>> GetRepairDepartmentsAsync(
        CancellationToken cancellationToken = default)
        => _departments.GetRepairDepartmentsAsync(cancellationToken);

    public Task<DataResult<IReadOnlyList<RepairDepartmentAdminListItem>>> GetRepairDepartmentsAdminAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => _departments.GetRepairDepartmentsAdminAsync(includeInactive, cancellationToken);

    public Task<DataResult<RepairDepartmentAdminListItem>> CreateRepairDepartmentAsync(
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default)
        => _departments.CreateRepairDepartmentAsync(input, cancellationToken);

    public Task<DataResult<RepairDepartmentAdminListItem>> UpdateRepairDepartmentAsync(
        Guid departmentId,
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default)
        => _departments.UpdateRepairDepartmentAsync(departmentId, input, cancellationToken);

    public Task<DataResult> SetRepairDepartmentActiveAsync(
        Guid departmentId,
        bool isActive,
        CancellationToken cancellationToken = default)
        => _departments.SetRepairDepartmentActiveAsync(departmentId, isActive, cancellationToken);

    public Task<DataResult> DeleteRepairDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
        => _departments.DeleteRepairDepartmentAsync(departmentId, cancellationToken);

    public Task<IReadOnlyList<LocationListItem>> GetLocationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => _locations.GetLocationsAsync(includeInactive, cancellationToken);

    public Task<DataResult<LocationListItem>> CreateLocationAsync(
        LocationEditInput input,
        CancellationToken cancellationToken = default)
        => _locations.CreateLocationAsync(input, cancellationToken);

    public Task<DataResult<LocationListItem>> UpdateLocationAsync(
        Guid locationId,
        LocationEditInput input,
        CancellationToken cancellationToken = default)
        => _locations.UpdateLocationAsync(locationId, input, cancellationToken);

    public Task<DataResult> ArchiveLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default)
        => _locations.ArchiveLocationBranchAsync(locationId, cancellationToken);

    public Task<DataResult> RestoreLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default)
        => _locations.RestoreLocationBranchAsync(locationId, cancellationToken);

    public Task<DataResult> HardDeleteLocationBranchAsync(Guid locationId, CancellationToken cancellationToken = default)
        => _locations.HardDeleteLocationBranchAsync(locationId, cancellationToken);
}
