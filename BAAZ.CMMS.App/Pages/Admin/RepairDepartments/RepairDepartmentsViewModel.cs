using System;
using System.Collections.Generic;
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

namespace BAAZ.CMMS.App.Pages.Admin.RepairDepartments;

public sealed partial class RepairDepartmentsViewModel : CrudWorkbenchViewModelBase<RepairDepartmentRow>
{
    private readonly IRepairDepartmentCatalogService _catalogService;
    private readonly IAuthService _authService;

    public RepairDepartmentsViewModel(IRepairDepartmentCatalogService catalogService, IAuthService authService)
    {
        _catalogService = catalogService;
        _authService = authService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_RepairDepartments");

    protected override string ColumnSettingsKey => "RepairDepartments";

    protected override string ToolbarResourcePrefix => "RepairDepartments";

    protected override Type? CrudSchemaModelType => typeof(RepairDepartmentModel);

    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public int EditorNameMaxLength => NameMaxLength;
    public int EditorCodeMaxLength => CodeMaxLength;

    public string ColumnName => ResourceStrings.Get("RepairDepartments_Column_Name");
    public string ColumnCode => ResourceStrings.Get("RepairDepartments_Column_Code");
    public string ColumnActive => ResourceStrings.Get("RepairDepartments_Column_Active");
    public string ColumnCreatedAt => ResourceStrings.Get("RepairDepartments_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("RepairDepartments_Column_UpdatedAt");

    public override string ToolbarHardDeleteLabel =>
        string.Format(ResourceStrings.Get("RepairDepartments_Toolbar_HardDelete"), SelectedCount);
    public override string ShowInactiveLabel => ResourceStrings.Get("RepairDepartments_ShowInactive");
    public string EditorLabelName => ResourceStrings.Get("RepairDepartments_Editor_Name");
    public string EditorLabelCode => ResourceStrings.Get("RepairDepartments_Editor_Code");

    public override string ToolbarDeleteLabel =>
        BuildToggleArchiveToolbarLabel(
            "RepairDepartments_Toolbar_Archive",
            "RepairDepartments_Toolbar_Archive",
            "RepairDepartments_Toolbar_Restore");

    [ObservableProperty]
    public partial string EditorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorCode { get; set; } = string.Empty;

    public async Task SetRowArchivedAsync(RepairDepartmentRow row, bool archive, CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _catalogService.SetRepairDepartmentActiveAsync(row.Id, !archive, ct);
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

    public async Task DeleteRowAsync(RepairDepartmentRow row, CancellationToken ct = default)
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
        if (row is not RepairDepartmentRow deptRow)
            return false;

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

        var input = new RepairDepartmentEditInput
        {
            Name = columnKey == "Name" ? (newValue ?? deptRow.Name) : deptRow.Name,
            Code = columnKey == "Code" ? newValue : deptRow.Code,
        };

        return await PersistDepartmentUpdateAsync(deptRow, input, ct);
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
        Columns.Add(CrudColumnTemplates.CreateActiveBoolColumn(ColumnActive));
        CrudColumnTemplates.AppendAuditColumns(Columns, ColumnCreatedAt, ColumnUpdatedAt);
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

        var input = new RepairDepartmentEditInput
        {
            Name = EditorName.Trim(),
            Code = string.IsNullOrWhiteSpace(EditorCode) ? null : EditorCode.Trim(),
        };

        if (isNew)
        {
            var result = await _catalogService.CreateRepairDepartmentAsync(input, ct);
            if (!result.IsSuccess)
            {
                EditorError = ResolveErrorMessage(result.Error!.MessageKey);
                return false;
            }

            await ReloadRowsAsync(ct);
        }
        else if (EditingRow is not null)
        {
            return await PersistDepartmentUpdateAsync(EditingRow, input, ct, editorErrors: true);
        }

        return true;
    }

    protected override async Task<bool> ArchiveAsync(IReadOnlyList<RepairDepartmentRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _catalogService.SetRepairDepartmentActiveAsync(row.Id, !row.IsActive, ct);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
                return false;
            }
        }

        await ReloadRowsAsync(ct);
        return true;
    }

    protected override async Task<bool> DeleteAsync(IReadOnlyList<RepairDepartmentRow> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var result = await _catalogService.DeleteRepairDepartmentAsync(row.Id, ct);
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

    protected override IEnumerable<RepairDepartmentRow> ApplyFilter(IEnumerable<RepairDepartmentRow> source)
    {
        var q = FilterText.Trim();
        if (string.IsNullOrEmpty(q))
            return source;

        return source.Where(r =>
            r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
            (r.Code?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));
    }

    protected override string GetNewRecordTitle() =>
        ResourceStrings.Get("RepairDepartments_Editor_Title_New");

    protected override string GetEditRecordTitle() =>
        ResourceStrings.Get("RepairDepartments_Editor_Title_Edit");

    protected override void OnNewRecordOpened()
    {
        EditorName = string.Empty;
        EditorCode = string.Empty;
    }

    protected override void OnRowOpened(RepairDepartmentRow row)
    {
        EditorName = row.Name;
        EditorCode = row.Code ?? string.Empty;
    }

    private async Task<bool> PersistDepartmentUpdateAsync(
        RepairDepartmentRow row,
        RepairDepartmentEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var result = await _catalogService.UpdateRepairDepartmentAsync(row.Id, input, ct);
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
        var result = await _catalogService.GetRepairDepartmentsAdminAsync(includeInactive: true, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException(ResolveErrorMessage(result.Error!.MessageKey));

        _allRows.Clear();
        foreach (var item in result.Value!)
            _allRows.Add(MapToRow(item));
    }

    private static RepairDepartmentRow MapToRow(RepairDepartmentAdminListItem item) => new()
    {
        Id = item.Id,
        IsActive = item.IsActive,
        Name = item.Name,
        Code = item.Code,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };

    private static string ResolveErrorMessage(string key) =>
        ResourceStrings.Get(key);

    private string? ValidateName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("RepairDepartments_Validation_NameRequired");

        if (value.Length > NameMaxLength)
            return ResolveErrorMessage("RepairDepartments_Validation_NameRequired");

        return null;
    }

    private string? ValidateCode(string? value)
    {
        if (value is not null && value.Length > CodeMaxLength)
            return ResolveErrorMessage("RepairDepartments_Validation_NameRequired");

        return null;
    }
}
