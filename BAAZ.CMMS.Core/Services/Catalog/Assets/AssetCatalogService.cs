using System.Linq;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class AssetCatalogService : IAssetCatalogService
{
    private static readonly string[] AssetStatusValues = ["active", "maintenance", "decommissioned"];

    private readonly IAssetRepository _assetRepo;
    private readonly ICatalogLocationEnricher _locationEnricher;
    private readonly IEquipmentCategoryRepository _categoryRepo;
    private readonly IAuthService _authService;

    public AssetCatalogService(
        IAssetRepository assetRepo,
        ICatalogLocationEnricher locationEnricher,
        IEquipmentCategoryRepository categoryRepo,
        IAuthService authService)
    {
        _assetRepo = assetRepo;
        _locationEnricher = locationEnricher;
        _categoryRepo = categoryRepo;
        _authService = authService;
    }

    public async Task<IReadOnlyList<AssetListItem>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAssetsAdminAsync(includeDecommissioned: false, cancellationToken);
        return result.IsSuccess ? result.Value! : [];
    }

    public async Task<DataResult<IReadOnlyList<AssetListItem>>> GetAssetsAdminAsync(
        bool includeDecommissioned = false,
        CancellationToken cancellationToken = default)
    {
        var assetResult = await _assetRepo.ListAsync(includeDecommissioned, cancellationToken);
        if (!assetResult.IsSuccess)
            return DataResult<IReadOnlyList<AssetListItem>>.Fail(assetResult.Error!);

        var locationNames = await _locationEnricher.GetFullPathMapAsync(cancellationToken: cancellationToken);
        var categoryNames = await GetCategoryNameMapAsync(cancellationToken);
        var items = assetResult.Value!
            .Select(model => MapAssetItem(model, locationNames, categoryNames))
            .ToList();

        return DataResult<IReadOnlyList<AssetListItem>>.Ok(items);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNameMapAsync(CancellationToken cancellationToken)
    {
        var result = await _categoryRepo.ListAsync(includeInactive: true, cancellationToken);
        return result.IsSuccess
            ? result.Value!.ToDictionary(c => c.Id, c => c.Name)
            : new Dictionary<Guid, string>();
    }

    public async Task<DataResult<AssetListItem>> CreateAssetAsync(
        AssetEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<AssetListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateAssetInput(input, isCreate: true);
        if (validation is not null)
            return DataResult<AssetListItem>.Fail(validation);

        var model = new AssetModel
        {
            AssetNumber = input.AssetNumber.Trim(),
            Name = input.Name.Trim(),
            LocationId = input.LocationId,
            CategoryId = input.CategoryId,
            Manufacturer = TrimOrNull(input.Manufacturer),
            Model = TrimOrNull(input.Model),
            SerialNumber = TrimOrNull(input.SerialNumber),
            CommissioningDate = input.CommissioningDate,
            Description = TrimOrNull(input.Description),
            Status = "active",
        };

        var result = await _assetRepo.InsertAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<AssetListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedAssetAsync(result.Value!, cancellationToken);
        return DataResult<AssetListItem>.Ok(persisted);
    }

    public async Task<DataResult<AssetListItem>> UpdateAssetAsync(
        Guid assetId,
        AssetEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<AssetListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateAssetInput(input, isCreate: false);
        if (validation is not null)
            return DataResult<AssetListItem>.Fail(validation);

        var existing = await _assetRepo.GetByIdAsync(assetId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<AssetListItem>.Fail(existing.Error!);

        var model = existing.Value!;
        model.AssetNumber = input.AssetNumber.Trim();
        model.Name = input.Name.Trim();
        model.LocationId = input.LocationId;
        model.CategoryId = input.CategoryId;
        model.Manufacturer = TrimOrNull(input.Manufacturer);
        model.Model = TrimOrNull(input.Model);
        model.SerialNumber = TrimOrNull(input.SerialNumber);
        model.CommissioningDate = input.CommissioningDate;
        model.Description = TrimOrNull(input.Description);

        if (!string.IsNullOrWhiteSpace(input.Status))
            model.Status = input.Status.Trim();

        var result = await _assetRepo.UpdateAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<AssetListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedAssetAsync(result.Value!, cancellationToken);
        return DataResult<AssetListItem>.Ok(persisted);
    }

    public async Task<DataResult> DecommissionAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult.Fail(DataError.Unauthorized());

        return await _assetRepo.SetStatusAsync(assetId, "decommissioned", cancellationToken);
    }

    public async Task<DataResult> RestoreAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult.Fail(DataError.Unauthorized());

        return await _assetRepo.SetStatusAsync(assetId, "active", cancellationToken);
    }

    public async Task<DataResult> DeleteAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult.Fail(DataError.Unauthorized());

        return await _assetRepo.DeleteAsync(assetId, cancellationToken);
    }

    private async Task<AssetListItem> ResolvePersistedAssetAsync(
        AssetModel model,
        CancellationToken cancellationToken)
    {
        var refreshed = await _assetRepo.GetByIdAsync(model.Id, cancellationToken);
        var persisted = refreshed.IsSuccess ? refreshed.Value! : model;
        var locationNames = await _locationEnricher.GetFullPathMapAsync(cancellationToken: cancellationToken);
        var categoryNames = await GetCategoryNameMapAsync(cancellationToken);
        return MapAssetItem(persisted, locationNames, categoryNames);
    }

    private static AssetListItem MapAssetItem(
        AssetModel model,
        IReadOnlyDictionary<Guid, string> locationNames,
        IReadOnlyDictionary<Guid, string> categoryNames)
    {
        locationNames.TryGetValue(model.LocationId, out var locationName);
        string? categoryName = null;
        if (model.CategoryId is Guid categoryId)
            categoryNames.TryGetValue(categoryId, out categoryName);

        return new AssetListItem
        {
            Id = model.Id,
            AssetNumber = model.AssetNumber,
            Name = model.Name,
            LocationId = model.LocationId,
            LocationName = locationName,
            CategoryId = model.CategoryId,
            CategoryName = categoryName,
            Status = model.Status,
            Manufacturer = model.Manufacturer,
            Model = model.Model,
            SerialNumber = model.SerialNumber,
            CommissioningDate = model.CommissioningDate,
            Description = model.Description,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
        };
    }

    private static DataError? ValidateAssetInput(AssetEditInput input, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(input.AssetNumber))
            return DataError.Validation("Assets_Validation_AssetNumberRequired");

        if (string.IsNullOrWhiteSpace(input.Name))
            return DataError.Validation("Assets_Validation_NameRequired");

        if (input.LocationId == Guid.Empty)
            return DataError.Validation("Assets_Validation_LocationRequired");

        if (!isCreate
            && !string.IsNullOrWhiteSpace(input.Status)
            && !AssetStatusValues.Contains(input.Status, StringComparer.Ordinal))
        {
            return DataError.Validation("Assets_Validation_InvalidStatus");
        }

        return null;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
