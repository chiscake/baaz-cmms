using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using DevWinUI;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Admin.Users;

public sealed partial class UsersViewModel : CrudWorkbenchViewModelBase<UserRow>
{
    private readonly IProfileAdminService _profileAdminService;
    private readonly IRepairDepartmentCatalogService _catalogService;
    private readonly IAuthService _authService;
    private readonly ILocationTreeCache _locationTreeCache;
    private readonly LocationScopeTreeProjectionCache _scopeProjectionCache;

    public UsersViewModel(
        IProfileAdminService profileAdminService,
        IRepairDepartmentCatalogService catalogService,
        IAuthService authService,
        ILocationTreeCache locationTreeCache,
        LocationScopeTreeProjectionCache scopeProjectionCache)
    {
        _profileAdminService = profileAdminService;
        _catalogService = catalogService;
        _authService = authService;
        _locationTreeCache = locationTreeCache;
        _scopeProjectionCache = scopeProjectionCache;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_Users");

    protected override string ColumnSettingsKey => "Users";

    protected override string ToolbarResourcePrefix => "Users";

    protected override Type? CrudSchemaModelType => typeof(ProfileModel);

    protected override void ApplyManualColumnSemantics(IList<CrudColumnDefinition> columns) =>
        CrudColumnSemantics.ApplyManual(columns, ("Email", Unique: true));

    public const int EmailMaxLength = 320;
    public const int FullNameMaxLength = 200;

    public int EditorEmailMaxLength => EmailMaxLength;
    public int EditorFullNameMaxLength => FullNameMaxLength;

    public override string ShowInactiveLabel => ResourceStrings.Get("Users_ShowBanned");

    // --- Column strings ---

    public string ColumnEmail => ResourceStrings.Get("Users_Column_Email");
    public string ColumnFullName => ResourceStrings.Get("Users_Column_FullName");
    public string ColumnRole => ResourceStrings.Get("Users_Column_Role");
    public string ColumnPhone => ResourceStrings.Get("Users_Column_Phone");
    public string ColumnLocation => ResourceStrings.Get("Users_Column_Location");
    public string ColumnLocationScopes => ResourceStrings.Get("Users_Column_LocationScopes");
    public string ColumnDepartment => ResourceStrings.Get("Users_Column_Department");
    public string ColumnBanned => ResourceStrings.Get("Users_Column_Banned");
    public string ColumnCreatedAt => ResourceStrings.Get("Users_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("Users_Column_UpdatedAt");

    public string ToolbarGeneratePassword => ResourceStrings.Get("Users_Editor_GeneratePassword");

    public override string ToolbarDeleteLabel =>
        BuildToggleArchiveToolbarLabel(
            "Users_Toolbar_Ban",
            "Users_Toolbar_Ban",
            "Users_Toolbar_Unban");

    public override string ToolbarHardDeleteLabel =>
        string.Format(ResourceStrings.Get("Users_Toolbar_HardDelete"), SelectedCount);

    public string EditorLabelEmail => ResourceStrings.Get("Users_Editor_Email");
    public string EditorLabelPassword => ResourceStrings.Get("Users_Editor_Password");
    public string EditorLabelFullName => ResourceStrings.Get("Users_Editor_FullName");
    public string EditorLabelRole => ResourceStrings.Get("Users_Editor_Role");
    public string EditorLabelLocation => ResourceStrings.Get("Users_Editor_Location");
    public string EditorLabelLocationScopes => ResourceStrings.Get("Users_Editor_LocationScopes");
    public string EditorLabelDepartment => ResourceStrings.Get("Users_Editor_Department");
    public string EditorLabelPhone => ResourceStrings.Get("Users_Editor_Phone");
    public string EditorDepartmentPlaceholder => ResourceStrings.Get("Common_SelectDepartment");
    public string EditorClearLocation => ResourceStrings.Get("Locations_Editor_ClearParent");

    public int LocationTreeVersion => _locationTreeCache.Current.Version;

    public IReadOnlyList<LocationTreeItem> LocationTreeRoots =>
        _locationTreeCache.Current.ActiveRoots;

    public IReadOnlyDictionary<Guid, string> LocationFullPaths =>
        _locationTreeCache.Current.FullPaths;

    public LocationScopeTreeProjection ScopeTreeProjection =>
        _scopeProjectionCache.Get(_locationTreeCache.Current);

    private readonly HashSet<Guid> _editorScopeLocationIds = [];

    public IReadOnlySet<Guid> EditorScopeLocationIds => _editorScopeLocationIds;

    public IReadOnlySet<Guid> DisabledLocationIds { get; } = new HashSet<Guid>();

    public ObservableCollection<RepairDepartmentListItem> Departments { get; } = [];
    public ObservableCollection<UserRoleOption> RoleOptions { get; } = [];

    [ObservableProperty]
    public partial string EditorEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorFullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial UserRoleOption? EditorRole { get; set; }

    [ObservableProperty]
    public partial Guid? EditorLocationId { get; set; }

    [ObservableProperty]
    public partial RepairDepartmentListItem? EditorDepartment { get; set; }

    public bool ShowDepartmentEditor =>
        string.Equals(EditorRole?.Value, "dispatcher", StringComparison.OrdinalIgnoreCase);

    public bool ShowScopeEditor =>
        string.Equals(EditorRole?.Value, "requester", StringComparison.OrdinalIgnoreCase)
        || string.Equals(EditorRole?.Value, "dispatcher", StringComparison.OrdinalIgnoreCase);

    partial void OnEditorRoleChanged(UserRoleOption? value)
    {
        OnPropertyChanged(nameof(ShowDepartmentEditor));
        OnPropertyChanged(nameof(ShowScopeEditor));
        if (!ShowDepartmentEditor)
            EditorDepartment = null;
        if (!ShowScopeEditor)
            SetEditorScopeLocationIds([]);
    }

    public void SetEditorScopeLocationIds(IEnumerable<Guid> locationIds)
    {
        var incoming = locationIds as IReadOnlyCollection<Guid> ?? locationIds.ToList();
        var projection = ScopeTreeProjection;
        var expanded = projection.NodesById.Count > 0 && incoming.Count > 0
            ? LocationScopeSelectionHelper.ExpandAnchorsToSelection(incoming, projection.NodesById)
            : incoming.ToHashSet();

        if (_editorScopeLocationIds.SetEquals(expanded))
            return;

        _editorScopeLocationIds.Clear();
        foreach (var id in expanded)
            _editorScopeLocationIds.Add(id);
        OnPropertyChanged(nameof(EditorScopeLocationIds));
    }

    public void GeneratePassword()
    {
        var generator = new PasswordGenerator();
        EditorPassword = generator.Generate(new PasswordOptions
        {
            Length = 12,
            CharacterSets = PasswordCharacterSet.UpperCase
                | PasswordCharacterSet.LowerCase
                | PasswordCharacterSet.Numbers
                | PasswordCharacterSet.Special,
        });
    }

    public async Task BanRowAsync(UserRow row)
    {
        var result = row.IsActive
            ? await _profileAdminService.BanUserAsync(row.Id)
            : await _profileAdminService.UnbanUserAsync(row.Id);

        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        ReplaceRow(row.Id, WithBanned(row, row.IsActive));
        NotifyRowDataChanged();
    }

    public async Task DeleteRowAsync(UserRow row, CancellationToken ct = default)
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

    public bool CanEditRow(UserRow row) =>
        !row.IsAdminAccount && Permissions.CanEdit;

    protected override bool CanOpenRow(UserRow row) => CanEditRow(row);

    public bool CanMutateRow(UserRow row) =>
        row.IsSelectable && Permissions.CanArchive;

    public override bool CanInlineEditCell(ICrudRow row, string columnKey)
    {
        if (row is not UserRow userRow || !CanEditRow(userRow))
            return false;

        if (columnKey == "Department")
            return string.Equals(userRow.Role, "dispatcher", StringComparison.OrdinalIgnoreCase)
                && base.CanInlineEditCell(row, columnKey);

        if (columnKey == "LocationScopes")
            return RoleUsesLocationScopes(userRow.Role)
                && base.CanInlineEditCell(row, columnKey);

        return base.CanInlineEditCell(row, columnKey);
    }

    public override string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) =>
        columnKey switch
        {
            "Email" => ValidateEmail(value),
            "FullName" => ValidateFullName(value),
            _ => null,
        };

    protected override async Task<bool> SaveInlineCellCoreAsync(
        ICrudRow row, string columnKey, string? newValue, CancellationToken ct)
    {
        if (row is not UserRow userRow || !CanEditRow(userRow))
            return false;

        var validationError = columnKey switch
        {
            "Email" => ValidateEmail(newValue),
            "FullName" => ValidateFullName(newValue),
            _ => null,
        };

        if (validationError is not null)
        {
            InfoBanner.Report(validationError, InfoBarSeverity.Error);
            return false;
        }

        switch (columnKey)
        {
            case "Email":
                {
                    var result = await _profileAdminService.UpdateUserEmailAsync(
                        userRow.Id, newValue!.Trim(), ct);
                    if (!result.IsSuccess)
                    {
                        InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                        return false;
                    }

                    CommitUserRow(userRow, result.Value!);
                    return true;
                }
            case "FullName":
                return await UpdateProfileFromRowAsync(userRow, new ProfileEditInput
                {
                    FullName = newValue!.Trim(),
                    Role = userRow.Role,
                    Phone = userRow.Phone,
                    LocationId = userRow.LocationId,
                    RepairDepartmentId = userRow.RepairDepartmentId,
                }, ct);
            case "Location" when Guid.TryParse(newValue, out var locationId):
                return await UpdateProfileFromRowAsync(userRow, new ProfileEditInput
                {
                    FullName = userRow.FullName,
                    Role = userRow.Role,
                    Phone = userRow.Phone,
                    LocationId = locationId,
                    RepairDepartmentId = userRow.RepairDepartmentId,
                }, ct);
            case "LocationScopes" when RoleUsesLocationScopes(userRow.Role):
                {
                    var scopeIds = LocationScopeIdsWireFormat.Parse(newValue);
                    var normalized = scopeIds.Count > 0
                        ? LocationHierarchyHelper.NormalizeScopeAnchors(
                            scopeIds,
                            _locationTreeCache.Current.AllItems)
                        : [];

                    return await UpdateProfileFromRowAsync(userRow, new ProfileEditInput
                    {
                        FullName = userRow.FullName,
                        Role = userRow.Role,
                        Phone = userRow.Phone,
                        LocationId = userRow.LocationId,
                        RepairDepartmentId = userRow.RepairDepartmentId,
                        LocationScopeIds = normalized,
                    }, ct);
                }
            case "Department" when Guid.TryParse(newValue, out var deptId):
                {
                    if (!string.Equals(userRow.Role, "dispatcher", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return await UpdateProfileFromRowAsync(userRow, new ProfileEditInput
                    {
                        FullName = userRow.FullName,
                        Role = userRow.Role,
                        Phone = userRow.Phone,
                        LocationId = userRow.LocationId,
                        RepairDepartmentId = deptId,
                    }, ct);
                }
        }

        return false;
    }

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Email",
            Header = ColumnEmail,
            DataTypeLabel = "text",
            DesiredWidth = 220,
            IsSortable = true,
            IsFilterable = true,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = EmailMaxLength,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "FullName",
            Header = ColumnFullName,
            DataTypeLabel = "text",
            DesiredWidth = 200,
            IsSortable = true,
            IsFilterable = true,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = FullNameMaxLength,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Role",
            Header = ColumnRole,
            DataTypeLabel = "enum",
            DesiredWidth = 130,
            IsSortable = true,
            IsFilterable = true,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Phone",
            Header = ColumnPhone,
            DataTypeLabel = "text",
            DesiredWidth = 140,
            IsSortable = true,
            IsFilterable = true,
            IsVisibleByDefault = false,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Location",
            Header = ColumnLocation,
            DataTypeLabel = "fk",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.LocationTree,
            AllowClearLocationSelection = false,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "LocationScopes",
            Header = ColumnLocationScopes,
            DataTypeLabel = "fk",
            DesiredWidth = 220,
            IsSortable = false,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.LocationScopeTree,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Department",
            Header = ColumnDepartment,
            DataTypeLabel = "fk",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            IsVisibleByDefault = false,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.EnumList,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Banned",
            Header = ColumnBanned,
            DataTypeLabel = "bool",
            DesiredWidth = 90,
            IsSortable = false,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Bool,
        });
        CrudColumnTemplates.AppendAuditColumns(
            Columns,
            ColumnCreatedAt,
            ColumnUpdatedAt,
            c =>
            {
                c.IsSortable = true;
                c.IsVisibleByDefault = true;
                c.DesiredWidth = 150;
            },
            c =>
            {
                c.IsSortable = true;
                c.IsVisibleByDefault = true;
                c.DesiredWidth = 150;
            });
        RefreshLocationColumnTree();
    }

