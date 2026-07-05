using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Admin.Locations;

public sealed partial class LocationsViewModel : CrudWorkbenchViewModelBase<LocationRow>
{
    private readonly ILocationCatalogService _catalogService;
    private readonly IAuthService _authService;
    private readonly ILocationTreeCache _locationTreeCache;
    private List<LocationListItem> _allLocationItems = [];

    public LocationsViewModel(
        ILocationCatalogService catalogService,
        IAuthService authService,
        ILocationTreeCache locationTreeCache)
    {
        _catalogService = catalogService;
        _authService = authService;
        _locationTreeCache = locationTreeCache;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_Locations");

    protected override string ColumnSettingsKey => "Locations";

    protected override string ToolbarResourcePrefix => "Locations";

    protected override Type? CrudSchemaModelType => typeof(LocationModel);

    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public int EditorNameMaxLength => NameMaxLength;
    public int EditorCodeMaxLength => CodeMaxLength;

    public string ColumnFullPath => ResourceStrings.Get("Locations_Column_FullPath");
    public string ColumnName => ResourceStrings.Get("Locations_Column_Name");
    public string ColumnCode => ResourceStrings.Get("Locations_Column_Code");
    public string ColumnParent => ResourceStrings.Get("Locations_Column_Parent");
    public string ColumnActive => ResourceStrings.Get("Locations_Column_Active");
    public string ColumnCreatedAt => ResourceStrings.Get("Locations_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("Locations_Column_UpdatedAt");

    public override string ToolbarHardDeleteLabel =>
        string.Format(ResourceStrings.Get("Locations_Toolbar_HardDelete"), SelectedCount);
    public override string ShowInactiveLabel => ResourceStrings.Get("Locations_ShowInactive");
    public string EditorLabelName => ResourceStrings.Get("Locations_Editor_Name");
    public string EditorLabelCode => ResourceStrings.Get("Locations_Editor_Code");
    public string EditorLabelParent => ResourceStrings.Get("Locations_Editor_Parent");
    public string EditorClearParent => ResourceStrings.Get("Locations_Editor_ClearParent");

    public override string ToolbarDeleteLabel =>
        BuildToggleArchiveToolbarLabel(
            "Locations_Toolbar_Archive",
            "Locations_Toolbar_Archive",
            "Locations_Toolbar_Restore");

    public int ParentTreeVersion => _locationTreeCache.Current.Version;

    public IReadOnlyList<LocationTreeItem> ParentTreeRoots =>
        _locationTreeCache.Current.ActiveRoots;

    public IReadOnlyDictionary<Guid, string> ParentLocationFullPaths =>
        _locationTreeCache.Current.FullPaths;

    public IReadOnlySet<Guid> DisabledParentIds { get; private set; } = new HashSet<Guid>();

    [ObservableProperty]
    public partial string EditorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Guid? EditorParentId { get; set; }

    public async Task SetRowArchivedAsync(LocationRow row, bool archive, CancellationToken ct = default)
    {
        var total = Stopwatch.StartNew();
        PerfDebug.Mark(
            "Locations.SetRowArchived",
            $"BEGIN id={row.Id} archive={archive} path={row.FullPath}");
        IsBusy = true;
        try
        {
            DataResult result;
            using (PerfDebug.Step("Locations.SetRowArchived", "RPC"))
            {
                result = archive
                    ? await _catalogService.ArchiveLocationBranchAsync(row.Id, ct)
                    : await _catalogService.RestoreLocationBranchAsync(row.Id, ct);
            }

            if (!result.IsSuccess)
            {
                PerfDebug.Mark("Locations.SetRowArchived", $"RPC failed key={result.Error?.MessageKey}");
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return;
            }

            await ReloadRowsAsync(ct, "SetRowArchived");
        }
        catch (OperationCanceledException)
        {
            PerfDebug.Mark("Locations.SetRowArchived", "CANCELLED");
        }
        catch (Exception ex)
        {
            PerfDebug.Mark("Locations.SetRowArchived", $"ERROR {ex.GetType().Name}: {ex.Message}");
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
            PerfDebug.Mark("Locations.SetRowArchived", $"TOTAL +{total.ElapsedMilliseconds}ms");
        }
    }

    public async Task DeleteRowAsync(LocationRow row, CancellationToken ct = default)
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
        if (row is not LocationRow locationRow)
            return false;

        if (columnKey == "Parent")
        {
            var parentId = ParseOptionalParentId(newValue);
            if (parentId is Guid pid
                && LocationHierarchyHelper.GetSubtreeIds(locationRow.Id, _locationTreeCache.Current.AllItems)
                    .Contains(pid))
            {
                InfoBanner.Report(ResolveErrorMessage("Locations_Validation_Cycle"), InfoBarSeverity.Error);
                return false;
            }

            var parentInput = new LocationEditInput
            {
                Name = locationRow.Name,
                Code = locationRow.Code,
                ParentId = parentId,
            };

            return await PersistLocationUpdateAsync(locationRow, parentInput, ct);
        }

        var validationError = columnKey switch
        {
            "Name" => ValidateName(newValue),
            "Code" => ValidateCode(newValue),
            _ => null,
        };

        if (validationError is not null)
        {
            InfoBanner.Report(validationError, InfoBarSeverity.Error);
            return false;
        }

        var input = new LocationEditInput
        {
            Name = columnKey == "Name" ? (newValue ?? locationRow.Name) : locationRow.Name,
            Code = columnKey == "Code" ? newValue : locationRow.Code,
            ParentId = locationRow.ParentId,
        };

        return await PersistLocationUpdateAsync(locationRow, input, ct);
    }

    public override void PrepareInlineCellEdit(ICrudRow row, string columnKey)
    {
        if (columnKey != "Parent" || row is not LocationRow locationRow)
            return;

        RefreshParentColumnTree(locationRow.Id);
    }

    public override string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) =>
        columnKey switch
        {
            "Name" => ValidateName(value),
            "Code" => ValidateCode(value),
            _ => null,
        };

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "FullPath",
            Header = ColumnFullPath,
            DataTypeLabel = "computed",
            DesiredWidth = 320,
            IsComputed = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = true,
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
            Key = "Code",
            Header = ColumnCode,
            DataTypeLabel = "text",
            DesiredWidth = 120,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = CodeMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Parent",
            Header = ColumnParent,
            DataTypeLabel = "fk",
            DesiredWidth = 180,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.LocationTree,
            AllowClearLocationSelection = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(CrudColumnTemplates.CreateActiveBoolColumn(ColumnActive));
        CrudColumnTemplates.AppendAuditColumns(Columns, ColumnCreatedAt, ColumnUpdatedAt);
        RefreshParentColumnTree();
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
        await ReloadRowsAsync(ct);
    }

    protected override async Task<bool> SaveAsync(bool isNew, CancellationToken ct)
    {
        var nameError = ValidateName(EditorName);
        if (nameError is not null)
        {
            EditorError = nameError;
            return false;
        }

        var codeError = ValidateCode(EditorCode);
        if (codeError is not null)
        {
            EditorError = codeError;
            return false;
        }

        var input = new LocationEditInput
        {
            Name = EditorName.Trim(),
            Code = string.IsNullOrWhiteSpace(EditorCode) ? null : EditorCode.Trim(),
            ParentId = EditorParentId,
        };

        if (isNew)
        {
            var result = await _catalogService.CreateLocationAsync(input, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }

            await ReloadRowsAsync(ct);
        }
        else if (EditingRow is not null)
        {
            return await PersistLocationUpdateAsync(EditingRow, input, ct, editorErrors: true);
        }

        return true;
    }

    protected override async Task<bool> ArchiveAsync(IReadOnlyList<LocationRow> rows, CancellationToken ct)
    {
        var total = Stopwatch.StartNew();
        PerfDebug.Mark("Locations.BulkArchive", $"BEGIN rows={rows.Count}");
        var index = 0;
        foreach (var row in rows)
        {
            index++;
            DataResult result;
            using (PerfDebug.Step(
                "Locations.BulkArchive",
                $"RPC {index}/{rows.Count} id={row.Id} active={row.IsActive}"))
            {
                result = row.IsActive
                    ? await _catalogService.ArchiveLocationBranchAsync(row.Id, ct)
                    : await _catalogService.RestoreLocationBranchAsync(row.Id, ct);
            }

            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                PerfDebug.Mark("Locations.BulkArchive", $"FAILED at {index}/{rows.Count} +{total.ElapsedMilliseconds}ms");
                return false;
            }
        }

        await ReloadRowsAsync(ct, "BulkArchive");
        PerfDebug.Mark("Locations.BulkArchive", $"TOTAL +{total.ElapsedMilliseconds}ms");
        return true;
    }

    protected override async Task<bool> DeleteAsync(IReadOnlyList<LocationRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _catalogService.HardDeleteLocationBranchAsync(row.Id, ct);
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

    protected override IEnumerable<LocationRow> ApplyFilter(IEnumerable<LocationRow> source)
    {
        var q = FilterText.Trim();
        if (string.IsNullOrEmpty(q))
            return source;

        return source.Where(r =>
            r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.Code?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            r.FullPath.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.ParentName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
    }

    protected override string GetNewRecordTitle() =>
        ResourceStrings.Get("Locations_Editor_Title_New");

    protected override string GetEditRecordTitle() =>
        ResourceStrings.Get("Locations_Editor_Title_Edit");

    protected override void OnNewRecordOpened()
    {
        EditorName = string.Empty;
        EditorCode = string.Empty;
        EditorParentId = null;
        RefreshParentTree(null);
    }

    protected override void OnRowOpened(LocationRow row)
    {
        EditorName = row.Name;
        EditorCode = row.Code ?? string.Empty;
        EditorParentId = row.ParentId;
        RefreshParentTree(row.Id);
    }

    private async Task<bool> PersistLocationUpdateAsync(
        LocationRow row,
        LocationEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var result = await _catalogService.UpdateLocationAsync(row.Id, input, ct);
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

    private async Task ReloadRowsAsync(CancellationToken ct, string caller = "ReloadRowsAsync")
    {
        using var step = PerfDebug.Step("Locations.ReloadRowsAsync", $"caller={caller}");
        IReadOnlyList<LocationListItem> items;
        using (PerfDebug.Step("Locations.ReloadRowsAsync", "GetLocationsAsync"))
            items = await _catalogService.GetLocationsAsync(includeInactive: true, ct);
        _allLocationItems = items.ToList();

        using (PerfDebug.Step("Locations.ReloadRowsAsync", $"MapRows count={_allLocationItems.Count}"))
        {
            var byId = _allLocationItems.ToDictionary(i => i.Id);
            _allRows.Clear();
            foreach (var item in _allLocationItems)
                _allRows.Add(MapToRow(item, byId));
        }

        var before = _locationTreeCache.Current.Version;
        using (PerfDebug.Step("Locations.ReloadRowsAsync", "LocationTreeCache.LoadFromItems"))
            _locationTreeCache.LoadFromItems(_allLocationItems);

        if (_locationTreeCache.Current.Version != before)
        {
            using (PerfDebug.Step("Locations.ReloadRowsAsync", "NotifyParentTreeChanged"))
                NotifyParentTreeChanged();
        }
        else
        {
            RefreshParentTree(EditingRow?.Id);
            RefreshParentColumnTree();
        }

        using (PerfDebug.Step("Locations.ReloadRowsAsync", "RefreshFilteredRows"))
            RefreshFilteredRows();
    }

    private void NotifyParentTreeChanged()
    {
        PerfDebug.Mark("Locations.NotifyParentTreeChanged", $"version={_locationTreeCache.Current.Version}");
        OnPropertyChanged(nameof(ParentTreeVersion));
        OnPropertyChanged(nameof(ParentTreeRoots));
        OnPropertyChanged(nameof(ParentLocationFullPaths));
        RefreshParentTree(EditingRow?.Id);
        RefreshParentColumnTree();
    }

    private void RefreshParentColumnTree(Guid? forEditingLocationId = null)
    {
        var col = Columns.FirstOrDefault(c => c.Key == "Parent");
        if (col is null)
            return;

        col.LocationTreeRoots = ParentTreeRoots;
        col.LocationTreeVersion = ParentTreeVersion;
        col.LocationPaths = ParentLocationFullPaths;
        col.DisabledLocationNodeIds = forEditingLocationId is Guid id
            ? LocationHierarchyHelper.GetSubtreeIds(id, _locationTreeCache.Current.AllItems)
            : DisabledParentIds;
    }

    /// <inheritdoc />
    protected override void OnInlineCellSaved() => OnRowDataSaved();

    private static readonly IReadOnlySet<Guid> EmptyExcludedIds = new HashSet<Guid>();

    private void RefreshParentTree(Guid? editingLocationId)
    {
        using var step = PerfDebug.Step(
            "Locations.RefreshParentTree",
            $"editing={editingLocationId?.ToString() ?? "null"}");
        DisabledParentIds = editingLocationId is Guid id
            ? LocationHierarchyHelper.GetSubtreeIds(id, _locationTreeCache.Current.AllItems)
            : EmptyExcludedIds;
        OnPropertyChanged(nameof(DisabledParentIds));
        PerfDebug.Mark("Locations.RefreshParentTree", $"disabled={DisabledParentIds.Count}");
    }

    private static LocationRow MapToRow(
        LocationListItem item,
        IReadOnlyDictionary<Guid, LocationListItem> byId)
    {
        string? parentName = null;
        if (item.ParentId is Guid parentId && byId.TryGetValue(parentId, out var parent))
            parentName = parent.Name;

        return new LocationRow
        {
            Id = item.Id,
            IsActive = item.IsActive,
            Name = item.Name,
            Code = item.Code,
            ParentId = item.ParentId,
            ParentName = parentName,
            FullPath = item.FullPath ?? item.Name,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };
    }

    private static string ResolveErrorMessage(string key) =>
        ResourceStrings.Get(key);

    private string? ValidateName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Locations_Validation_NameRequired");

        if (value.Length > NameMaxLength)
            return ResolveErrorMessage("Locations_Validation_NameRequired");

        return null;
    }

    private string? ValidateCode(string? value)
    {
        if (value is not null && value.Length > CodeMaxLength)
            return ResolveErrorMessage("Locations_Validation_NameRequired");

        return null;
    }

    private static Guid? ParseOptionalParentId(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Guid.TryParse(value, out var id)
                ? id
                : null;
}
