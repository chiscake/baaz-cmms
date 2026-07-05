using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class LocationCatalogService : ILocationCatalogService
{
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly ILocationRepository _locationRepo;
    private readonly ICatalogLocationEnricher _locationEnricher;
    private readonly IAuthService _authService;

    public LocationCatalogService(
        ISupabaseClientProvider clientProvider,
        ILocationRepository locationRepo,
        ICatalogLocationEnricher locationEnricher,
        IAuthService authService)
    {
        _clientProvider = clientProvider;
        _locationRepo = locationRepo;
        _locationEnricher = locationEnricher;
        _authService = authService;
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleLocationIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var ids = await SupabaseRestClient.CallRpcAsync<Guid>(
            _clientProvider,
            "profile_accessible_location_ids",
            new { },
            cancellationToken);

        return ids ?? [];
    }

    public async Task<IReadOnlyList<LocationListItem>> GetLocationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        using var step = PerfDebug.Step(
            "LocationCatalogService.GetLocationsAsync",
            $"includeInactive={includeInactive}");
        var result = await _locationRepo.ListAsync(includeInactive, cancellationToken);
        if (!result.IsSuccess)
            return [];

        using (PerfDebug.Step("LocationCatalogService.EnrichLocations"))
        {
            var enriched = _locationEnricher.EnrichLocations(result.Value!);
            PerfDebug.Mark("LocationCatalogService.GetLocationsAsync", $"enriched={enriched.Count}");
            return enriched;
        }
    }

    public async Task<DataResult<LocationListItem>> CreateLocationAsync(
        LocationEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<LocationListItem>.Fail(DataError.Unauthorized());

        var validation = await ValidateLocationInputAsync(null, input, cancellationToken);
        if (validation is not null)
            return DataResult<LocationListItem>.Fail(validation);

        var model = new LocationModel
        {
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim(),
            ParentId = input.ParentId,
            IsActive = true,
        };

        var result = await _locationRepo.InsertAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<LocationListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedLocationAsync(result.Value!, cancellationToken);
        return DataResult<LocationListItem>.Ok(await MapLocationItemAsync(persisted, cancellationToken));
    }

    public async Task<DataResult<LocationListItem>> UpdateLocationAsync(
        Guid locationId,
        LocationEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<LocationListItem>.Fail(DataError.Unauthorized());

        var validation = await ValidateLocationInputAsync(locationId, input, cancellationToken);
        if (validation is not null)
            return DataResult<LocationListItem>.Fail(validation);

        var existing = await _locationRepo.GetByIdAsync(locationId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<LocationListItem>.Fail(existing.Error!);

        var model = existing.Value!;
        model.Name = input.Name.Trim();
        model.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        model.ParentId = input.ParentId;

        var result = await _locationRepo.UpdateAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<LocationListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedLocationAsync(result.Value!, cancellationToken);
        return DataResult<LocationListItem>.Ok(await MapLocationItemAsync(persisted, cancellationToken));
    }

    public Task<DataResult> ArchiveLocationBranchAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return Task.FromResult(DataResult.Fail(DataError.Unauthorized()));

        return _locationRepo.ArchiveBranchAsync(locationId, cancellationToken);
    }

    public Task<DataResult> RestoreLocationBranchAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return Task.FromResult(DataResult.Fail(DataError.Unauthorized()));

        return _locationRepo.RestoreBranchAsync(locationId, cancellationToken);
    }

    public Task<DataResult> HardDeleteLocationBranchAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return Task.FromResult(DataResult.Fail(DataError.Unauthorized()));

        return _locationRepo.HardDeleteBranchAsync(locationId, cancellationToken);
    }

    private async Task<DataError?> ValidateLocationInputAsync(
        Guid? locationId,
        LocationEditInput input,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            return DataError.Validation("Locations_Validation_NameRequired");

        var allResult = await _locationRepo.ListAsync(includeInactive: true, ct);
        if (!allResult.IsSuccess)
            return allResult.Error;

        var all = _locationEnricher.EnrichLocations(allResult.Value!);

        if (locationId.HasValue &&
            LocationHierarchyHelper.WouldCreateCycle(locationId.Value, input.ParentId, all))
        {
            return DataError.Validation("Locations_Validation_Cycle");
        }

        return null;
    }

    private async Task<LocationListItem> MapLocationItemAsync(LocationModel model, CancellationToken ct)
    {
        var allResult = await _locationRepo.ListAsync(includeInactive: true, ct);
        if (!allResult.IsSuccess)
            return _locationEnricher.MapLocationModel(model);

        var items = _locationEnricher.EnrichLocations(allResult.Value!);
        return items.FirstOrDefault(i => i.Id == model.Id) ?? _locationEnricher.MapLocationModel(model);
    }

    private async Task<LocationModel> ResolvePersistedLocationAsync(LocationModel model, CancellationToken ct)
    {
        if (model.CreatedAt.HasValue && model.UpdatedAt.HasValue)
            return model;

        var reload = await _locationRepo.GetByIdAsync(model.Id, ct);
        return reload.IsSuccess ? reload.Value! : model;
    }
}
