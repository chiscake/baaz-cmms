using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.TmsIssuance;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed partial class ToolRequisitionHistoryViewModel : PageViewModelBase
{
    public const int BrowseLimit = 200;

    private static readonly string?[] StatusFilterValues =
    [
        null,
        TmsRequisitionStatuses.New,
        TmsRequisitionStatuses.PartiallyReserved,
        TmsRequisitionStatuses.ReadyForIssue,
        TmsRequisitionStatuses.Issued,
        TmsRequisitionStatuses.PartiallyReturned,
        TmsRequisitionStatuses.Returned,
        TmsRequisitionStatuses.Cancelled,
    ];

    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IRealtimeNotificationService _realtimeService;

    private List<TmsToolRequisitionLinkModel> _browseSource = [];
    private Guid? _selectedLinkId;
    private string? _pendingStatusFilter;
    private Guid? _pendingLinkId;
    private string? _detailLastKnownStatus;
    private bool _realtimeSubscribed;

    public ToolRequisitionHistoryViewModel(
        ITmsToolRequisitionService tmsToolRequisitionService,
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        IRealtimeNotificationService realtimeService,
        ToolRequisitionHistoryTableViewModel tableViewModel)
    {
        _tmsToolRequisitionService = tmsToolRequisitionService;
        _requestService = requestService;
        _maintenanceService = maintenanceService;
        _realtimeService = realtimeService;
        TableViewModel = tableViewModel;
        TableViewModel.RecordPicked += OnTableRecordPicked;
    }

    public ToolRequisitionHistoryTableViewModel TableViewModel { get; }

    public override string PageTitle => ResourceStrings.Get("Nav_ToolRequisitionHistory");

    public string OpenInTableLabel => ResourceStrings.Get("MyRequests_OpenInTable");
    public string BackToListLabel => ResourceStrings.Get("MyRequests_BackToList");
    public string SearchPlaceholder => ResourceStrings.Get("ToolRequisitionHistory_Search_Placeholder");
    public string FilterStatusLabel => ResourceStrings.Get("MyRequests_Filter_Status");
    public string EmptyListText => ResourceStrings.Get("ToolRequisitionHistory_Empty_List");
    public string EmptySelectionText => ResourceStrings.Get("MyRequests_Empty_Selection");
    public string EmptyFilterText => ResourceStrings.Get("MyRequests_Empty_Filter");

    public string DetailLabelNumber => ResourceStrings.Get("ToolRequisitionHistory_Detail_Number");
    public string DetailLabelWarehouse => ResourceStrings.Get("ToolRequisitionHistory_Detail_Warehouse");
    public string DetailLabelWorkOrderKind => ResourceStrings.Get("ToolRequisition_Label_WorkOrderKind");
    public string DetailLabelWorkOrder => ResourceStrings.Get("ToolRequisition_Label_WorkOrder");
    public string DetailLabelNotes => ResourceStrings.Get("ToolRequisition_Section_Notes");
    public string DetailLabelCreatedAt => ResourceStrings.Get("MyRequests_Detail_CreatedAt");
    public string DetailLabelUpdatedAt => ResourceStrings.Get("MyRequests_Detail_UpdatedAt");
    public string DetailLabelLastSyncedAt => ResourceStrings.Get("ToolRequisitionHistory_Detail_LastSyncedAt");
    public string CancelDetailLabel => ResourceStrings.Get("ToolRequisitionHistory_Action_Cancel");

    public bool CanCancelDetail =>
        _selectedLinkId is not null
        && TmsRequisitionStatuses.IsCancellable(_detailLastKnownStatus);

    public IReadOnlyList<string> StatusFilterLabels { get; } =
    [
        ResourceStrings.Get("MyRequests_Filter_Status_All"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_New"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_PartiallyReserved"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_ReadyForIssue"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_Issued"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_PartiallyReturned"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_Returned"),
        ResourceStrings.Get("ToolRequisition_TmsStatus_Cancelled"),
    ];

    public ObservableCollection<ToolRequisitionBrowseItem> BrowseItems { get; } = [];

    [ObservableProperty]
    public partial bool IsTableView { get; set; }

    public bool IsBrowseView => !IsTableView;

    partial void OnIsTableViewChanged(bool value) => OnPropertyChanged(nameof(IsBrowseView));

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedStatusIndex { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingBrowse { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingDetail { get; set; }

    [ObservableProperty]
    public partial bool HasBrowseItems { get; set; }

    [ObservableProperty]
    public partial bool HasFilteredItems { get; set; }

    [ObservableProperty]
    public partial bool HasDetail { get; set; }

    [ObservableProperty]
    public partial string DetailTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailStatusLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailStatusBackgroundKey { get; set; } = StatusBadgeFactory.DefaultBackgroundKey;

    [ObservableProperty]
    public partial string DetailStatusForegroundKey { get; set; } = StatusBadgeFactory.DefaultForegroundKey;

    [ObservableProperty]
    public partial string DetailWarehouse { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailWorkOrderKind { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailWorkOrder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasDetailNotes { get; set; }

    [ObservableProperty]
    public partial string DetailCreatedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailUpdatedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailLastSyncedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLastSyncedAt { get; set; }

    public bool ShowEmptyList => !IsLoadingBrowse && !HasBrowseItems;
    public bool ShowEmptyFilter => !IsLoadingBrowse && HasBrowseItems && !HasFilteredItems;
    public bool ShowEmptySelection => !IsLoadingDetail && HasFilteredItems && !HasDetail;

    public async Task OnPageLoadedAsync(object? parameter = null)
    {
        if (parameter is ToolRequisitionHistoryNavigationArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.StatusFilter))
                _pendingStatusFilter = args.StatusFilter;
            if (args.LinkId is Guid linkId)
                _pendingLinkId = linkId;
        }

        ApplyPendingStatusFilter();
        await LoadBrowseAsync(_pendingLinkId);
        _pendingLinkId = null;
    }

    partial void OnSearchTextChanged(string value) => ApplyBrowseFilter();

    partial void OnSelectedStatusIndexChanged(int value) => ApplyBrowseFilter();

    [RelayCommand]
    private async Task OpenTableAsync()
    {
        IsTableView = true;
        TableViewModel.SetSort("~UpdatedAt");
        await TableViewModel.OnPageLoadedAsync();
    }

    [RelayCommand]
    private void BackToList() => IsTableView = false;

    [RelayCommand]
    private async Task CancelDetailAsync()
    {
        if (_selectedLinkId is not Guid linkId || !CanCancelDetail)
            return;

        var confirmed = await AppDialogHelper.ConfirmAsync(
            ResourceStrings.Get("ToolRequisitionHistory_Cancel_Confirm_Title"),
            ResourceStrings.Get("ToolRequisitionHistory_Cancel_Confirm_Message"));
        if (!confirmed)
            return;

        try
        {
            var result = await _tmsToolRequisitionService.CancelRequisitionAsync(linkId);
            if (!result.IsSuccess || result.Value is null)
            {
                var key = result.Error?.MessageKey ?? "ToolRequisitionHistory_Cancel_Failed";
                var message = ResourceStrings.Get(key);
                if (message == key)
                    message = ResourceStrings.Get("ToolRequisitionHistory_Cancel_Failed");

                InfoBanner.Report(message, InfoBarSeverity.Error);
                return;
            }

            InfoBanner.Report(
                ResourceStrings.Get("ToolRequisitionHistory_Cancel_Success"),
                InfoBarSeverity.Success);
            await LoadBrowseAsync(linkId);
        }
        catch
        {
            InfoBanner.Report(
                ResourceStrings.Get("ToolRequisitionHistory_Cancel_Failed"),
                InfoBarSeverity.Error);
        }
    }

    public void SubscribeRealtime()
    {
        if (_realtimeSubscribed)
            return;

        _realtimeService.EventReceived += OnRealtimeEvent;
        _realtimeSubscribed = true;
    }

    public void UnsubscribeRealtime()
    {
        if (!_realtimeSubscribed)
            return;

        _realtimeService.EventReceived -= OnRealtimeEvent;
        _realtimeSubscribed = false;
    }

    private void OnRealtimeEvent(object? sender, RealtimeEvent e)
    {
        if (e.Table is not "tms_tool_requisition_links")
            return;

        RealtimeUiRefresh.EnqueueDebounced("tool-requisition-history", () => LoadBrowseAsync(_selectedLinkId));
    }

    [RelayCommand]
    private async Task SelectItemAsync(ToolRequisitionBrowseItem? item)
    {
        if (item is null)
            return;

        await SelectLinkAsync(item.Id);
    }

    private void OnTableRecordPicked(object? sender, Guid linkId)
    {
        IsTableView = false;
        _ = LoadBrowseAsync(linkId);
    }

    private void ApplyPendingStatusFilter()
    {
        if (string.IsNullOrWhiteSpace(_pendingStatusFilter))
            return;

        var index = Array.IndexOf(StatusFilterValues, _pendingStatusFilter);
        if (index >= 0)
            SelectedStatusIndex = index;

        _pendingStatusFilter = null;
    }

    private async Task LoadBrowseAsync(Guid? selectId = null)
    {
        IsLoadingBrowse = true;
        InfoBanner.Report(string.Empty);

        try
        {
            await _tmsToolRequisitionService.RefreshAllLocalAsync(BrowseLimit);
            var result = await _tmsToolRequisitionService.ListAllLocalAsync(BrowseLimit);
            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
                _browseSource = [];
                HasBrowseItems = false;
                ApplyBrowseFilter(selectId);
                return;
            }

            _browseSource = result.Value.ToList();
            HasBrowseItems = _browseSource.Count > 0;

            if (selectId is null && _selectedLinkId is not null)
                selectId = _selectedLinkId;

            ApplyBrowseFilter(selectId);
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoadingBrowse = false;
            NotifyEmptyStates();
        }
    }

    private void ApplyBrowseFilter(Guid? selectId = null)
    {
        var statusFilter = SelectedStatusIndex >= 0 && SelectedStatusIndex < StatusFilterValues.Length
            ? StatusFilterValues[SelectedStatusIndex]
            : null;

        var query = _browseSource.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            query = query.Where(link =>
            {
                var number = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId);
                var warehouse = link.WarehouseName ?? string.Empty;
                return number.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                       || warehouse.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                       || TmsRequisitionStatusHelper.GetLabel(link.LastKnownStatus)
                           .Contains(q, StringComparison.CurrentCultureIgnoreCase);
            });
        }

        if (statusFilter is not null)
            query = query.Where(link => string.Equals(link.LastKnownStatus, statusFilter, StringComparison.Ordinal));

        var filtered = query
            .OrderByDescending(link => link.UpdatedAt ?? link.CreatedAt ?? DateTimeOffset.MinValue)
            .ToList();

        HasFilteredItems = filtered.Count > 0;
        RebuildBrowseItems(filtered, selectId);
        NotifyEmptyStates();

        if (selectId is not null)
        {
            _ = SelectLinkAsync(selectId.Value);
            return;
        }

        if (filtered.Count > 0 && _selectedLinkId is null)
            _ = SelectLinkAsync(filtered[0].Id);
        else if (filtered.Count == 0)
            ClearDetail();
    }

    private void RebuildBrowseItems(IReadOnlyList<TmsToolRequisitionLinkModel> filtered, Guid? pinId)
    {
        var items = new List<ToolRequisitionBrowseItem>();
        TmsToolRequisitionLinkModel? pinSource = null;

        if (pinId is Guid id)
        {
            pinSource = filtered.FirstOrDefault(link => link.Id == id)
                ?? _browseSource.FirstOrDefault(link => link.Id == id);
        }

        if (pinSource is not null)
        {
            items.Add(ToolRequisitionBrowseItem.FromLink(pinSource, isPinned: true));
            foreach (var row in filtered.Where(link => link.Id != pinSource.Id))
                items.Add(ToolRequisitionBrowseItem.FromLink(row));
        }
        else
        {
            foreach (var row in filtered)
                items.Add(ToolRequisitionBrowseItem.FromLink(row));
        }

        BrowseItems.Clear();
        foreach (var item in items)
            BrowseItems.Add(item);
    }

    private async Task SelectLinkAsync(Guid linkId)
    {
        _selectedLinkId = linkId;

        foreach (var item in BrowseItems)
            item.IsSelected = item.Id == linkId;

        if (BrowseItems.All(i => i.Id != linkId))
        {
            await EnsurePinnedItemAsync(linkId);
            foreach (var item in BrowseItems)
                item.IsSelected = item.Id == linkId;
        }

        await LoadDetailAsync(linkId);
    }

    private async Task EnsurePinnedItemAsync(Guid linkId)
    {
        if (BrowseItems.Any(i => i.Id == linkId))
            return;

        var result = await _tmsToolRequisitionService.GetLocalByIdAsync(linkId);
        if (!result.IsSuccess || result.Value is null)
            return;

        BrowseItems.Insert(0, ToolRequisitionBrowseItem.FromLink(result.Value, isPinned: true));
    }

    private async Task LoadDetailAsync(Guid linkId)
    {
        IsLoadingDetail = true;
        try
        {
            var link = _browseSource.FirstOrDefault(l => l.Id == linkId);
            if (link is null)
            {
                var result = await _tmsToolRequisitionService.GetLocalByIdAsync(linkId);
                if (!result.IsSuccess || result.Value is null)
                {
                    ClearDetail();
                    return;
                }

                link = result.Value;
            }

            DetailTitle = string.IsNullOrWhiteSpace(link.WarehouseName)
                ? TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId)
                : link.WarehouseName;
            DetailNumber = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId);
            DetailStatusLabel = TmsRequisitionStatusHelper.GetLabel(link.LastKnownStatus);
            var statusBadge = StatusBadgeFactory.ForTmsRequisition(link.LastKnownStatus);
            DetailStatusBackgroundKey = statusBadge.BackgroundKey;
            DetailStatusForegroundKey = statusBadge.ForegroundKey;
            DetailWarehouse = string.IsNullOrWhiteSpace(link.WarehouseName)
                ? ResourceStrings.Get("ToolRequisitionHistory_UnknownWarehouse")
                : link.WarehouseName;
            DetailWorkOrderKind = FormatWorkOrderKind(link.WorkOrderKind);
            DetailWorkOrder = await ResolveWorkOrderTextAsync(link);
            DetailNotes = link.Notes ?? string.Empty;
            HasDetailNotes = !string.IsNullOrWhiteSpace(link.Notes);
            DetailCreatedAt = DateTimeDisplayHelper.Format(link.CreatedAt);
            DetailUpdatedAt = DateTimeDisplayHelper.Format(link.UpdatedAt);
            DetailLastSyncedAt = DateTimeDisplayHelper.Format(link.LastSyncedAt);
            HasLastSyncedAt = link.LastSyncedAt is not null;
            _detailLastKnownStatus = link.LastKnownStatus;
            HasDetail = true;
            OnPropertyChanged(nameof(CanCancelDetail));
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
            ClearDetail();
        }
        finally
        {
            IsLoadingDetail = false;
            NotifyEmptyStates();
        }
    }

    private async Task<string> ResolveWorkOrderTextAsync(TmsToolRequisitionLinkModel link)
    {
        if (link.CmmsRequestId is Guid requestId)
        {
            var detail = await _requestService.GetRequestByIdAsync(requestId);
            if (detail is not null)
                return $"{detail.RequestNumber} — {detail.Title}";
        }

        if (link.CmmsScheduleId is Guid scheduleId)
        {
            var schedule = (await _maintenanceService.GetScheduleAsync())
                .FirstOrDefault(s => s.Id == scheduleId);
            if (schedule is not null)
                return $"{schedule.AssetNumber} — {schedule.MaintenanceType} ({schedule.PlannedDate:dd.MM.yyyy})";
        }

        return ResourceStrings.Get("Common_None");
    }

    private static string FormatWorkOrderKind(string kind)
        => kind switch
        {
            "request" => ResourceStrings.Get("ToolRequisition_Kind_Request"),
            "schedule" => ResourceStrings.Get("ToolRequisition_Kind_Schedule"),
            _ => kind,
        };

    private void ClearDetail()
    {
        HasDetail = false;
        DetailTitle = string.Empty;
        HasDetailNotes = false;
        HasLastSyncedAt = false;
        _detailLastKnownStatus = null;
        OnPropertyChanged(nameof(CanCancelDetail));
        NotifyEmptyStates();
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(ShowEmptyList));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(ShowEmptySelection));
    }
}
