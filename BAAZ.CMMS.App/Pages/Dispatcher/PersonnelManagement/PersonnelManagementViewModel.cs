using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Dispatcher.PersonnelManagement;

public sealed partial class PersonnelManagementViewModel : CrudWorkbenchViewModelBase<PersonnelRow>
{
    private readonly ITechnicianCatalogService _technicianCatalog;
    private readonly IRepairDepartmentCatalogService _departmentCatalog;
    private readonly IAuthService _authService;

    public PersonnelManagementViewModel(
        ITechnicianCatalogService technicianCatalog,
        IRepairDepartmentCatalogService departmentCatalog,
        IAuthService authService)
    {
        _technicianCatalog = technicianCatalog;
        _departmentCatalog = departmentCatalog;
        _authService = authService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_Personnel");

    // --- Column strings ---

    public string ColumnFullName => ResourceStrings.Get("Personnel_Column_FullName");
    public string ColumnSpecialty => ResourceStrings.Get("Personnel_Column_Specialty");
    public string ColumnDepartment => ResourceStrings.Get("Personnel_Column_Department");
    public string ColumnActive => ResourceStrings.Get("Personnel_Column_Active");
    public string ColumnCreatedAt => ResourceStrings.Get("Personnel_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("Personnel_Column_UpdatedAt");

    protected override string ToolbarResourcePrefix => "Personnel";

    public override string ToolbarDeleteLabel =>
        string.Format(ResourceStrings.Get("Personnel_Toolbar_Delete"), SelectedCount);
    public override string ToolbarHardDeleteLabel =>
        string.Format(ResourceStrings.Get("Personnel_Toolbar_HardDelete"), SelectedCount);
    public override string ShowInactiveLabel => ResourceStrings.Get("Personnel_ShowInactive");
    public string EditorLabelFullName => ResourceStrings.Get("Personnel_Editor_FullName");
    public string EditorLabelSpecialty => ResourceStrings.Get("Personnel_Editor_Specialty");
    public string EditorLabelDepartment => ResourceStrings.Get("Personnel_Editor_Department");
    public string EditorLabelIsActive => ResourceStrings.Get("Personnel_Editor_IsActive");
    public string EditorDepartmentPlaceholder => ResourceStrings.Get("Common_SelectDepartment");

    // --- Role flags ---

    public bool IsAdminRole => _authService.CurrentProfile?.Role == UserRole.Admin;
    public bool IsDispatcherRole => _authService.CurrentProfile?.Role == UserRole.Dispatcher;

    // --- Departments for admin ComboBox ---

    public ObservableCollection<RepairDepartmentListItem> Departments { get; } = [];

    // --- Editor fields ---

    [ObservableProperty]
    public partial string EditorFullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorSpecialty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RepairDepartmentListItem? EditorDepartment { get; set; }

    [ObservableProperty]
    public partial bool EditorIsActive { get; set; } = true;

    /// <summary>Для диспетчера — read-only название отдела.</summary>
    public string DispatcherDepartmentName =>
        _authService.CurrentProfile?.RepairDepartmentName ?? string.Empty;

    // --- Row-level archive/restore (context menu) ---

    public async Task SetRowActiveAsync(PersonnelRow row, bool isActive)
    {
        var result = await _technicianCatalog.SetTechnicianActiveAsync(row.Id, isActive);
        if (!result.IsSuccess)
            return;

        var idx = IndexOf(_allRows, row.Id);
        if (idx >= 0)
            _allRows[idx] = WithActive(_allRows[idx], isActive);
        RefreshFilteredRows();
    }

    /// <summary>Безвозвратное удаление (только admin, подтверждение — на странице).</summary>
    public async Task DeleteRowAsync(PersonnelRow row, CancellationToken ct = default)
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

    // --- Inline cell save ---

    protected override async Task<bool> SaveInlineCellCoreAsync(
        ICrudRow row, string columnKey, string? newValue, CancellationToken ct)
    {
        if (row is not PersonnelRow techRow) return false;

        switch (columnKey)
        {
            case "FullName" or "Specialty":
                {
                    var validationError = columnKey == "FullName"
                        ? ValidateFullName(newValue)
                        : ValidateSpecialty(newValue);
                    if (validationError is not null)
                    {
                        InfoBanner.Report(validationError, InfoBarSeverity.Error);
                        return false;
                    }

                    return await PersistTechnicianUpdateAsync(techRow, new TechnicianEditInput
                    {
                        FullName = columnKey == "FullName" ? (newValue ?? techRow.FullName) : techRow.FullName,
                        Specialty = columnKey == "Specialty" ? newValue : techRow.Specialty,
                        RepairDepartmentId = techRow.RepairDepartmentId,
                    }, ct);
                }
            case "Department" when Guid.TryParse(newValue, out var deptId):
                return await PersistTechnicianUpdateAsync(techRow, new TechnicianEditInput
                {
                    FullName = techRow.FullName,
                    Specialty = techRow.Specialty,
                    RepairDepartmentId = deptId,
                }, ct);
        }

        return false;
    }

    public override bool CanInlineEditCell(ICrudRow row, string columnKey)
    {
        if (!base.CanInlineEditCell(row, columnKey))
            return false;

        if (columnKey == "Department")
            return Permissions.CanChangeDepartment;

        return true;
    }

    public override string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) =>
        columnKey switch
        {
            "FullName" => ValidateFullName(value),
            "Specialty" => ValidateSpecialty(value),
            _ => null,
        };

    // --- CrudWorkbenchViewModelBase overrides ---

    protected override string ColumnSettingsKey => "Personnel";

    protected override Type? CrudSchemaModelType => typeof(TechnicianModel);

    public const int FullNameMaxLength = 200;
    public const int SpecialtyMaxLength = 200;

    public int EditorFullNameMaxLength => FullNameMaxLength;
    public int EditorSpecialtyMaxLength => SpecialtyMaxLength;

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "FullName",
            Header = ColumnFullName,
            DataTypeLabel = "text",
            DesiredWidth = double.NaN,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = FullNameMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Specialty",
            Header = ColumnSpecialty,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = SpecialtyMaxLength,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        if (IsAdminRole)
            Columns.Add(new CrudColumnDefinition
            {
                Key = "Department",
                Header = ColumnDepartment,
                DataTypeLabel = "fk",
                DesiredWidth = 180,
                IsInlineEditable = true,
                EditKind = CrudColumnEditKind.EnumList,
                FilterKind = CrudColumnFilterKind.Text,
                IsFilterable = true,
            });
        Columns.Add(CrudColumnTemplates.CreateActiveBoolColumn(ColumnActive));
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
    }

