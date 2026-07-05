using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Admin.AssetRegistry;

public sealed partial class AssetRegistryViewModel : CrudWorkbenchViewModelBase<AssetRow>
{
    private readonly IAssetCatalogService _catalogService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAuthService _authService;
    private readonly ILocationTreeCache _locationTreeCache;

    public AssetRegistryViewModel(
        IAssetCatalogService catalogService,
        IMaintenanceService maintenanceService,
        IAuthService authService,
        ILocationTreeCache locationTreeCache)
    {
        _catalogService = catalogService;
        _maintenanceService = maintenanceService;
        _authService = authService;
        _locationTreeCache = locationTreeCache;
        StatusOptions =
        [
            new("active", ResourceStrings.Get("AssetStatus_Active")),
            new("maintenance", ResourceStrings.Get("AssetStatus_Maintenance")),
            new("decommissioned", ResourceStrings.Get("AssetStatus_Decommissioned")),
        ];
    }

    public override string PageTitle => ResourceStrings.Get("Nav_Equipment");

    protected override string ColumnSettingsKey => "Assets";

    protected override string ToolbarResourcePrefix => "Assets";

    protected override Type? CrudSchemaModelType => typeof(AssetModel);

    public const int AssetNumberMaxLength = 100;
    public const int NameMaxLength = 300;
    public const int ManufacturerMaxLength = 200;
    public const int ModelMaxLength = 200;
    public const int SerialNumberMaxLength = 100;
    public const int DescriptionMaxLength = 2000;

    public int EditorAssetNumberMaxLength => AssetNumberMaxLength;
    public int EditorNameMaxLength => NameMaxLength;
    public int EditorManufacturerMaxLength => ManufacturerMaxLength;
    public int EditorModelMaxLength => ModelMaxLength;
    public int EditorSerialNumberMaxLength => SerialNumberMaxLength;
    public int EditorDescriptionMaxLength => DescriptionMaxLength;

