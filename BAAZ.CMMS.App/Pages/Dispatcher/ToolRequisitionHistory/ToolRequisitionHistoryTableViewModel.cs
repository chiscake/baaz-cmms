using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.TmsIssuance;

using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed class ToolRequisitionHistoryTableViewModel : CrudWorkbenchViewModelBase<ToolRequisitionHistoryRow>
{
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;

    public event EventHandler<Guid>? RecordPicked;

    public ToolRequisitionHistoryTableViewModel(
        ITmsToolRequisitionService tmsToolRequisitionService,
        IRequestService requestService,
        IMaintenanceService maintenanceService)
    {
        _tmsToolRequisitionService = tmsToolRequisitionService;
        _requestService = requestService;
        _maintenanceService = maintenanceService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_ToolRequisitionHistory");

    protected override string ColumnSettingsKey => "ToolRequisitionHistoryTable";

    protected override string ToolbarResourcePrefix => "ToolRequisitionHistory";

    protected override CrudRowOpenMode RowOpenMode => CrudRowOpenMode.Pick;

    public override string ToolbarAdd => string.Empty;
    public override string ToolbarRefresh => ResourceStrings.Get("MyRequests_Table_Refresh");
    public override string ToolbarColumns => ResourceStrings.Get("MyRequests_Table_Columns");
    public override string FilterPlaceholder => ResourceStrings.Get("ToolRequisitionHistory_Search_Placeholder");
    public override string ShowInactiveLabel => string.Empty;

    public string ColumnNumber => ResourceStrings.Get("ToolRequisitionHistory_Column_Number");
    public string ColumnWarehouse => ResourceStrings.Get("ToolRequisitionHistory_Column_Warehouse");
    public string ColumnStatus => ResourceStrings.Get("MyRequests_Column_Status");
    public string ColumnWorkOrderKind => ResourceStrings.Get("ToolRequisition_Label_WorkOrderKind");
    public string ColumnWorkOrder => ResourceStrings.Get("ToolRequisition_Label_WorkOrder");
    public string ColumnCreatedAt => ResourceStrings.Get("MyRequests_Column_CreatedAt");
    public string ColumnUpdatedAt => ResourceStrings.Get("MyRequests_Detail_UpdatedAt");

    protected override void InitColumns()
    {
        Columns.Clear();
        Columns.Add(new CrudColumnDefinition
        {
            Key = "RequisitionNumber",
            Header = ColumnNumber,
            DataTypeLabel = "text",
            DesiredWidth = 170,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "WarehouseName",
            Header = ColumnWarehouse,
            DataTypeLabel = "text",
            DesiredWidth = 160,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "Status",
            Header = ColumnStatus,
            DataTypeLabel = "enum",
            DesiredWidth = 150,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "WorkOrderKind",
            Header = ColumnWorkOrderKind,
            DataTypeLabel = "enum",
            DesiredWidth = 120,
            IsSortable = true,
            IsFilterable = true,
            FilterKind = CrudColumnFilterKind.Text,
        });
        Columns.Add(new CrudColumnDefinition
        {
            Key = "WorkOrderRef",
            Header = ColumnWorkOrder,
            DataTypeLabel = "text",
            DesiredWidth = 220,
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
            Key = "UpdatedAt",
            Header = ColumnUpdatedAt,
            DataTypeLabel = "timestamptz",
            DesiredWidth = 150,
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
        await _tmsToolRequisitionService.RefreshAllLocalAsync(ToolRequisitionHistoryViewModel.BrowseLimit, ct);
        var result = await _tmsToolRequisitionService.ListAllLocalAsync(ToolRequisitionHistoryViewModel.BrowseLimit, ct);
        _allRows.Clear();
        if (!result.IsSuccess || result.Value is null)
            return;

        var scheduleCache = (await _maintenanceService.GetScheduleAsync(ct)).ToDictionary(s => s.Id);
        foreach (var link in result.Value)
        {
            var workOrderRef = await ResolveWorkOrderRefAsync(link, scheduleCache, ct);
            _allRows.Add(ToolRequisitionHistoryRow.FromLink(link, workOrderRef));
        }
    }

    protected override Task<bool> SaveAsync(bool isNew, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> ArchiveAsync(IReadOnlyList<ToolRequisitionHistoryRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override Task<bool> DeleteAsync(IReadOnlyList<ToolRequisitionHistoryRow> rows, CancellationToken ct)
        => Task.FromResult(false);

    protected override IEnumerable<ToolRequisitionHistoryRow> ApplySort(IEnumerable<ToolRequisitionHistoryRow> source)
    {
        source = ApplyDateTimeColumnSort(source, "UpdatedAt", r => r.UpdatedAt);
        if (string.Equals(SortColumnKey, "UpdatedAt", StringComparison.Ordinal))
            return source;

        source = ApplyDateTimeColumnSort(source, "CreatedAt", r => r.CreatedAt);
        if (string.Equals(SortColumnKey, "CreatedAt", StringComparison.Ordinal))
            return source;

        return base.ApplySort(source);
    }

    protected override void OnRecordPicked(ToolRequisitionHistoryRow row)
        => RecordPicked?.Invoke(this, row.Id);

    protected override string GetNewRecordTitle() => string.Empty;
    protected override string GetEditRecordTitle() => string.Empty;

    private async Task<string> ResolveWorkOrderRefAsync(
        Core.Data.Models.TmsToolRequisitionLinkModel link,
        IReadOnlyDictionary<Guid, MaintenanceScheduleItem> scheduleCache,
        CancellationToken ct)
    {
        if (link.CmmsRequestId is Guid requestId)
        {
            var detail = await _requestService.GetRequestByIdAsync(requestId, ct);
            if (detail is not null)
                return $"{detail.RequestNumber} — {detail.Title}";
        }

        if (link.CmmsScheduleId is Guid scheduleId
            && scheduleCache.TryGetValue(scheduleId, out var schedule))
        {
            return $"{schedule.AssetNumber} — {schedule.MaintenanceType} ({schedule.PlannedDate:dd.MM.yyyy})";
        }

        return ResourceStrings.Get("Common_None");
    }
}
