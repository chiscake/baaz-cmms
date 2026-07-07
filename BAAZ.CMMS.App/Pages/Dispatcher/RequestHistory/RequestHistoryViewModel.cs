using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.App.Services;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.DocumentExport;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestHistory;

public sealed partial class RequestHistoryViewModel : PageViewModelBase
{
    public const int BrowseLimit = 200;

    private static readonly string?[] StatusFilterValues =
    [
        null,
        "completed",
        "closed",
        "rejected",
        "cancelled",
    ];

    private static readonly string[] SourceStatuses =
    [
        "completed",
        "closed",
        "rejected",
        "cancelled",
    ];

    private readonly IRequestService _requestService;
    private readonly IDocumentSaveLocationService _saveLocationService;
    private readonly IWindowsShellFileService _shellFileService;
    private readonly IRepairRequestExportService _repairRequestExportService;
    private readonly IRequestCardExportService _requestCardExportService;

    private List<RequestListItem> _browseSource = [];
    private Guid? _selectedRequestId;

    public RequestHistoryViewModel(
        IRequestService requestService,
        RequestHistoryTableViewModel tableViewModel,
        IDocumentSaveLocationService saveLocationService,
        IWindowsShellFileService shellFileService,
        IRepairRequestExportService repairRequestExportService,
        IRequestCardExportService requestCardExportService)
    {
        _requestService = requestService;
        _saveLocationService = saveLocationService;
        _shellFileService = shellFileService;
        _repairRequestExportService = repairRequestExportService;
        _requestCardExportService = requestCardExportService;
        TableViewModel = tableViewModel;
        TableViewModel.RecordPicked += OnTableRecordPicked;
    }

    public RequestHistoryTableViewModel TableViewModel { get; }

    public override string PageTitle => ResourceStrings.Get("Nav_RequestHistory");

    public string OpenInTableLabel => ResourceStrings.Get("MyRequests_OpenInTable");
    public string BackToListLabel => ResourceStrings.Get("MyRequests_BackToList");
    public string SearchPlaceholder => ResourceStrings.Get("MyRequests_Search_Placeholder");
    public string FilterStatusLabel => ResourceStrings.Get("MyRequests_Filter_Status");
    public string EmptyListText => ResourceStrings.Get("RequestHistory_Empty_List");
    public string EmptySelectionText => ResourceStrings.Get("MyRequests_Empty_Selection");
    public string EmptyFilterText => ResourceStrings.Get("MyRequests_Empty_Filter");
    public string HistoryTitle => ResourceStrings.Get("MyRequests_History_Title");
    public string HistoryEmptyText => ResourceStrings.Get("MyRequests_History_Empty");
    public string ActionExportRepairRequest => ResourceStrings.Get("RequestDetail_Export_RepairRequest");
    public string ActionExportRequestCard => ResourceStrings.Get("RequestDetail_Export_RequestCard");

    public string DetailLabelNumber => ResourceStrings.Get("MyRequests_Detail_Number");
    public string DetailLabelType => ResourceStrings.Get("MyRequests_Detail_Type");
    public string DetailLabelPriority => ResourceStrings.Get("MyRequests_Detail_Priority");
    public string DetailLabelRepairZone => ResourceStrings.Get("MyRequests_Detail_RepairZone");
    public string DetailLabelAsset => ResourceStrings.Get("MyRequests_Detail_Asset");
    public string DetailLabelLocation => ResourceStrings.Get("MyRequests_Detail_Location");
    public string DetailLabelDescription => ResourceStrings.Get("MyRequests_Detail_Description");
    public string DetailLabelDepartments => ResourceStrings.Get("MyRequests_Detail_Departments");
    public string DetailLabelContractorName => ResourceStrings.Get("MyRequests_Detail_ContractorName");
    public string DetailLabelCreatedAt => ResourceStrings.Get("MyRequests_Detail_CreatedAt");
    public string DetailLabelUpdatedAt => ResourceStrings.Get("MyRequests_Detail_UpdatedAt");

    public IReadOnlyList<string> StatusFilterLabels { get; } =
    [
        ResourceStrings.Get("MyRequests_Filter_Status_All"),
        ResourceStrings.Get("RequestStatus_Completed"),
        ResourceStrings.Get("RequestStatus_Closed"),
        ResourceStrings.Get("RequestStatus_Rejected"),
        ResourceStrings.Get("RequestStatus_Cancelled"),
    ];

