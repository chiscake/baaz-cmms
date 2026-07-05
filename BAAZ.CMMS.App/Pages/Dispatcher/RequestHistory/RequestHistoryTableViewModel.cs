using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestHistory;

/// <summary>
/// Таблица истории заявок в режиме <see cref="CrudRowOpenMode.Pick"/> — только просмотр и выбор строки.
/// </summary>
public sealed class RequestHistoryTableViewModel : CrudWorkbenchViewModelBase<RequestRow>
{
    private static readonly string[] SourceStatuses =
    [
        "completed",
        "closed",
        "rejected",
        "cancelled",
    ];

    private readonly IRequestService _requestService;

    public event EventHandler<Guid>? RecordPicked;

    public RequestHistoryTableViewModel(IRequestService requestService)
    {
        _requestService = requestService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_RequestHistory");

    protected override string ColumnSettingsKey => "RequestHistoryTable";

    protected override string ToolbarResourcePrefix => "MyRequests";

    protected override CrudRowOpenMode RowOpenMode => CrudRowOpenMode.Pick;

    public override string ToolbarAdd => string.Empty;
    public override string ToolbarRefresh => ResourceStrings.Get("MyRequests_Table_Refresh");
    public override string ToolbarColumns => ResourceStrings.Get("MyRequests_Table_Columns");
    public override string FilterPlaceholder => ResourceStrings.Get("MyRequests_Search_Placeholder");
    public override string ShowInactiveLabel => string.Empty;

    public string ColumnRequestNumber => ResourceStrings.Get("MyRequests_Column_Number");
    public string ColumnTitle => ResourceStrings.Get("MyRequests_Column_Title");
    public string ColumnStatus => ResourceStrings.Get("MyRequests_Column_Status");
    public string ColumnPriority => ResourceStrings.Get("MyRequests_Column_Priority");
    public string ColumnType => ResourceStrings.Get("MyRequests_Column_Type");
    public string ColumnCreatedAt => ResourceStrings.Get("MyRequests_Column_CreatedAt");
    public string ColumnAssignee => ResourceStrings.Get("MyRequests_Column_Assignee");

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
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "CreatedAt",
            Header = ColumnCreatedAt,
            DataTypeLabel = "timestamptz",
            DesiredWidth = 150,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
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
        });
        Columns.Add(CrudColumnTemplates.CreateHiddenUuidColumn());
    }

    protected override void InitPermissions()
    {
        Permissions = new CrudPermissions();
        OnPropertyChanged(nameof(Permissions));
    }

    protected override async Task LoadDataAsync(CancellationToken ct)
    {
        var items = await _requestService.GetRequestsByStatusesAsync(SourceStatuses, ct);
        _allRows.Clear();
        foreach (var item in items.Take(RequestHistoryViewModel.BrowseLimit))
            _allRows.Add(RequestRow.FromListItem(item));
    }

    protected override Task<bool> SaveAsync(bool isNew, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> ArchiveAsync(IReadOnlyList<RequestRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> DeleteAsync(IReadOnlyList<RequestRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override IEnumerable<RequestRow> ApplySort(IEnumerable<RequestRow> source)
    {
        source = ApplyDateTimeColumnSort(source, "CreatedAt", r => r.CreatedAt);
        if (string.Equals(SortColumnKey, "CreatedAt", StringComparison.Ordinal))
            return source;

        return base.ApplySort(source);
    }

    protected override void OnRecordPicked(RequestRow row)
        => RecordPicked?.Invoke(this, row.Id);

    protected override string GetNewRecordTitle() => string.Empty;
    protected override string GetEditRecordTitle() => string.Empty;
}
