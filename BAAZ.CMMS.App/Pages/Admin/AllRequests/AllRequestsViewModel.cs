using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Pages.Admin.AllRequests;

public sealed partial class AllRequestsViewModel : CrudWorkbenchViewModelBase<AdminRequestRow>
{
    private static readonly string[] TypeValues = ["breakdown", "service", "inspection"];
    private static readonly string[] PriorityValues = ["low", "normal", "high", "critical"];

    public const int RequestNumberMaxLength = 100;

    private readonly IRequestService _requestService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private IReadOnlyList<CrudEnumOption>? _typeEnumOptions;
    private IReadOnlyList<CrudEnumOption>? _priorityEnumOptions;

    public AllRequestsViewModel(
        IRequestService requestService,
        IAuthService authService,
        INavigationService navigationService)
    {
        _requestService = requestService;
        _authService = authService;
        _navigationService = navigationService;
    }

    protected override bool AddNavigatesExternally => true;

    public override string PageTitle => ResourceStrings.Get("Nav_AllRequests");

    protected override string ColumnSettingsKey => "AllRequests";

    protected override string ToolbarResourcePrefix => "AllRequests";

    protected override Type? CrudSchemaModelType => typeof(RequestModel);

    public int EditorRequestNumberMaxLength => RequestNumberMaxLength;