    public ObservableCollection<RequestBrowseItem> BrowseItems { get; } = [];
    public ObservableCollection<RequestHistoryDisplayItem> HistoryItems { get; } = [];
    public ObservableCollection<RequestDepartmentDisplayItem> DetailDepartments { get; } = [];

    [ObservableProperty]
    public partial bool IsTableView { get; set; }

    public bool IsBrowseView => !IsTableView;

    partial void OnIsTableViewChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBrowseView));
    }

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
    public partial string DetailType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailPriority { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailRepairZone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailAsset { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailLocation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailContractorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasContractorName { get; set; }

    [ObservableProperty]
    public partial bool HasDetailDepartments { get; set; }

    [ObservableProperty]
    public partial string DetailCreatedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailUpdatedAt { get; set; } = string.Empty;

    public bool ShowEmptyList => !IsLoadingBrowse && !HasBrowseItems;
    public bool ShowEmptyFilter => !IsLoadingBrowse && HasBrowseItems && !HasFilteredItems;
    public bool ShowEmptySelection => !IsLoadingDetail && HasFilteredItems && !HasDetail;
    public bool HasHistoryItems => HistoryItems.Count > 0;
    public bool ShowHistoryEmpty => HasDetail && !HasHistoryItems;

    public async Task OnPageLoadedAsync()
    {
        await LoadBrowseAsync();
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
    private async Task SelectItemAsync(RequestBrowseItem? item)
    {
        if (item is null)
            return;

        await SelectRequestAsync(item.Id);
    }

    private void OnTableRecordPicked(object? sender, Guid requestId)
    {
        IsTableView = false;
        _ = LoadBrowseAsync(requestId);
    }

    private async Task LoadBrowseAsync(Guid? selectId = null)
    {
        IsLoadingBrowse = true;
        InfoBanner.Report(string.Empty);

        try
        {
            _browseSource = (await _requestService.GetRequestsByStatusesAsync(SourceStatuses, default))
                .Take(BrowseLimit)
                .ToList();
            HasBrowseItems = _browseSource.Count > 0;

            if (selectId is null && _selectedRequestId is not null)
                selectId = _selectedRequestId;

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
            query = query.Where(r =>
                r.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                || r.RequestNumber.Contains(q, StringComparison.CurrentCultureIgnoreCase));
        }

        if (statusFilter is not null)
            query = query.Where(r => string.Equals(r.Status, statusFilter, StringComparison.Ordinal));

        var filtered = query.OrderByDescending(r => r.UpdatedAt).ToList();
        HasFilteredItems = filtered.Count > 0;
        RebuildBrowseItems(filtered, selectId);
        NotifyEmptyStates();

        if (selectId is not null)
        {
            _ = SelectRequestAsync(selectId.Value);
            return;
        }

        if (filtered.Count > 0 && _selectedRequestId is null)
            _ = SelectRequestAsync(filtered[0].Id);
        else if (filtered.Count == 0)
            ClearDetail();
    }

    private void RebuildBrowseItems(IReadOnlyList<RequestListItem> filtered, Guid? pinId)
    {
        var items = new List<RequestBrowseItem>();
        RequestListItem? pinSource = null;

        if (pinId is Guid id)
        {
            pinSource = filtered.FirstOrDefault(r => r.Id == id)
                ?? _browseSource.FirstOrDefault(r => r.Id == id);
        }

        if (pinSource is not null)
        {
            items.Add(RequestBrowseItem.FromListItem(pinSource, isPinned: true));
            foreach (var row in filtered.Where(r => r.Id != pinSource.Id))
                items.Add(RequestBrowseItem.FromListItem(row));
        }
        else
        {
            foreach (var row in filtered)
                items.Add(RequestBrowseItem.FromListItem(row));
        }

        BrowseItems.Clear();
        foreach (var item in items)
            BrowseItems.Add(item);
    }

    private async Task SelectRequestAsync(Guid requestId)
    {
        _selectedRequestId = requestId;

        foreach (var item in BrowseItems)
            item.IsSelected = item.Id == requestId;

        if (BrowseItems.All(i => i.Id != requestId))
        {
            await EnsurePinnedItemAsync(requestId);
            foreach (var item in BrowseItems)
                item.IsSelected = item.Id == requestId;
        }

        await LoadDetailAsync(requestId);
    }

    private async Task EnsurePinnedItemAsync(Guid requestId)
    {
        if (BrowseItems.Any(i => i.Id == requestId))
            return;

        var detail = await _requestService.GetRequestByIdAsync(requestId);
        if (detail is null)
            return;

        var listItem = new RequestListItem
        {
            Id = detail.Id,
            RequestNumber = detail.RequestNumber,
            Title = detail.Title,
            Status = detail.Status,
            Priority = detail.Priority,
            Type = detail.Type,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            AssigneeName = detail.AssigneeName,
        };

        BrowseItems.Insert(0, RequestBrowseItem.FromListItem(listItem, isPinned: true));
    }

    private async Task LoadDetailAsync(Guid requestId)
    {
        IsLoadingDetail = true;
        try
        {
            var detail = await _requestService.GetRequestByIdAsync(requestId);
            if (detail is null)
            {
                ClearDetail();
                return;
            }

            DetailTitle = detail.Title;
            DetailNumber = detail.RequestNumber;
            DetailStatusLabel = RequestStatusHelper.GetLabel(detail.Status);
            var statusBadge = StatusBadgeFactory.ForRequest(detail.Status);
            DetailStatusBackgroundKey = statusBadge.BackgroundKey;
            DetailStatusForegroundKey = statusBadge.ForegroundKey;
            DetailType = RequestEnumLabels.Type(detail.Type);
            DetailPriority = RequestEnumLabels.Priority(detail.Priority);
            DetailRepairZone = RequestEnumLabels.RepairZone(detail.RepairZone);
            DetailAsset = RequestDetailDisplayHelper.FormatAsset(detail);
            DetailLocation = detail.LocationDescription;
            DetailDescription = detail.Description;
            DetailContractorName = detail.ContractorName ?? string.Empty;
            HasContractorName = string.Equals(detail.RepairZone, "external", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(detail.ContractorName);

            RequestDepartmentDisplayHelper.ReplaceCollection(DetailDepartments, detail);
            HasDetailDepartments = DetailDepartments.Count > 0;
            DetailCreatedAt = DateTimeDisplayHelper.Format(detail.CreatedAt);
            DetailUpdatedAt = DateTimeDisplayHelper.Format(detail.UpdatedAt);
            HasDetail = true;

            await LoadHistoryAsync(requestId);
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

    private async Task LoadHistoryAsync(Guid requestId)
    {
        HistoryItems.Clear();
        var history = await _requestService.GetStatusHistoryAsync(requestId);
        foreach (var item in history)
            HistoryItems.Add(RequestHistoryDisplayItem.From(item));
        OnPropertyChanged(nameof(HasHistoryItems));
        OnPropertyChanged(nameof(ShowHistoryEmpty));
    }

    private void ClearDetail()
    {
        HasDetail = false;
        DetailTitle = string.Empty;
        DetailDepartments.Clear();
        HasDetailDepartments = false;
        HasContractorName = false;
        HistoryItems.Clear();
        OnPropertyChanged(nameof(HasHistoryItems));
        OnPropertyChanged(nameof(ShowHistoryEmpty));
        NotifyEmptyStates();
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(ShowEmptyList));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(ShowEmptySelection));
    }

    [RelayCommand]
    private async Task ExportRepairRequestAsync()
    {
        if (_selectedRequestId is not Guid requestId)
            return;

        await DocumentExportHelper.RunDocxExportAsync(
            this,
            _saveLocationService,
            _shellFileService,
            $"Заявка-ремонт_{SanitizeFileName(DetailNumber)}.docx",
            "DocumentExport_RepairRequest_Success_Title",
            "DocumentExport_Success_Message",
            path => _repairRequestExportService.ExportAsync(requestId, path));
    }

    [RelayCommand]
    private async Task ExportRequestCardAsync()
    {
        if (_selectedRequestId is not Guid requestId)
            return;

        await DocumentExportHelper.RunDocxExportAsync(
            this,
            _saveLocationService,
            _shellFileService,
            $"Карточка-заявки_{SanitizeFileName(DetailNumber)}.docx",
            "DocumentExport_RequestCard_Success_Title",
            "DocumentExport_Success_Message",
            path => _requestCardExportService.ExportAsync(requestId, path));
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }
}