    public string ColumnAssetNumber => ResourceStrings.Get("Assets_Column_Number");
    public string ColumnName => ResourceStrings.Get("Assets_Column_Name");
    public string ColumnLocation => ResourceStrings.Get("Assets_Column_Location");
    public string ColumnCategory => ResourceStrings.Get("Assets_Column_Category");
    public string ColumnManufacturer => ResourceStrings.Get("Assets_Column_Manufacturer");
    public string ColumnModel => ResourceStrings.Get("Assets_Column_Model");
    public string ColumnSerialNumber => ResourceStrings.Get("Assets_Column_SerialNumber");
    public string ColumnCommissioningDate => ResourceStrings.Get("Assets_Column_CommissioningDate");
    public string ColumnStatus => ResourceStrings.Get("Assets_Column_Status");
    public string ColumnDescription => ResourceStrings.Get("Assets_Column_Description");
    public string ColumnCreatedAt => ResourceStrings.Get("Assets_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("Assets_Column_UpdatedAt");

    public override string ToolbarHardDeleteLabel =>
        string.Format(ResourceStrings.Get("Assets_Toolbar_HardDelete"), SelectedCount);
    public override string ShowInactiveLabel => ResourceStrings.Get("Assets_ShowDecommissioned");
    public string EditorLabelAssetNumber => ResourceStrings.Get("Assets_Editor_AssetNumber");
    public string EditorLabelName => ResourceStrings.Get("Assets_Editor_Name");
    public string EditorLabelLocation => ResourceStrings.Get("Assets_Editor_Location");
    public string EditorClearLocation => ResourceStrings.Get("Locations_Editor_ClearParent");
    public string EditorLabelCategory => ResourceStrings.Get("Assets_Editor_Category");
    public string EditorLabelManufacturer => ResourceStrings.Get("Assets_Editor_Manufacturer");
    public string EditorLabelModel => ResourceStrings.Get("Assets_Editor_Model");
    public string EditorLabelSerialNumber => ResourceStrings.Get("Assets_Editor_SerialNumber");
    public string EditorLabelCommissioningDate => ResourceStrings.Get("Assets_Editor_CommissioningDate");
    public string EditorLabelStatus => ResourceStrings.Get("Assets_Editor_Status");
    public string EditorLabelDescription => ResourceStrings.Get("Assets_Editor_Description");
    public string EditorCategoryPlaceholder => ResourceStrings.Get("Common_NotSpecified");

    public IReadOnlyList<AssetStatusOption> StatusOptions { get; }

    public ObservableCollection<AssetCategoryOption> CategoryOptions { get; } = [];

    [ObservableProperty]
    public partial Guid? EditorCategoryId { get; set; }

    public int LocationTreeVersion => _locationTreeCache.Current.Version;

    public IReadOnlyList<LocationTreeItem> LocationTreeRoots =>
        _locationTreeCache.Current.ActiveRoots;

    public IReadOnlyDictionary<Guid, string> LocationFullPaths =>
        _locationTreeCache.Current.FullPaths;

    public override string ToolbarDeleteLabel =>
        BuildToggleArchiveToolbarLabel(
            "Assets_Toolbar_Decommission",
            "Assets_Toolbar_Decommission",
            "Assets_Toolbar_Restore");

    [ObservableProperty]
    public partial string EditorAssetNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorManufacturer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorModel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorSerialNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Guid? EditorLocationId { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? EditorCommissioningDate { get; set; }

    [ObservableProperty]
    public partial AssetStatusOption? EditorStatus { get; set; }

    public async Task SetRowDecommissionedAsync(AssetRow row, bool decommission, CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = decommission
                ? await _catalogService.DecommissionAssetAsync(row.Id, ct)
                : await _catalogService.RestoreAssetAsync(row.Id, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return;
            }

            await ReloadRowsAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteRowAsync(AssetRow row, CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            if (await DeleteAsync([row], ct))
                RefreshFilteredRows();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override async Task<bool> SaveInlineCellCoreAsync(
        ICrudRow row, string columnKey, string? newValue, CancellationToken ct)
    {
        if (row is not AssetRow assetRow)
            return false;

        var validationError = columnKey switch
        {
            "AssetNumber" => ValidateAssetNumber(newValue),
            "Name" => ValidateName(newValue),
            "Manufacturer" => ValidateOptionalText(newValue, ManufacturerMaxLength),
            "Model" => ValidateOptionalText(newValue, ModelMaxLength),
            "SerialNumber" => ValidateOptionalText(newValue, SerialNumberMaxLength),
            _ => null,
        };

        if (validationError is not null)
        {
            InfoBanner.Report(validationError, InfoBarSeverity.Error);
            return false;
        }

        if (columnKey == "Location")
        {
            if (!Guid.TryParse(newValue, out var locationId) || locationId == Guid.Empty)
            {
                InfoBanner.Report(ResolveErrorMessage("Assets_Validation_LocationRequired"), InfoBarSeverity.Error);
                return false;
            }

            var baseInput = BuildEditInputFromRow(assetRow);
            var locationInput = new AssetEditInput
            {
                AssetNumber = baseInput.AssetNumber,
                Name = baseInput.Name,
                LocationId = locationId,
                CategoryId = baseInput.CategoryId,
                Manufacturer = baseInput.Manufacturer,
                Model = baseInput.Model,
                SerialNumber = baseInput.SerialNumber,
                Description = baseInput.Description,
                CommissioningDate = baseInput.CommissioningDate,
                Status = baseInput.Status,
            };

            return await PersistAssetUpdateAsync(assetRow, locationInput, ct);
        }

        if (columnKey == "Category")
        {
            Guid? categoryId = string.IsNullOrEmpty(newValue)
                ? null
                : Guid.TryParse(newValue, out var parsedId)
                    ? parsedId
                    : assetRow.CategoryId;

            var baseInput = BuildEditInputFromRow(assetRow);
            var categoryInput = new AssetEditInput
            {
                AssetNumber = baseInput.AssetNumber,
                Name = baseInput.Name,
                LocationId = baseInput.LocationId,
                CategoryId = categoryId,
                Manufacturer = baseInput.Manufacturer,
                Model = baseInput.Model,
                SerialNumber = baseInput.SerialNumber,
                Description = baseInput.Description,
                CommissioningDate = baseInput.CommissioningDate,
                Status = baseInput.Status,
            };

            return await PersistAssetUpdateAsync(assetRow, categoryInput, ct);
        }

        var rowInput = BuildEditInputFromRow(assetRow);
        var input = new AssetEditInput
        {
            AssetNumber = columnKey == "AssetNumber" ? (newValue ?? assetRow.AssetNumber) : rowInput.AssetNumber,
            Name = columnKey == "Name" ? (newValue ?? assetRow.Name) : rowInput.Name,
            LocationId = rowInput.LocationId,
            CategoryId = rowInput.CategoryId,
            Manufacturer = columnKey == "Manufacturer" ? newValue : rowInput.Manufacturer,
            Model = columnKey == "Model" ? newValue : rowInput.Model,
            SerialNumber = columnKey == "SerialNumber" ? newValue : rowInput.SerialNumber,
            Description = rowInput.Description,
            CommissioningDate = columnKey == "CommissioningDate"
                ? DateDisplayHelper.ParseWireFormat(newValue)
                : rowInput.CommissioningDate,
            Status = rowInput.Status,
        };

        return await PersistAssetUpdateAsync(assetRow, input, ct);
    }

    public override string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) =>
        columnKey switch
        {
            "AssetNumber" => ValidateAssetNumber(value),
            "Name" => ValidateName(value),
            "Manufacturer" => ValidateOptionalText(value, ManufacturerMaxLength),
            "Model" => ValidateOptionalText(value, ModelMaxLength),
            "SerialNumber" => ValidateOptionalText(value, SerialNumberMaxLength),
            _ => null,
        };

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "AssetNumber",
            Header = ColumnAssetNumber,
            DataTypeLabel = "text",
            DesiredWidth = 140,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = AssetNumberMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Name",
            Header = ColumnName,
            DataTypeLabel = "text",
            DesiredWidth = double.NaN,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = NameMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Location",
            Header = ColumnLocation,
            DataTypeLabel = "fk",
            DesiredWidth = 200,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.LocationTree,
            AllowClearLocationSelection = false,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Category",
            Header = ColumnCategory,
            DataTypeLabel = "fk",
            DesiredWidth = 160,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.EnumList,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Manufacturer",
            Header = ColumnManufacturer,
            DataTypeLabel = "text",
            DesiredWidth = 140,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = ManufacturerMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Model",
            Header = ColumnModel,
            DataTypeLabel = "text",
            DesiredWidth = 120,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = ModelMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "SerialNumber",
            Header = ColumnSerialNumber,
            DataTypeLabel = "text",
            DesiredWidth = 120,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = SerialNumberMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "CommissioningDate",
            Header = ColumnCommissioningDate,
            DataTypeLabel = "date",
            DesiredWidth = 130,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Date,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Status",
            Header = ColumnStatus,
            DataTypeLabel = "enum",
            DesiredWidth = 130,
            IsSortable = false,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Description",
            Header = ColumnDescription,
            DataTypeLabel = "text",
            DesiredWidth = 200,
            EditKind = CrudColumnEditKind.ReadOnly,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
        });
        CrudColumnTemplates.AppendAuditColumns(Columns, ColumnCreatedAt, ColumnUpdatedAt);
        RefreshLocationColumnTree();
    }