    protected override void InitPermissions()
    {
        var role = _authService.CurrentProfile?.Role;
        Permissions = new CrudPermissions
        {
            CanCreate = role is UserRole.Admin or UserRole.Dispatcher,
            CanUpdate = role is UserRole.Admin or UserRole.Dispatcher,
            CanEdit = role is UserRole.Admin or UserRole.Dispatcher,
            CanArchive = role is UserRole.Admin or UserRole.Dispatcher,
            CanInlineEdit = role is UserRole.Admin or UserRole.Dispatcher,
            CanBulkArchive = role is UserRole.Admin or UserRole.Dispatcher,
            CanHardDelete = role == UserRole.Admin,
            CanChangeDepartment = role == UserRole.Admin,
        };
        OnPropertyChanged(nameof(Permissions));
    }

    protected override async Task LoadDataAsync(CancellationToken ct)
    {
        // Загрузить отделы (для ComboBox)
        if (IsAdminRole && Departments.Count == 0)
        {
            var deptResult = await _departmentCatalog.GetRepairDepartmentsAsync(ct);
            if (deptResult.IsSuccess)
            {
                Departments.Clear();
                foreach (var d in deptResult.Value!)
                    Departments.Add(d);
                RefreshDepartmentColumnOptions();
            }
        }

        var result = await _technicianCatalog.GetTechniciansAsync(ct);
        if (!result.IsSuccess)
        {
            Debug.WriteLine($"[Personnel] GetTechniciansAsync failed: {result.Error?.MessageKey} — {result.Error?.Detail}");
            throw new InvalidOperationException(ResolveErrorMessage(result.Error!.MessageKey));
        }

        Debug.WriteLine($"[Personnel] GetTechniciansAsync returned {result.Value!.Count} item(s)");

        _allRows.Clear();
        foreach (var t in result.Value!)
            _allRows.Add(MapToRow(t));
    }

    protected override async Task<bool> SaveAsync(bool isNew, CancellationToken ct)
    {
        var fullNameError = ValidateFullName(EditorFullName);
        if (fullNameError is not null)
        {
            EditorError = fullNameError;
            return false;
        }

        var specialtyError = ValidateSpecialty(EditorSpecialty);
        if (specialtyError is not null)
        {
            EditorError = specialtyError;
            return false;
        }

        var input = new TechnicianEditInput
        {
            FullName = EditorFullName.Trim(),
            Specialty = EditorSpecialty.Trim(),
            RepairDepartmentId = EditorDepartment?.Id,
        };

        if (isNew)
        {
            var result = await _technicianCatalog.CreateTechnicianAsync(input, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }
            _allRows.Insert(0, MapToRow(result.Value!));
            Debug.WriteLine($"[Personnel] Created technician id={result.Value!.Id}, name={result.Value.FullName}");
        }
        else if (EditingRow is not null)
        {
            if (!await PersistTechnicianUpdateAsync(EditingRow, input, ct, editorErrors: true))
                return false;

            if (EditorIsActive != EditingRow.IsActive)
            {
                var activeResult = await _technicianCatalog.SetTechnicianActiveAsync(EditingRow.Id, EditorIsActive, ct);
                if (!activeResult.IsSuccess)
                {
                    EditorError = ResolveErrorMessage(activeResult.Error!.MessageKey);
                    return false;
                }

                var idx = FindRowIndex(EditingRow.Id);
                if (idx >= 0)
                    CommitRowUpdate(EditingRow.Id, WithActive(_allRows[idx], EditorIsActive));
            }
        }

        return true;
    }

