using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services.AuditLog;

namespace BAAZ.CMMS.App.Pages.Admin.AuditLog;

public sealed class AuditLogTableViewModel : CrudWorkbenchViewModelBase<AuditLogRow>
{
    public const int DefaultLoadLimit = 100;

    private readonly IAuditLogService _auditLogService;

    public event EventHandler<AuditLogRow>? RecordPicked;

    public AuditLogTableViewModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        SetSort("~ChangedAt");
    }

    public override string PageTitle => ResourceStrings.Get("Nav_AuditLog");

    protected override string ColumnSettingsKey => "AuditLogTable";

    protected override string ToolbarResourcePrefix => "AuditLog";

    protected override CrudRowOpenMode RowOpenMode => CrudRowOpenMode.Pick;

    public override string ToolbarAdd => string.Empty;
    public override string ToolbarRefresh => ResourceStrings.Get("CrudGrid_Refresh");
    public override string ToolbarColumns => ResourceStrings.Get("CrudGrid_Columns");
    public override string FilterPlaceholder => ResourceStrings.Get("AuditLog_Search_Placeholder");
    public override string ShowInactiveLabel => string.Empty;

    public string ColumnChangedAt => ResourceStrings.Get("AuditLog_Column_ChangedAt");
    public string ColumnActor => ResourceStrings.Get("AuditLog_Column_Actor");
    public string ColumnOperation => ResourceStrings.Get("AuditLog_Column_Operation");
    public string ColumnTableName => ResourceStrings.Get("AuditLog_Column_TableName");
    public string ColumnRecordKey => ResourceStrings.Get("AuditLog_Column_RecordKey");

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "ChangedAt",
            Header = ColumnChangedAt,
            DataTypeLabel = "timestamptz",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "ActorName",
            Header = ColumnActor,
            DataTypeLabel = "text",
            DesiredWidth = 180,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Operation",
            Header = ColumnOperation,
            DataTypeLabel = "enum",
            DesiredWidth = 120,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "TableName",
            Header = ColumnTableName,
            DataTypeLabel = "text",
            DesiredWidth = 200,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "RecordKey",
            Header = ColumnRecordKey,
            DataTypeLabel = "text",
            DesiredWidth = 260,
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
        var items = await _auditLogService.GetRecentAsync(DefaultLoadLimit, ct);
        _allRows.Clear();
        foreach (var item in items)
        {
            var actorDisplay = string.IsNullOrWhiteSpace(item.ActorName)
                ? ResourceStrings.Get("AuditLog_SystemActor")
                : item.ActorName;
            _allRows.Add(AuditLogRow.FromListItem(item, actorDisplay));
        }
    }

    protected override IEnumerable<AuditLogRow> ApplyFilter(IEnumerable<AuditLogRow> source)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return source;

        var term = FilterText.Trim();
        return source.Where(row =>
            row.GetCellText("ChangedAt").Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || row.ActorName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || row.GetCellText("Operation").Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || row.TableName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || row.RecordKey.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || row.Operation.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    protected override IEnumerable<AuditLogRow> ApplySort(IEnumerable<AuditLogRow> source)
    {
        source = ApplyDateTimeColumnSort(source, "ChangedAt", r => r.ChangedAt);
        if (string.Equals(SortColumnKey, "ChangedAt", StringComparison.Ordinal))
            return source;

        return base.ApplySort(source);
    }

    protected override Task<bool> SaveAsync(bool isNew, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> ArchiveAsync(IReadOnlyList<AuditLogRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> DeleteAsync(IReadOnlyList<AuditLogRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override void OnRecordPicked(AuditLogRow row)
        => RecordPicked?.Invoke(this, row);

    protected override string GetNewRecordTitle() => string.Empty;
    protected override string GetEditRecordTitle() => string.Empty;
}