    public string ColumnRequestNumber => ResourceStrings.Get("AllRequests_Column_RequestNumber");
    public string ColumnTitle => ResourceStrings.Get("AllRequests_Column_Title");
    public string ColumnDescription => ResourceStrings.Get("AllRequests_Column_Description");
    public string ColumnStatus => ResourceStrings.Get("AllRequests_Column_Status");
    public string ColumnPriority => ResourceStrings.Get("AllRequests_Column_Priority");
    public string ColumnType => ResourceStrings.Get("AllRequests_Column_Type");
    public string ColumnRequester => ResourceStrings.Get("AllRequests_Column_Requester");
    public string ColumnAssignee => ResourceStrings.Get("AllRequests_Column_Assignee");
    public string ColumnLocation => ResourceStrings.Get("AllRequests_Column_Location");
    public string ColumnAsset => ResourceStrings.Get("AllRequests_Column_Asset");
    public string ColumnRepairZone => ResourceStrings.Get("AllRequests_Column_RepairZone");
    public string ColumnContractorName => ResourceStrings.Get("AllRequests_Column_ContractorName");
    public string ColumnTargetDepartment => ResourceStrings.Get("AllRequests_Column_TargetDepartment");
    public string ColumnCreatedAt => ResourceStrings.Get("AllRequests_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("AllRequests_Column_UpdatedAt");

    public string EditorLabelRequestNumber => ColumnRequestNumber;
    public string EditorLabelTitle => ColumnTitle;
    public string EditorLabelDescription => ColumnDescription;
    public string EditorLabelType => ColumnType;
    public string EditorLabelPriority => ColumnPriority;
    public string EditorLabelStatus => ColumnStatus;
    public string EditorLabelRequester => ColumnRequester;

    public IReadOnlyList<string> TypeLabels { get; } =
    [
        ResourceStrings.Get("RequestType_Breakdown"),
        ResourceStrings.Get("RequestType_Service"),
        ResourceStrings.Get("RequestType_Inspection"),
    ];

    public IReadOnlyList<string> PriorityLabels { get; } =
    [
        ResourceStrings.Get("RequestPriority_Low"),
        ResourceStrings.Get("RequestPriority_Normal"),
        ResourceStrings.Get("RequestPriority_High"),
        ResourceStrings.Get("RequestPriority_Critical"),
    ];

    [ObservableProperty]
    public partial string EditorRequestNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorRequestTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int EditorTypeIndex { get; set; }

    [ObservableProperty]
    public partial int EditorPriorityIndex { get; set; }

    [ObservableProperty]
    public partial string EditorStatusLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorRequesterName { get; set; } = string.Empty;

    private IReadOnlyList<CrudEnumOption> TypeEnumOptions =>
        _typeEnumOptions ??= BuildEnumOptions(TypeValues, TypeLabels);

    private IReadOnlyList<CrudEnumOption> PriorityEnumOptions =>
        _priorityEnumOptions ??= BuildEnumOptions(PriorityValues, PriorityLabels);

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "RequestNumber",
            Header = ColumnRequestNumber,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
            MaxLength = RequestNumberMaxLength,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Title",
            Header = ColumnTitle,
            DataTypeLabel = "text",
            DesiredWidth = 220,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Status",
            Header = ColumnStatus,
            DataTypeLabel = "enum",
            DesiredWidth = 130,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Priority",
            Header = ColumnPriority,
            DataTypeLabel = "enum",
            DesiredWidth = 110,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.EnumList,
            EnumOptions = PriorityEnumOptions,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Type",
            Header = ColumnType,
            DataTypeLabel = "enum",
            DesiredWidth = 120,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsInlineEditable = true,
            EditKind = CrudColumnEditKind.EnumList,
            EnumOptions = TypeEnumOptions,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Requester",
            Header = ColumnRequester,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Assignee",
            Header = ColumnAssignee,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Location",
            Header = ColumnLocation,
            DataTypeLabel = "text",
            DesiredWidth = 180,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Asset",
            Header = ColumnAsset,
            DataTypeLabel = "fk",
            DesiredWidth = 200,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "RepairZone",
            Header = ColumnRepairZone,
            DataTypeLabel = "enum",
            DesiredWidth = 130,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Description",
            Header = ColumnDescription,
            DataTypeLabel = "text",
            DesiredWidth = 220,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "ContractorName",
            Header = ColumnContractorName,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "TargetDepartment",
            Header = ColumnTargetDepartment,
            DataTypeLabel = "fk",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
            IsVisibleByDefault = false,
            EditKind = CrudColumnEditKind.ReadOnly,
        });
        CrudColumnTemplates.AppendAuditColumns(
            Columns,
            ColumnCreatedAt,
            ColumnUpdatedAt,
            createdAt => createdAt.IsVisibleByDefault = true,
            updatedAt => updatedAt.IsVisibleByDefault = false);
    }

    protected override void InitPermissions()
    {
        var isAdmin = _authService.CurrentProfile?.Role == UserRole.Admin;
        Permissions = new CrudPermissions
        {
            CanCreate = true,
            CanEdit = isAdmin,
            CanUpdate = isAdmin,
            CanInlineEdit = isAdmin,
            CanArchive = false,
            CanHardDelete = false,
        };
        OnPropertyChanged(nameof(Permissions));
    }

    protected override async Task LoadDataAsync(CancellationToken ct)
    {
        await ReloadRowsAsync(ct);
    }

    protected override void OnRowOpened(AdminRequestRow row)
    {
        EditorRequestNumber = row.RequestNumber;
        EditorRequestTitle = row.Title;
        EditorDescription = row.Description;
        EditorTypeIndex = Math.Max(0, Array.IndexOf(TypeValues, row.Type));
        EditorPriorityIndex = Math.Max(0, Array.IndexOf(PriorityValues, row.Priority));
        EditorStatusLabel = RequestStatusHelper.GetLabel(row.Status);
        EditorRequesterName = row.RequesterName ?? ResourceStrings.Get("Common_None");

        if (string.IsNullOrEmpty(row.Description))
            _ = LoadEditorDescriptionAsync(row.Id);
    }

    private async Task LoadEditorDescriptionAsync(Guid requestId)
    {
        var detail = await _requestService.GetRequestByIdAsync(requestId);
        if (detail is not null && EditingRow?.Id == requestId)
            EditorDescription = detail.Description;
    }

    protected override async Task<bool> SaveAsync(bool isNew, CancellationToken ct)
    {
        if (isNew || EditingRow is not AdminRequestRow row)
            return false;

        var requestNumberError = ValidateRequestNumber(EditorRequestNumber);
        if (requestNumberError is not null)
        {
            InfoBanner.Report(requestNumberError, InfoBarSeverity.Warning);
            return false;
        }

        var titleError = ValidateTitle(EditorRequestTitle);
        if (titleError is not null)
        {
            InfoBanner.Report(titleError, InfoBarSeverity.Warning);
            return false;
        }

        return await PersistRequestUpdateAsync(
            row,
            BuildEditInputFromEditor(row),
            ct,
            editorErrors: true);
    }

    protected override async Task<bool> SaveInlineCellCoreAsync(
        ICrudRow row,
        string columnKey,
        string? newValue,
        CancellationToken ct)
    {
        if (row is not AdminRequestRow requestRow)
            return false;

        var validationError = ValidateInlineCellValue(row, columnKey, newValue);
        if (validationError is not null)
        {
            InfoBanner.Report(validationError, InfoBarSeverity.Error);
            return false;
        }

        var baseInput = BuildEditInputFromRow(requestRow);
        var input = columnKey switch
        {
            "RequestNumber" => new RequestEditInput
            {
                RequestNumber = newValue?.Trim() ?? requestRow.RequestNumber,
                Title = baseInput.Title,
                Description = baseInput.Description,
                Type = baseInput.Type,
                Priority = baseInput.Priority,
            },
            "Title" => new RequestEditInput
            {
                RequestNumber = baseInput.RequestNumber,
                Title = newValue?.Trim() ?? requestRow.Title,
                Description = baseInput.Description,
                Type = baseInput.Type,
                Priority = baseInput.Priority,
            },
            "Priority" => new RequestEditInput
            {
                RequestNumber = baseInput.RequestNumber,
                Title = baseInput.Title,
                Description = baseInput.Description,
                Type = baseInput.Type,
                Priority = newValue ?? requestRow.Priority,
            },
            "Type" => new RequestEditInput
            {
                RequestNumber = baseInput.RequestNumber,
                Title = baseInput.Title,
                Description = baseInput.Description,
                Type = newValue ?? requestRow.Type,
                Priority = baseInput.Priority,
            },
            _ => baseInput,
        };

        return await PersistRequestUpdateAsync(requestRow, input, ct);
    }

    public override string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) =>
        columnKey switch
        {
            "RequestNumber" => ValidateRequestNumber(value),
            "Title" => ValidateTitle(value),
            _ => null,
        };

    protected override IEnumerable<AdminRequestRow> ApplyFilter(IEnumerable<AdminRequestRow> source)
    {
        var q = FilterText.Trim();
        if (string.IsNullOrEmpty(q))
            return source;

        return source.Where(r =>
            r.RequestNumber.Contains(q, StringComparison.CurrentCultureIgnoreCase)
            || r.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase));
    }

    protected override Task<bool> ArchiveAsync(IReadOnlyList<AdminRequestRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> DeleteAsync(IReadOnlyList<AdminRequestRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override IEnumerable<AdminRequestRow> ApplySort(IEnumerable<AdminRequestRow> source)
    {
        source = ApplyDateTimeColumnSort(source, "CreatedAt", r => r.CreatedAt);
        if (string.Equals(SortColumnKey, "CreatedAt", StringComparison.Ordinal))
            return source;

        source = ApplyDateTimeColumnSort(source, "UpdatedAt", r => r.UpdatedAt);
        if (string.Equals(SortColumnKey, "UpdatedAt", StringComparison.Ordinal))
            return source;

        return base.ApplySort(source);
    }

    protected override void OnAddRequested()
        => _navigationService.NavigateTo("NewRequest");

    protected override string GetNewRecordTitle() => string.Empty;

    protected override string GetEditRecordTitle()
        => string.Format(ResourceStrings.Get("AllRequests_Editor_Title"), EditorRequestNumber);

    private async Task ReloadRowsAsync(CancellationToken ct)
    {
        var items = await _requestService.GetAllRequestsAsync(limit: null, ct);
        _allRows.Clear();
        foreach (var item in items)
            _allRows.Add(AdminRequestRow.FromListItem(item));
    }

    private async Task<bool> PersistRequestUpdateAsync(
        AdminRequestRow source,
        RequestEditInput input,
        CancellationToken ct,
        bool editorErrors = false)
    {
        var result = await _requestService.UpdateRequestFieldsAsync(source.Id, input, ct);
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
        RefreshFilteredRows();
        return true;
    }

    private RequestEditInput BuildEditInputFromRow(AdminRequestRow row) => new()
    {
        RequestNumber = row.RequestNumber,
        Title = row.Title,
        Description = row.Description,
        Type = row.Type,
        Priority = row.Priority,
    };

    private RequestEditInput BuildEditInputFromEditor(AdminRequestRow row) => new()
    {
        RequestNumber = EditorRequestNumber,
        Title = EditorRequestTitle,
        Description = EditorDescription,
        Type = TypeValues[EditorTypeIndex],
        Priority = PriorityValues[EditorPriorityIndex],
    };

    private static IReadOnlyList<CrudEnumOption> BuildEnumOptions(
        IReadOnlyList<string> values,
        IReadOnlyList<string> labels)
    {
        var options = new List<CrudEnumOption>(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            options.Add(new CrudEnumOption
            {
                Value = values[i],
                Label = labels[i],
            });
        }

        return options;
    }

    private static string? ValidateRequestNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("AllRequests_Validation_RequestNumberRequired");

        if (value.Length > RequestNumberMaxLength)
            return ResolveErrorMessage("AllRequests_Validation_RequestNumberRequired");

        return null;
    }

    private static string? ValidateTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ResolveErrorMessage("AllRequests_Validation_TitleRequired");

        return null;
    }

    private static string ResolveErrorMessage(string key)
    {
        var value = ResourceStrings.Get(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }
}