    protected override void InitPermissions()
    {
        var role = _authService.CurrentProfile?.Role;
        Permissions = new CrudPermissions
        {
            CanCreate = role == UserRole.Admin,
            CanUpdate = role == UserRole.Admin,
            CanEdit = role == UserRole.Admin,
            CanArchive = role == UserRole.Admin,
            CanInlineEdit = role == UserRole.Admin,
            CanBulkArchive = role == UserRole.Admin,
            CanHardDelete = role == UserRole.Admin,
        };
        OnPropertyChanged(nameof(Permissions));
    }

    protected override string GetActiveStatusColumnKey() => "Banned";

    protected override bool ShouldForceShowInactiveForColumnFilter(
        CrudColumnDefinition col, CrudColumnFilter filter)
        => col.Key == "Banned"
            && col.FilterKind == CrudColumnFilterKind.Bool
            && string.Equals(filter.Value, "true", StringComparison.OrdinalIgnoreCase);

    protected override async Task LoadDataAsync(CancellationToken ct)
    {
        await EnsureLookupsAsync(ct, forceReloadLocations: _allRows.Count > 0);

        var result = await _profileAdminService.GetProfilesAsync(ct);
        if (!result.IsSuccess)
        {
            var error = result.Error!;
            Debug.WriteLine($"[Users] GetProfilesAsync failed: {error.MessageKey} ({error.Detail})");
            var message = IsEdgeFunctionUnavailable(error)
                ? ResolveErrorMessage("Users_Error_EdgeFunctionUnavailable")
                : ResolveErrorMessage(error.MessageKey);
            if (!IsEdgeFunctionUnavailable(error)
                && !string.IsNullOrWhiteSpace(error.Detail)
                && error.Code == DataErrorCode.Network)
            {
                message = $"{message} {error.Detail}";
            }

            throw new InvalidOperationException(message);
        }

        var currentUserId = _authService.CurrentProfile?.Id;
        _allRows.Clear();
        foreach (var p in result.Value!)
            _allRows.Add(MapToRow(p, currentUserId));
    }