    protected override void InitPermissions()
    {
        var isAdmin = _authService.CurrentProfile?.Role == UserRole.Admin;
        Permissions = new CrudPermissions
        {
            CanCreate = isAdmin,
            CanUpdate = isAdmin,
            CanEdit = isAdmin,
            CanArchive = isAdmin,
            CanInlineEdit = isAdmin,
            CanBulkArchive = isAdmin,
            CanHardDelete = isAdmin,
        };
        OnPropertyChanged(nameof(Permissions));
    }

    protected override async Task LoadDataAsync(CancellationToken ct)
    {
        await EnsureLocationCacheAsync(ct);
        await EnsureCategoryOptionsAsync(ct);
        await ReloadRowsAsync(ct);
    }

    private async Task EnsureCategoryOptionsAsync(CancellationToken ct)
    {
        var result = await _maintenanceService.GetCategoriesAsync(includeInactive: false, ct);
        if (!result.IsSuccess)
            return;

        CategoryOptions.Clear();
        CategoryOptions.Add(new AssetCategoryOption
        {
            Id = null,
            Name = ResourceStrings.Get("Assets_Category_None"),
        });
        foreach (var category in result.Value!.OrderBy(c => c.Name))
        {
            CategoryOptions.Add(new AssetCategoryOption
            {
                Id = category.Id,
                Name = category.Name,
            });
        }

        RefreshCategoryColumnOptions();
        ResyncEditorCategory();
    }