    protected override async Task<bool> ArchiveAsync(IReadOnlyList<PersonnelRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _technicianCatalog.SetTechnicianActiveAsync(row.Id, false, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }

            var idx = IndexOf(_allRows, row.Id);
            if (idx >= 0)
                _allRows[idx] = WithActive(_allRows[idx], false);
        }
        return true;
    }

    protected override async Task<bool> DeleteAsync(IReadOnlyList<PersonnelRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _technicianCatalog.DeleteTechnicianAsync(row.Id, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }

            var idx = IndexOf(_allRows, row.Id);
            if (idx >= 0)
                _allRows.RemoveAt(idx);

            if (IsEditorOpen && EditingRow?.Id == row.Id)
            {
                IsEditorOpen = false;
                EditingRow = null;
            }
        }

        return true;
    }

    protected override IEnumerable<PersonnelRow> ApplyFilter(IEnumerable<PersonnelRow> source)
    {
        var q = FilterText.Trim().ToLowerInvariant();
        return source.Where(r =>
            r.FullName.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            r.Specialty.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.RepairDepartmentName?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
    }

    protected override IEnumerable<PersonnelRow> ApplySort(IEnumerable<PersonnelRow> source)
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

    protected override string GetNewRecordTitle() =>
        ResourceStrings.Get("Personnel_Editor_Title_New");

    protected override string GetEditRecordTitle() =>
        ResourceStrings.Get("Personnel_Editor_Title_Edit");

    protected override void OnNewRecordOpened()
    {
        EditorFullName = string.Empty;
        EditorSpecialty = string.Empty;
        EditorDepartment = null;
        EditorIsActive = true;
    }

    protected override void OnRowOpened(PersonnelRow row)
    {
        EditorFullName = row.FullName;
        EditorSpecialty = row.Specialty;
        EditorIsActive = row.IsActive;
        EditorDepartment = Departments.FirstOrDefault(d => d.Id == row.RepairDepartmentId);
    }

    // --- Helpers ---

    private static PersonnelRow MapToRow(TechnicianListItem t) => new()
    {
        Id = t.Id,
        FullName = t.FullName,
        Specialty = t.Specialty,
        IsActive = t.IsActive,
        RepairDepartmentId = t.RepairDepartmentId,
        RepairDepartmentName = t.RepairDepartmentName,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };

    private static PersonnelRow WithActive(PersonnelRow row, bool isActive) => new()
    {
        Id = row.Id,
        FullName = row.FullName,
        Specialty = row.Specialty,
        IsActive = isActive,
        RepairDepartmentId = row.RepairDepartmentId,
        RepairDepartmentName = row.RepairDepartmentName,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
    };

    private async Task<bool> PersistTechnicianUpdateAsync(
        PersonnelRow source,
        TechnicianEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var result = await _technicianCatalog.UpdateTechnicianAsync(source.Id, input, ct);
        if (!result.IsSuccess)
        {
            var message = ResolveErrorMessage(result.Error!.MessageKey);
            if (editorErrors)
                EditorError = message;
            else
                InfoBanner.Report(message, InfoBarSeverity.Error);
            return false;
        }

        CommitRowUpdate(source.Id, MapToRow(result.Value!));
        return true;
    }

    private static int IndexOf(System.Collections.ObjectModel.ObservableCollection<PersonnelRow> collection, Guid id)
    {
        for (int i = 0; i < collection.Count; i++)
            if (collection[i].Id == id) return i;
        return -1;
    }

    private static string ResolveErrorMessage(string key)
    {
        var value = ResourceStrings.Get(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    private string? ValidateFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("Personnel_Validation_FullNameRequired");

        if (value.Length > FullNameMaxLength)
            return ResolveErrorMessage("Personnel_Validation_FullNameRequired");

        return null;
    }

    private string? ValidateSpecialty(string? value)
    {
        if (value is not null && value.Length > SpecialtyMaxLength)
            return ResolveErrorMessage("Personnel_Validation_FullNameRequired");

        return null;
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
}