    protected override async Task<bool> SaveAsync(bool isNew, CancellationToken ct)
    {
        if (isNew)
        {
            if (EditorLocationId is null)
            {
                EditorError = ResolveErrorMessage("Users_Validation_LocationRequired");
                return false;
            }

            var emailError = ValidateEmail(EditorEmail);
            if (emailError is not null)
            {
                EditorError = emailError;
                return false;
            }

            var fullNameError = ValidateFullName(EditorFullName);
            if (fullNameError is not null)
            {
                EditorError = fullNameError;
                return false;
            }

            var createInput = new CreateUserInput
            {
                Email = EditorEmail.Trim(),
                Password = EditorPassword,
                FullName = EditorFullName.Trim(),
                Role = EditorRole?.Value ?? "requester",
                LocationId = EditorLocationId.Value,
                LocationScopeIds = GetNormalizedEditorScopeIds(),
                Phone = string.IsNullOrWhiteSpace(EditorPhone) ? null : EditorPhone.Trim(),
                RepairDepartmentId = EditorDepartment?.Id,
            };

            var result = await _profileAdminService.CreateUserAsync(createInput, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }

            _allRows.Insert(0, MapToRow(result.Value!, _authService.CurrentProfile?.Id));
            return true;
        }

        if (EditingRow is null)
            return false;

        var trimmedEmail = EditorEmail.Trim();
        var editEmailError = ValidateEmail(trimmedEmail);
        if (editEmailError is not null)
        {
            EditorError = editEmailError;
            return false;
        }

        var editFullNameError = ValidateFullName(EditorFullName);
        if (editFullNameError is not null)
        {
            EditorError = editFullNameError;
            return false;
        }

        var row = EditingRow;
        if (!string.Equals(trimmedEmail, row.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _profileAdminService.UpdateUserEmailAsync(row.Id, trimmedEmail, ct);
            if (!emailResult.IsSuccess)
            {
                EditorError = ResolveErrorMessage(emailResult.Error!.MessageKey);
                return false;
            }

            CommitUserRow(row, emailResult.Value!);
            var rowIdx = FindRowIndex(row.Id);
            if (rowIdx >= 0)
                row = _allRows[rowIdx];
        }

        var updateInput = new ProfileEditInput
        {
            FullName = EditorFullName.Trim(),
            Role = EditorRole?.Value ?? row.Role,
            Phone = string.IsNullOrWhiteSpace(EditorPhone) ? null : EditorPhone.Trim(),
            LocationId = EditorLocationId ?? row.LocationId,
            LocationScopeIds = RoleUsesLocationScopes(EditorRole?.Value)
                ? GetNormalizedEditorScopeIds()
                : [],
            RepairDepartmentId = EditorDepartment?.Id,
        };

        return await UpdateProfileFromRowAsync(row, updateInput, ct, editorErrors: true);
    }

    protected override async Task<bool> ArchiveAsync(IReadOnlyList<UserRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = row.IsActive
                ? await _profileAdminService.BanUserAsync(row.Id, ct)
                : await _profileAdminService.UnbanUserAsync(row.Id, ct);

            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }

            ReplaceRow(row.Id, WithBanned(row, row.IsActive));
        }