    private void RefreshCategoryColumnOptions()
    {
        var col = Columns.FirstOrDefault(c => c.Key == "Category");
        if (col is null)
            return;

        var noneLabel = ResourceStrings.Get("Assets_Category_None");
        col.EnumOptions =
        [
            new CrudEnumOption { Value = string.Empty, Label = noneLabel },
            ..CategoryOptions
                .Where(c => c.Id is not null)
                .Select(c => new CrudEnumOption { Value = c.Id!.Value.ToString(), Label = c.Name }),
        ];
        OnPropertyChanged(nameof(Columns));
    }

    private void ResyncEditorCategory()
    {
        if (!IsEditorOpen)
            return;

        var currentId = EditingRow?.CategoryId ?? EditorCategoryId;
        EditorCategoryId = currentId;
    }

    protected override async Task<bool> SaveAsync(bool isNew, CancellationToken ct)
    {
        var assetNumberError = ValidateAssetNumber(EditorAssetNumber);
        if (assetNumberError is not null)
        {
            EditorError = assetNumberError;
            return false;
        }

        var nameError = ValidateName(EditorName);
        if (nameError is not null)
        {
            EditorError = nameError;
            return false;
        }

        if (EditorLocationId is null)
        {
            EditorError = ResolveErrorMessage("Assets_Validation_LocationRequired");
            return false;
        }

        var input = new AssetEditInput
        {
            AssetNumber = EditorAssetNumber.Trim(),
            Name = EditorName.Trim(),
            LocationId = EditorLocationId.Value,
            CategoryId = EditorCategoryId,
            Manufacturer = string.IsNullOrWhiteSpace(EditorManufacturer) ? null : EditorManufacturer.Trim(),
            Model = string.IsNullOrWhiteSpace(EditorModel) ? null : EditorModel.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(EditorSerialNumber) ? null : EditorSerialNumber.Trim(),
            Description = string.IsNullOrWhiteSpace(EditorDescription) ? null : EditorDescription.Trim(),
            CommissioningDate = ToDateOnly(EditorCommissioningDate),
            Status = isNew ? null : EditorStatus?.Value,
        };

        if (isNew)
        {
            var result = await _catalogService.CreateAssetAsync(input, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }

            await ReloadRowsAsync(ct);
        }
        else if (EditingRow is not null)
        {
            return await PersistAssetUpdateAsync(EditingRow, input, ct, editorErrors: true);
        }

        return true;
    }

    protected override async Task<bool> ArchiveAsync(IReadOnlyList<AssetRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = row.IsActive
                ? await _catalogService.DecommissionAssetAsync(row.Id, ct)
                : await _catalogService.RestoreAssetAsync(row.Id, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }
        }

        await ReloadRowsAsync(ct);
        return true;
    }

    protected override async Task<bool> DeleteAsync(IReadOnlyList<AssetRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _catalogService.DeleteAssetAsync(row.Id, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }

            if (IsEditorOpen && EditingRow?.Id == row.Id)
            {
                IsEditorOpen = false;
                EditingRow = null;
            }
        }

        await ReloadRowsAsync(ct);
        return true;
    }

    protected override IEnumerable<AssetRow> ApplyFilter(IEnumerable<AssetRow> source)
    {
        var q = FilterText.Trim();
        if (string.IsNullOrEmpty(q))
            return source;

        return source.Where(r =>
            r.AssetNumber.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.LocationName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.Manufacturer?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.Model?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.SerialNumber?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.CategoryName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
    }

    protected override string GetNewRecordTitle() =>
        ResourceStrings.Get("Assets_Editor_Title_New");

    protected override string GetEditRecordTitle() =>
        ResourceStrings.Get("Assets_Editor_Title_Edit");

    protected override void OnNewRecordOpened()
    {
        Debug.WriteLine("[AssetRegistryViewModel] OnNewRecordOpened START");
        EditorAssetNumber = string.Empty;
        EditorName = string.Empty;
        EditorManufacturer = string.Empty;
        EditorModel = string.Empty;
        EditorSerialNumber = string.Empty;
        EditorDescription = string.Empty;
        EditorLocationId = null;
        EditorCategoryId = null;
        Debug.WriteLine($"[AssetRegistryViewModel] OnNewRecordOpened: CategoryOptions={CategoryOptions.Count}");
        EditorCommissioningDate = null;
        EditorStatus = StatusOptions.FirstOrDefault();
        Debug.WriteLine("[AssetRegistryViewModel] OnNewRecordOpened END");
    }

    protected override void OnRowOpened(AssetRow row)
    {
        Debug.WriteLine($"[AssetRegistryViewModel] OnRowOpened START id={row.Id}, categoryId={row.CategoryId}");
        EditorAssetNumber = row.AssetNumber;
        EditorName = row.Name;
        EditorManufacturer = row.Manufacturer ?? string.Empty;
        EditorModel = row.Model ?? string.Empty;
        EditorSerialNumber = row.SerialNumber ?? string.Empty;
        EditorDescription = row.Description ?? string.Empty;
        Debug.WriteLine($"[AssetRegistryViewModel] OnRowOpened: before EditorLocationId={row.LocationId}");
        EditorLocationId = row.LocationId;
        EditorCategoryId = row.CategoryId;
        Debug.WriteLine($"[AssetRegistryViewModel] OnRowOpened: EditorCategoryId={EditorCategoryId}");
        EditorCommissioningDate = row.CommissioningDate.HasValue
            ? new DateTimeOffset(row.CommissioningDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        Debug.WriteLine($"[AssetRegistryViewModel] OnRowOpened: EditorCommissioningDate={EditorCommissioningDate}");
        EditorStatus = StatusOptions.FirstOrDefault(s => s.Value == row.Status)
            ?? StatusOptions.FirstOrDefault();
        Debug.WriteLine($"[AssetRegistryViewModel] OnRowOpened: EditorStatus={EditorStatus?.Value}, END");
    }

    partial void OnEditorCategoryIdChanged(Guid? value)
        => Debug.WriteLine($"[AssetRegistryViewModel] OnEditorCategoryIdChanged -> {value}");

    private async Task<bool> PersistAssetUpdateAsync(
        AssetRow row,
        AssetEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var result = await _catalogService.UpdateAssetAsync(row.Id, input, ct);
        if (!result.IsSuccess)
        {
            var message = ResolveErrorMessage(result.Error!.MessageKey);
            if (editorErrors)
                EditorError = message;
            else
                InfoBanner.Report(message, InfoBarSeverity.Error);
            return false;
        }

        await ReloadRowsAsync(ct);
        return true;
    }

    private async Task ReloadRowsAsync(CancellationToken ct)
    {
        var result = await _catalogService.GetAssetsAdminAsync(includeDecommissioned: true, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException(ResolveErrorMessage(result.Error!.MessageKey));

        _allRows.Clear();
        foreach (var item in result.Value!)
            _allRows.Add(MapToRow(item));
    }

    private async Task EnsureLocationCacheAsync(CancellationToken ct, bool forceReload = false)
    {
        var before = _locationTreeCache.Current.Version;
        if (forceReload)
            await _locationTreeCache.InvalidateAndReloadAsync(ct);
        else
            await _locationTreeCache.EnsureLoadedAsync(ct);

        if (_locationTreeCache.Current.Version != before)
        {
            OnPropertyChanged(nameof(LocationTreeVersion));
            OnPropertyChanged(nameof(LocationTreeRoots));
            OnPropertyChanged(nameof(LocationFullPaths));
        }

        RefreshLocationColumnTree();
    }

    private void RefreshLocationColumnTree()
    {
        var col = Columns.FirstOrDefault(c => c.Key == "Location");
        if (col is null)
            return;

        col.LocationTreeRoots = LocationTreeRoots;
        col.LocationTreeVersion = LocationTreeVersion;
        col.LocationPaths = LocationFullPaths;
    }

    private static AssetRow MapToRow(AssetListItem item) => new()
    {
        Id = item.Id,
        AssetNumber = item.AssetNumber,
        Name = item.Name,
        LocationId = item.LocationId ?? Guid.Empty,
        LocationName = item.LocationName,
        CategoryId = item.CategoryId,
        CategoryName = item.CategoryName,
        Status = item.Status,
        Manufacturer = item.Manufacturer,
        Model = item.Model,
        SerialNumber = item.SerialNumber,
        CommissioningDate = item.CommissioningDate,
        Description = item.Description,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };

    private static AssetEditInput BuildEditInputFromRow(AssetRow row) => new()
    {
        AssetNumber = row.AssetNumber,
        Name = row.Name,
        LocationId = row.LocationId,
        CategoryId = row.CategoryId,
        Manufacturer = row.Manufacturer,
        Model = row.Model,
        SerialNumber = row.SerialNumber,
        Description = row.Description,
        CommissioningDate = row.CommissioningDate,
        Status = row.Status,
    };

    private static DateOnly? ToDateOnly(DateTimeOffset? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value.Date) : null;

    private static string ResolveErrorMessage(string key) =>
        ResourceStrings.Get(key);

    private string? ValidateAssetNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Assets_Validation_AssetNumberRequired");

        if (value.Length > AssetNumberMaxLength)
            return ResolveErrorMessage("Assets_Validation_AssetNumberRequired");

        return null;
    }

    private string? ValidateName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Assets_Validation_NameRequired");

        if (value.Length > NameMaxLength)
            return ResolveErrorMessage("Assets_Validation_NameRequired");

        return null;
    }

    private static string? ValidateOptionalText(string? value, int maxLength)
    {
        if (value is not null && value.Length > maxLength)
            return ResourceStrings.Get("Assets_Validation_TextTooLong");

        return null;
    }

    public async Task ApplyNavigationArgsAsync(AssetRegistryNavigationArgs? args)
    {
        if (args?.AssetId is not Guid assetId)
            return;

        await OnPageLoadedAsync();

        var row = _allRows.FirstOrDefault(r => r.Id == assetId);
        if (row is not null && OpenRowCommand.CanExecute(row))
            OpenRowCommand.Execute(row);
    }
}

public sealed record AssetStatusOption(string Value, string Label);

public sealed class AssetCategoryOption
{
    public Guid? Id { get; init; }

    public required string Name { get; init; }
}