        return true;
    }

    protected override async Task<bool> DeleteAsync(IReadOnlyList<UserRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _profileAdminService.DeleteUserAsync(row.Id, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }

            var idx = IndexOf(_allRows, row.Id);
            if (idx >= 0)
                _allRows.RemoveAt(idx);
        }

        return true;
    }

    protected override IEnumerable<UserRow> ApplyFilter(IEnumerable<UserRow> source)
    {
        var q = FilterText.Trim();
        if (string.IsNullOrEmpty(q))
            return source;

        return source.Where(r =>
            r.Email.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            r.FullName.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            r.RoleDisplay.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.Phone?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.LocationName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            (r.RepairDepartmentName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
    }

    protected override IEnumerable<UserRow> ApplySort(IEnumerable<UserRow> source)
    {
        if (string.IsNullOrEmpty(SortColumnKey)) return source;

        if (SortColumnKey == "Id")
        {
            return SortDirection == SortDirection.Ascending
                ? source.OrderBy(r => r.Id)
                : source.OrderByDescending(r => r.Id);
        }

        source = ApplyDateTimeColumnSort(source, "CreatedAt", r => r.CreatedAt);
        if (string.Equals(SortColumnKey, "CreatedAt", StringComparison.Ordinal))
            return source;

        source = ApplyDateTimeColumnSort(source, "UpdatedAt", r => r.UpdatedAt);
        if (string.Equals(SortColumnKey, "UpdatedAt", StringComparison.Ordinal))
            return source;

        return base.ApplySort(source);
    }

    protected override string GetNewRecordTitle() => ResourceStrings.Get("Users_Editor_Title_New");
    protected override string GetEditRecordTitle() => ResourceStrings.Get("Users_Editor_Title_Edit");

    protected override void OnNewRecordOpened()
    {
        EditorEmail = string.Empty;
        EditorPassword = string.Empty;
        EditorFullName = string.Empty;
        EditorPhone = string.Empty;
        EditorRole = RoleOptions.FirstOrDefault();
        EditorLocationId = null;
        SetEditorScopeLocationIds([]);
        EditorDepartment = null;
    }

    protected override void OnRowOpened(UserRow row)
    {
        EditorEmail = row.Email;
        EditorPassword = string.Empty;
        EditorFullName = row.FullName;
        EditorPhone = row.Phone ?? string.Empty;
        EditorRole = RoleOptions.FirstOrDefault(r => r.Value == row.Role)
            ?? RoleOptions.FirstOrDefault();
        EditorLocationId = row.LocationId;
        SetEditorScopeLocationIds(row.LocationScopeIds);
        EditorDepartment = Departments.FirstOrDefault(d => d.Id == row.RepairDepartmentId);
    }

    private void NotifyLocationTreeChanged()
    {
        OnPropertyChanged(nameof(LocationTreeVersion));
        OnPropertyChanged(nameof(LocationTreeRoots));
        OnPropertyChanged(nameof(LocationFullPaths));
        OnPropertyChanged(nameof(ScopeTreeProjection));
        RefreshLocationColumnTree();
    }

    private async Task EnsureLocationCacheAsync(CancellationToken ct, bool forceReload = false)
    {
        var before = _locationTreeCache.Current.Version;
        if (forceReload)
            await _locationTreeCache.InvalidateAndReloadAsync(ct);
        else
            await _locationTreeCache.EnsureLoadedAsync(ct);

        if (_locationTreeCache.Current.Version != before)
            NotifyLocationTreeChanged();

        // InitColumns() сбрасывает метаданные LocationTree на колонке — синхронизируем после каждой загрузки кэша.
        RefreshLocationColumnTree();
    }

    private IReadOnlyList<Guid> GetNormalizedEditorScopeIds()
    {
        if (_editorScopeLocationIds.Count == 0)
            return [];

        return LocationHierarchyHelper.NormalizeScopeAnchors(
            _editorScopeLocationIds,
            _locationTreeCache.Current.AllItems);
    }

    private static bool RoleUsesLocationScopes(string? role) =>
        string.Equals(role, "requester", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "dispatcher", StringComparison.OrdinalIgnoreCase);

    private string BuildScopeSummary(IReadOnlyList<Guid> scopeIds, IReadOnlyList<string>? serverLabels = null)
    {
        if (scopeIds.Count == 0)
            return "—";

        var selected = scopeIds.ToHashSet();
        var labels = LocationScopeSelectionHelper.BuildCollapsedDisplayLabels(
            LocationTreeRoots,
            selected,
            id => LocationFullPaths.TryGetValue(id, out var path) ? path : id.ToString());

        if (labels.Count == 0 && serverLabels is { Count: > 0 })
            labels = serverLabels;

        if (labels.Count == 0)
            labels = ResolveScopeLabels(scopeIds);

        return LocationScopeSelectionHelper.FormatMultiline(labels);
    }

    private IReadOnlyList<string> ResolveScopeLabels(IReadOnlyList<Guid> scopeIds)
    {
        if (scopeIds.Count == 0)
            return [];

        return scopeIds
            .Select(id => LocationFullPaths.TryGetValue(id, out var path) ? path : id.ToString())
            .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private string? ResolveLocationDisplayName(Guid? locationId)
    {
        if (locationId is null)
            return null;

        if (_locationTreeCache.Current.FullPaths.TryGetValue(locationId.Value, out var path))
            return path;

        if (_locationTreeCache.Current.ById.TryGetValue(locationId.Value, out var item))
            return item.FullPath ?? item.Name;

        return null;
    }

    private async Task EnsureLookupsAsync(CancellationToken ct, bool forceReloadLocations = false)
    {
        if (RoleOptions.Count == 0)
        {
            RoleOptions.Add(new UserRoleOption("requester", ResourceStrings.Get("Users_Role_Requester")));
            RoleOptions.Add(new UserRoleOption("dispatcher", ResourceStrings.Get("Users_Role_Dispatcher")));
        }

        await EnsureLocationCacheAsync(ct, forceReloadLocations);

        if (Departments.Count == 0)
        {
            var deptResult = await _catalogService.GetRepairDepartmentsAsync(ct);
            if (deptResult.IsSuccess)
            {
                foreach (var d in deptResult.Value!)
                    Departments.Add(d);
                RefreshDepartmentColumnOptions();
            }
        }
        else
        {
            RefreshDepartmentColumnOptions();
        }
    }

    private string? ResolveDepartmentDisplayName(Guid? departmentId)
    {
        if (departmentId is null)
            return null;

        return Departments.FirstOrDefault(d => d.Id == departmentId.Value)?.Name;
    }

    private void RefreshDepartmentColumnOptions()
    {
        var col = Columns.FirstOrDefault(c => c.Key == "Department");
        if (col is null)
            return;

        col.EnumOptions = Departments
            .Select(d => new CrudEnumOption { Value = d.Id.ToString(), Label = d.Name })
            .ToList();
        OnPropertyChanged(nameof(Columns));
    }

    private UserRow MapToRow(ProfileListItem p, Guid? currentUserId)
    {
        var scopeLabels = p.LocationScopeLabels.Count > 0
            ? p.LocationScopeLabels
            : null;

        return new UserRow
        {
            Id = p.Id,
            Email = p.Email,
            FullName = p.FullName,
            Role = p.Role,
            RoleDisplay = FormatRole(p.Role),
            Phone = p.Phone,
            LocationId = p.LocationId,
            LocationName = ResolveLocationDisplayName(p.LocationId) ?? p.LocationName,
            LocationScopeIds = p.LocationScopeIds,
            LocationScopeSummary = BuildScopeSummary(p.LocationScopeIds, scopeLabels),
            RepairDepartmentId = p.RepairDepartmentId,
            RepairDepartmentName = p.RepairDepartmentName,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            IsBanned = p.IsBanned,
            IsAdminAccount = p.IsAdminAccount,
            IsCurrentUser = currentUserId.HasValue && p.Id == currentUserId.Value,
        };
    }

    private ProfileListItem MergeListItem(UserRow existing, ProfileListItem updated)
    {
        var locationId = updated.LocationId ?? existing.LocationId;
        return new ProfileListItem
        {
            Id = updated.Id,
            Email = string.IsNullOrEmpty(updated.Email) ? existing.Email : updated.Email,
            FullName = updated.FullName,
            Role = updated.Role,
            Phone = updated.Phone,
            LocationId = locationId,
            LocationName = ResolveLocationDisplayName(locationId)
                ?? updated.LocationName
                ?? existing.LocationName,
            LocationScopeIds = updated.LocationScopeIds,
            LocationScopeLabels = [],
            RepairDepartmentId = updated.RepairDepartmentId ?? existing.RepairDepartmentId,
            RepairDepartmentName = ResolveDepartmentDisplayName(updated.RepairDepartmentId ?? existing.RepairDepartmentId)
                ?? updated.RepairDepartmentName
                ?? existing.RepairDepartmentName,
            CreatedAt = updated.CreatedAt ?? existing.CreatedAt,
            UpdatedAt = updated.UpdatedAt ?? existing.UpdatedAt,
            IsBanned = existing.IsBanned,
            IsAdminAccount = updated.IsAdminAccount,
        };
    }

    private static UserRow WithBanned(UserRow row, bool wasActive) => new()
    {
        Id = row.Id,
        Email = row.Email,
        FullName = row.FullName,
        Role = row.Role,
        RoleDisplay = row.RoleDisplay,
        Phone = row.Phone,
        LocationId = row.LocationId,
        LocationName = row.LocationName,
        LocationScopeIds = row.LocationScopeIds,
        LocationScopeSummary = row.LocationScopeSummary,
        RepairDepartmentId = row.RepairDepartmentId,
        RepairDepartmentName = row.RepairDepartmentName,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        IsBanned = wasActive,
        IsAdminAccount = row.IsAdminAccount,
        IsCurrentUser = row.IsCurrentUser,
    };

    private void RefreshLocationColumnTree()
    {
        foreach (var col in Columns)
        {
            if (col.Key == "Location")
            {
                col.LocationTreeRoots = LocationTreeRoots;
                col.LocationTreeVersion = LocationTreeVersion;
                col.LocationPaths = LocationFullPaths;
                col.DisabledLocationNodeIds = DisabledLocationIds;
            }
            else if (col.Key == "LocationScopes")
            {
                col.LocationTreeRoots = LocationTreeRoots;
                col.LocationTreeVersion = LocationTreeVersion;
                col.LocationPaths = LocationFullPaths;
                col.ScopeTreeProjection = ScopeTreeProjection;
            }
        }

        OnPropertyChanged(nameof(Columns));
    }

    private async Task<bool> UpdateProfileFromRowAsync(
        UserRow source,
        ProfileEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var updateResult = await _profileAdminService.UpdateProfileAsync(source.Id, input, ct);
        if (!updateResult.IsSuccess)
        {
            var message = ResolveErrorMessage(updateResult.Error!.MessageKey);
            if (editorErrors)
                EditorError = message;
            else
                InfoBanner.Report(message, InfoBarSeverity.Error);
            return false;
        }

        CommitUserRow(source, updateResult.Value!);
        return true;
    }

    private void CommitUserRow(UserRow source, ProfileListItem updated)
    {
        var currentUserId = _authService.CurrentProfile?.Id;
        CommitRowUpdate(source.Id, MapToRow(MergeListItem(source, updated), currentUserId));
    }

    private void ReplaceRow(Guid id, UserRow newRow)
    {
        var idx = IndexOf(_allRows, id);
        if (idx >= 0)
            _allRows[idx] = newRow;
    }

    private static int IndexOf(ObservableCollection<UserRow> collection, Guid id)
    {
        for (int i = 0; i < collection.Count; i++)
            if (collection[i].Id == id) return i;
        return -1;
    }

    private static string FormatRole(string role) => role switch
    {
        "admin" => ResourceStrings.Get("Users_Role_Admin"),
        "dispatcher" => ResourceStrings.Get("Users_Role_Dispatcher"),
        _ => ResourceStrings.Get("Users_Role_Requester"),
    };

    private static bool IsEdgeFunctionUnavailable(DataError error) =>
        error.Code == DataErrorCode.Network
        && error.Detail?.Contains("name resolution", StringComparison.OrdinalIgnoreCase) == true;

    private static string ResolveErrorMessage(string key)
    {
        var value = ResourceStrings.Get(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    private string? ValidateEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Users_Validation_EmailRequired");

        if (value.Length > EmailMaxLength)
            return ResolveErrorMessage("Users_Validation_EmailRequired");

        return null;
    }

    private string? ValidateFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Users_Validation_FullNameRequired");

        if (value.Length > FullNameMaxLength)
            return ResolveErrorMessage("Users_Validation_FullNameRequired");

        return null;
    }
}

public sealed record UserRoleOption(string Value, string DisplayName);
