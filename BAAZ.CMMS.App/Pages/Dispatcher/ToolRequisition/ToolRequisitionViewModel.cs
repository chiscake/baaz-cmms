using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Services;
using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.ToolTracker;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using BAAZ.CMMS.Core.Services.Requisitions;
using BAAZ.CMMS.Core.Services.TmsIssuance;
using BAAZ.CMMS.Core.Services.ToolRequisition;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

using Helpers.Settings;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed partial class ToolRequisitionViewModel : PageViewModelBase
{
    private readonly IToolRequisitionService _toolRequisitionService;
    private readonly ITmsIssuanceClient _tmsIssuanceClient;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly ITechnicianCatalogService _technicianCatalogService;
    private readonly IAuthService _authService;
    private readonly IDocumentSaveLocationService _saveLocationService;
    private readonly IWindowsShellFileService _shellFileService;
    private readonly IRealtimeNotificationService _realtimeService;
    private bool _realtimeSubscribed;

    private Guid? _lockedRequestId;
    private Guid? _lockedScheduleId;
    private IReadOnlyList<TechnicianListItem> _technicians = [];
    private readonly HashSet<Guid> _blockingWarehouseIds = [];

    public ToolRequisitionViewModel(
        IToolRequisitionService toolRequisitionService,
        ITmsIssuanceClient tmsIssuanceClient,
        ITmsToolRequisitionService tmsToolRequisitionService,
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        ITechnicianCatalogService technicianCatalogService,
        IAuthService authService,
        IDocumentSaveLocationService saveLocationService,
        IWindowsShellFileService shellFileService,
        IRealtimeNotificationService realtimeService)
    {
        _toolRequisitionService = toolRequisitionService;
        _tmsIssuanceClient = tmsIssuanceClient;
        _tmsToolRequisitionService = tmsToolRequisitionService;
        _requestService = requestService;
        _maintenanceService = maintenanceService;
        _technicianCatalogService = technicianCatalogService;
        _authService = authService;
        _saveLocationService = saveLocationService;
        _shellFileService = shellFileService;
        _realtimeService = realtimeService;

        AttachLine(new ToolRequisitionLineRow());
        WarehouseName = ResourceStrings.Get("ToolRequisition_Default_WarehouseName");
    }

    public override string PageTitle => ResourceStrings.Get("Nav_ToolRequisition");

    public string SectionMode => ResourceStrings.Get("ToolRequisition_Section_Channel");
    public string ModeDocxLabel => ResourceStrings.Get("ToolRequisition_Mode_Docx");
    public string ModeTmsLabel => ResourceStrings.Get("ToolRequisition_Mode_Tms");
    public string SectionWorkOrder => ResourceStrings.Get("ToolRequisition_Section_WorkOrder");
    public string SectionWarehouse => ResourceStrings.Get("ToolRequisition_Section_Warehouse");
    public string SectionWarehouseTms => ResourceStrings.Get("ToolRequisition_Section_WarehouseTms");
    public string SectionLines => ResourceStrings.Get("ToolRequisition_Section_Lines");
    public string SectionLinesHintDocx => ResourceStrings.Get("ToolRequisition_Section_Lines_Hint_Docx");
    public string SectionLinesHintTms => ResourceStrings.Get("ToolRequisition_Section_Lines_Hint_Tms");
    public string SectionNotes => ResourceStrings.Get("ToolRequisition_Section_Notes");
    public string SectionTmsLinks => ResourceStrings.Get("ToolRequisition_Section_TmsLinks");
    public string TmsLinksEmpty => ResourceStrings.Get("ToolRequisition_TmsLinks_Empty");
    public string LabelWorkOrderKind => ResourceStrings.Get("ToolRequisition_Label_WorkOrderKind");
    public string LabelWorkOrder => ResourceStrings.Get("ToolRequisition_Label_WorkOrder");
    public string LabelTechnician => ResourceStrings.Get("ToolRequisition_Label_Technician");
    public string LabelWarehouseName => ResourceStrings.Get("ToolRequisition_Label_WarehouseName");
    public string LabelTmsWarehouse => ResourceStrings.Get("ToolRequisition_Label_TmsWarehouse");
    public string LineNameHeader => ResourceStrings.Get("ToolRequisition_Line_Name");
    public string LineInventoryHeader => ResourceStrings.Get("ToolRequisition_Line_Inventory");
    public string LineQuantityHeader => ResourceStrings.Get("ToolRequisition_Line_Quantity");
    public string LineNoteHeader => ResourceStrings.Get("ToolRequisition_Line_LineNote");
    public string LineCatalogHeader => ResourceStrings.Get("ToolRequisition_Line_Catalog");
    public string LineNamePlaceholder => ResourceStrings.Get("ToolRequisition_Line_Name_Placeholder");
    public string LineInventoryPlaceholder => ResourceStrings.Get("ToolRequisition_Line_Inventory_Placeholder");
    public string LineCatalogPlaceholder => ResourceStrings.Get("ToolRequisition_Line_Catalog_Placeholder");
    public string LineNotePlaceholder => ResourceStrings.Get("ToolRequisition_Line_LineNote_Placeholder");
    public string LineItemTitlePrefix => ResourceStrings.Get("ToolRequisition_Line_ItemTitle");
    public string ActionAddLine => ResourceStrings.Get("ToolRequisition_Action_AddLine");
    public string ActionRemoveLine => ResourceStrings.Get("ToolRequisition_Action_RemoveLine");
    public string ActionSubmitDocx => ResourceStrings.Get("Common_Action_GenerateDocx");
    public string ActionSubmitTms => ResourceStrings.Get("ToolRequisition_Action_SubmitTms");
    public string DuplicateTmsHint => ResourceStrings.Get("ToolRequisition_DuplicateActiveLink_Hint");
    public string NotesPlaceholder => ResourceStrings.Get("ToolRequisition_Notes_Placeholder");
    public string WorkOrderKindPlaceholder => ResourceStrings.Get("Common_SelectWorkOrderKind");
    public string WorkOrderPlaceholder => ResourceStrings.Get("Common_SelectWorkOrder");
    public string TechnicianPlaceholder => ResourceStrings.Get("Common_SelectTechnician");
    public string TmsWarehousePlaceholder => ResourceStrings.Get("ToolRequisition_TmsWarehouse_Placeholder");

    public bool IsTmsLiveIntegration => TmsIntegrationSettingsSync.IsLive;

    public bool ShowTmsMockWarning => IsTmsMode && !IsTmsLiveIntegration;

    public string TmsMockWarningText => ResourceStrings.Get("ToolRequisition_TmsMock_Warning");

    public string TmsLiveModeHint => ResourceStrings.Get("ToolRequisition_TmsLive_Hint");

    public IReadOnlyList<string> WorkOrderKindLabels { get; } =
    [
        ResourceStrings.Get("ToolRequisition_Kind_Request"),
        ResourceStrings.Get("ToolRequisition_Kind_Schedule"),
    ];

    public ObservableCollection<ToolRequisitionWorkOrderOption> WorkOrderOptions { get; } = [];

    public ObservableCollection<TechnicianListItem> TechnicianOptions { get; } = [];

    public ObservableCollection<ToolRequisitionLineRow> Lines { get; } = [];

    public ObservableCollection<TmsWarehousePickerItem> TmsWarehouses { get; } = [];

    public ObservableCollection<TmsToolCatalogItem> CatalogTools { get; } = [];

    public ObservableCollection<TmsLinkDisplayItem> TmsLinks { get; } = [];

    [ObservableProperty]
    public partial int SelectedChannelIndex { get; set; } = (int)ToolRequisitionChannel.Tms;

    [ObservableProperty]
    public partial int SelectedWorkOrderKindIndex { get; set; } = -1;

    [ObservableProperty]
    public partial ToolRequisitionWorkOrderOption? SelectedWorkOrder { get; set; }

    [ObservableProperty]
    public partial TechnicianListItem? SelectedTechnician { get; set; }

    [ObservableProperty]
    public partial string WarehouseName { get; set; }

    [ObservableProperty]
    public partial TmsWarehousePickerItem? SelectedTmsWarehouse { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContextSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsWorkOrderLocked { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSubmitting { get; set; }

    public bool CanSubmit => !IsSubmitting && !IsLoading;

    public bool HasBlockingLinkForSelectedWarehouse =>
        SelectedTmsWarehouse is { } warehouse && _blockingWarehouseIds.Contains(warehouse.WarehouseId);

    public bool CanSubmitTms => CanSubmit && !HasBlockingLinkForSelectedWarehouse;

    public bool ShowDuplicateTmsHint => IsTmsMode && HasBlockingLinkForSelectedWarehouse;

    public bool IsDocxMode => SelectedChannelIndex == (int)ToolRequisitionChannel.Docx;

    public bool IsTmsMode => SelectedChannelIndex == (int)ToolRequisitionChannel.Tms;

    public string SectionLinesHint => IsDocxMode ? SectionLinesHintDocx : SectionLinesHintTms;

    public bool ShowWorkOrderPicker => !IsWorkOrderLocked;

    public bool ShowTmsCatalog => IsTmsMode && SelectedTmsWarehouse is not null;

    public bool HasTmsLinks => TmsLinks.Count > 0;

    partial void OnIsSubmittingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanSubmitTms));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanSubmitTms));
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDocxMode));
        OnPropertyChanged(nameof(IsTmsMode));
        OnPropertyChanged(nameof(ShowTmsCatalog));
        OnPropertyChanged(nameof(SectionLinesHint));
        OnPropertyChanged(nameof(ShowDuplicateTmsHint));
        OnPropertyChanged(nameof(CanSubmitTms));
        OnPropertyChanged(nameof(ShowTmsMockWarning));
    }

    partial void OnSelectedWorkOrderKindIndexChanged(int value)
    {
        if (!IsWorkOrderLocked)
            _ = LoadWorkOrderOptionsAsync();
    }

    partial void OnSelectedWorkOrderChanged(ToolRequisitionWorkOrderOption? value)
    {
        UpdateContextSummary();
        TryPrefillTechnician(value);
        _ = RefreshTmsLinksAsync();
    }

    partial void OnIsWorkOrderLockedChanged(bool value) =>
        OnPropertyChanged(nameof(ShowWorkOrderPicker));

    partial void OnSelectedTmsWarehouseChanged(TmsWarehousePickerItem? value)
    {
        if (value is not null)
            WarehouseName = value.Name;

        OnPropertyChanged(nameof(ShowTmsCatalog));
        OnPropertyChanged(nameof(HasBlockingLinkForSelectedWarehouse));
        OnPropertyChanged(nameof(ShowDuplicateTmsHint));
        OnPropertyChanged(nameof(CanSubmitTms));
        _ = LoadCatalogToolsAsync();
    }

    public async Task OnPageLoadedAsync(object? navigationParameter)
    {
        if (navigationParameter is ToolRequisitionNavigationArgs args)
        {
            _lockedRequestId = args.RequestId;
            _lockedScheduleId = args.ScheduleId;
            IsWorkOrderLocked = args.RequestId is not null || args.ScheduleId is not null;
            if (args.RequestId is not null)
                SelectedWorkOrderKindIndex = 0;
            if (args.ScheduleId is not null)
                SelectedWorkOrderKindIndex = 1;
            if (args.Channel is ToolRequisitionChannel channel)
                SelectedChannelIndex = (int)channel;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private void AddLine() => AttachLine(new ToolRequisitionLineRow());

    [RelayCommand]
    private void RemoveLine(ToolRequisitionLineRow? row)
    {
        if (row is null || Lines.Count <= 1)
            return;

        Lines.Remove(row);
        RefreshLineNumbers();
    }

    [RelayCommand]
    private async Task SubmitDocxAsync()
    {
        if (IsSubmitting || !TryBuildInput(includeWarehouseId: false, out var input))
            return;

        var suggestedName = BuildSuggestedFileName();
        var targetPath = await _saveLocationService.PickDocxSavePathAsync(suggestedName);
        if (targetPath is null)
            return;

        IsSubmitting = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var result = await _toolRequisitionService.SubmitDocxAsync(input, targetPath);
            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(ResolveError(result.Error), InfoBarSeverity.Error);
                return;
            }

            var open = await AppDialogHelper.ConfirmSuccessAsync(
                ResourceStrings.Get("ToolRequisition_SubmitDocx_Success_Title"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("ToolRequisition_SubmitDocx_Success_Message"),
                    result.Value.SavedFilePath),
                App.MainWindow);

            if (open)
                await _shellFileService.OpenFileAsync(result.Value.SavedFilePath);
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_SaveFailed"), InfoBarSeverity.Error);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    private async Task SubmitTmsAsync()
    {
        if (IsSubmitting || !TryBuildInput(includeWarehouseId: true, out var input))
            return;

        if (HasBlockingLinkForSelectedWarehouse)
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_DuplicateActiveLink"), InfoBarSeverity.Warning);
            return;
        }

        var profile = _authService.CurrentProfile;
        if (profile is null)
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_NotAuthenticated"), InfoBarSeverity.Error);
            return;
        }

        IsSubmitting = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var result = await _toolRequisitionService.SubmitToTmsAsync(input, profile.Id);
            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(ResolveError(result.Error), InfoBarSeverity.Error);
                return;
            }

            await AppDialogHelper.ConfirmSuccessAsync(
                ResourceStrings.Get("ToolRequisition_SubmitTms_Success_Title"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get(
                        IsTmsLiveIntegration
                            ? "ToolRequisition_SubmitTms_Success_Message"
                            : "ToolRequisition_SubmitTms_Success_Message_Mock"),
                    result.Value.RequisitionNumber),
                App.MainWindow);

            await RefreshTmsLinksAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_TmsFailed"), InfoBarSeverity.Error);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void AttachLine(ToolRequisitionLineRow row)
    {
        row.Owner = this;
        Lines.Add(row);
        RefreshLineNumbers();
    }

    private void RefreshLineNumbers()
    {
        for (var i = 0; i < Lines.Count; i++)
        {
            Lines[i].LineNumber = i + 1;
            Lines[i].NotifyRemoveVisibility();
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
            SyncTmsIntegrationSettings();
            OnPropertyChanged(nameof(IsTmsLiveIntegration));
            OnPropertyChanged(nameof(ShowTmsMockWarning));

            var techniciansResult = await _technicianCatalogService.GetTechniciansAsync();
            if (!techniciansResult.IsSuccess || techniciansResult.Value is null)
            {
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
                return;
            }

            _technicians = techniciansResult.Value.Where(t => t.IsActive).ToList();
            TechnicianOptions.Clear();
            foreach (var technician in _technicians.OrderBy(t => t.FullName, StringComparer.CurrentCultureIgnoreCase))
                TechnicianOptions.Add(technician);

            await LoadTmsWarehousesAsync();
            await LoadWorkOrderOptionsAsync();
            await RefreshTmsLinksAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTmsWarehousesAsync()
    {
        TmsWarehouses.Clear();
        SelectedTmsWarehouse = null;

        try
        {
            var result = await _tmsIssuanceClient.GetWarehousesAsync();
            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(
                    result.Error is not null
                        ? ResolveError(result.Error)
                        : ResourceStrings.Get("ToolRequisition_Error_TmsWarehousesFailed"),
                    InfoBarSeverity.Error);
                return;
            }

            foreach (var warehouse in result.Value.Warehouses)
            {
                TmsWarehouses.Add(new TmsWarehousePickerItem
                {
                    WarehouseId = warehouse.WarehouseId,
                    Name = warehouse.Name,
                });
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or JsonException)
        {
            InfoBanner.Report(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("ToolRequisition_Error_TmsFixturesFailed"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    private async Task LoadCatalogToolsAsync()
    {
        CatalogTools.Clear();
        if (SelectedTmsWarehouse is null)
            return;

        try
        {
            var result = await _tmsIssuanceClient.GetToolsAsync(
                SelectedTmsWarehouse.WarehouseId,
                TmsToolAvailability.Available);

            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(
                    result.Error is not null
                        ? ResolveError(result.Error)
                        : ResourceStrings.Get("ToolRequisition_Error_TmsCatalogFailed"),
                    InfoBarSeverity.Error);
                return;
            }

            foreach (var item in result.Value.Items)
                CatalogTools.Add(item);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or JsonException)
        {
            InfoBanner.Report(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("ToolRequisition_Error_TmsFixturesFailed"),
                    ex.Message),
                InfoBarSeverity.Error);
        }
    }

    private async Task LoadWorkOrderOptionsAsync()
    {
        WorkOrderOptions.Clear();
        SelectedWorkOrder = null;

        if (IsWorkOrderLocked)
        {
            if (_lockedRequestId is Guid requestId)
            {
                var detail = await _requestService.GetRequestByIdAsync(requestId);
                if (detail is not null && WorkOrderRequisitionPolicy.AllowsToolRequisition(detail.Status))
                {
                    var option = new ToolRequisitionWorkOrderOption
                    {
                        IsRequest = true,
                        Id = requestId,
                        DisplayText = $"{detail.RequestNumber} — {detail.Title}",
                        AssigneeName = detail.AssigneeName,
                    };
                    WorkOrderOptions.Add(option);
                    SelectedWorkOrder = option;
                }
            }
            else if (_lockedScheduleId is Guid scheduleId)
            {
                var item = (await _maintenanceService.GetScheduleAsync())
                    .FirstOrDefault(s => s.Id == scheduleId
                        && WorkOrderRequisitionPolicy.AllowsToolRequisitionSchedule(s.Status));
                if (item is not null)
                {
                    var option = new ToolRequisitionWorkOrderOption
                    {
                        IsRequest = false,
                        Id = scheduleId,
                        DisplayText = $"{item.AssetNumber} — {item.AssetName} ({MaintenanceTypeLabels.Get(item.MaintenanceType)})",
                    };
                    WorkOrderOptions.Add(option);
                    SelectedWorkOrder = option;
                }
            }

            UpdateContextSummary();
            TryPrefillTechnician(SelectedWorkOrder);
            return;
        }

        if (SelectedWorkOrderKindIndex < 0)
            return;

        if (SelectedWorkOrderKindIndex == 0)
        {
            var requests = await _requestService.GetRequestsByStatusesAsync(["accepted", "in_progress"]);
            foreach (var request in requests.OrderByDescending(r => r.UpdatedAt))
            {
                WorkOrderOptions.Add(new ToolRequisitionWorkOrderOption
                {
                    IsRequest = true,
                    Id = request.Id,
                    DisplayText = $"{request.RequestNumber} — {request.Title}",
                    AssigneeName = request.AssigneeName,
                });
            }
        }
        else
        {
            var schedule = (await _maintenanceService.GetScheduleAsync())
                .Where(s => WorkOrderRequisitionPolicy.AllowsToolRequisitionSchedule(s.Status))
                .OrderBy(s => s.PlannedDate);

            foreach (var item in schedule)
            {
                WorkOrderOptions.Add(new ToolRequisitionWorkOrderOption
                {
                    IsRequest = false,
                    Id = item.Id,
                    DisplayText = $"{item.AssetNumber} — {item.AssetName} ({MaintenanceTypeLabels.Get(item.MaintenanceType)})",
                });
            }
        }
    }

    private async Task RefreshTmsLinksAsync()
    {
        TmsLinks.Clear();
        _blockingWarehouseIds.Clear();
        OnPropertyChanged(nameof(HasTmsLinks));
        OnPropertyChanged(nameof(HasBlockingLinkForSelectedWarehouse));
        OnPropertyChanged(nameof(ShowDuplicateTmsHint));
        OnPropertyChanged(nameof(CanSubmitTms));

        var workOrder = ResolveWorkOrderRef();
        if (workOrder is null)
            return;

        await _tmsToolRequisitionService.RefreshWorkOrderStatusAsync(workOrder);

        var result = await _tmsToolRequisitionService.ListLocalByWorkOrderAsync(workOrder);
        if (!result.IsSuccess || result.Value is null)
            return;

        foreach (var link in result.Value.OrderByDescending(l => l.LastSyncedAt ?? l.CreatedAt))
        {
            if (TmsRequisitionStatuses.BlocksDuplicateSubmission(link.LastKnownStatus))
                _blockingWarehouseIds.Add(link.WarehouseId);

            TmsLinks.Add(new TmsLinkDisplayItem
            {
                WarehouseName = link.WarehouseName ?? "—",
                TmsRequisitionId = link.TmsRequisitionId,
                Status = link.LastKnownStatus,
                StatusLabel = FormatTmsStatus(link.LastKnownStatus),
                LastSyncedAt = link.LastSyncedAt ?? link.CreatedAt,
            });
        }

        OnPropertyChanged(nameof(HasTmsLinks));
        OnPropertyChanged(nameof(HasBlockingLinkForSelectedWarehouse));
        OnPropertyChanged(nameof(ShowDuplicateTmsHint));
        OnPropertyChanged(nameof(CanSubmitTms));
    }

    private static void SyncTmsIntegrationSettings()
    {
        var settings = SettingsHelper.Current;
        TmsIntegrationSettingsSync.Apply(
            settings.TmsIntegrationMode,
            settings.TmsBaseUrl,
            settings.TmsIntegrationSecret,
            settings.SupabaseAnonKey);
    }

    private TmsWorkOrderRef? ResolveWorkOrderRef()
    {
        if (SelectedWorkOrder is null)
            return null;

        return new TmsWorkOrderRef
        {
            Kind = SelectedWorkOrder.IsRequest ? TmsWorkOrderKind.Request : TmsWorkOrderKind.Schedule,
            Id = SelectedWorkOrder.Id,
        };
    }

    private void UpdateContextSummary()
    {
        if (SelectedWorkOrder is null)
        {
            ContextSummary = string.Empty;
            return;
        }

        ContextSummary = SelectedWorkOrder.IsRequest
            ? string.Format(
                CultureInfo.CurrentCulture,
                ResourceStrings.Get("ToolRequisition_Context_Request"),
                SelectedWorkOrder.DisplayText,
                SelectedWorkOrder.AssigneeName ?? ResourceStrings.Get("Common_None"))
            : SelectedWorkOrder.DisplayText;
    }

    private void TryPrefillTechnician(ToolRequisitionWorkOrderOption? option)
    {
        if (option?.SuggestedTechnicianId is Guid techId && techId != Guid.Empty)
        {
            SelectedTechnician = TechnicianOptions.FirstOrDefault(t => t.Id == techId);
            return;
        }

        if (option?.AssigneeName is not null)
        {
            SelectedTechnician = TechnicianOptions.FirstOrDefault(t =>
                string.Equals(t.FullName, option.AssigneeName, StringComparison.OrdinalIgnoreCase));
        }
    }

    private bool TryBuildInput(bool includeWarehouseId, out ToolRequisitionFormInput input)
    {
        input = null!;

        if (SelectedWorkOrder is null)
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_NoWorkOrder"), InfoBarSeverity.Error);
            return false;
        }

        if (SelectedTechnician is null)
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_NoTechnician"), InfoBarSeverity.Error);
            return false;
        }

        if (includeWarehouseId && SelectedTmsWarehouse is null)
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_NoWarehouseId"), InfoBarSeverity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(WarehouseName))
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_NoWarehouse"), InfoBarSeverity.Error);
            return false;
        }

        var lines = Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Name))
            .Select(l => new ToolRequisitionLine
            {
                Name = l.Name.Trim(),
                InventoryNumber = string.IsNullOrWhiteSpace(l.InventoryNumber) ? null : l.InventoryNumber.Trim(),
                Quantity = (int)Math.Max(1, Math.Round(l.Quantity)),
                LineNote = string.IsNullOrWhiteSpace(l.LineNote) ? null : l.LineNote.Trim(),
                ToolId = includeWarehouseId ? l.ToolId : null,
            })
            .ToList();

        if (lines.Count == 0 || lines.Any(l => l.Quantity <= 0))
        {
            InfoBanner.Report(ResourceStrings.Get("ToolRequisition_Error_InvalidQuantity"), InfoBarSeverity.Error);
            return false;
        }

        input = new ToolRequisitionFormInput
        {
            RequestId = SelectedWorkOrder.IsRequest ? SelectedWorkOrder.Id : null,
            ScheduleId = SelectedWorkOrder.IsRequest ? null : SelectedWorkOrder.Id,
            TechnicianId = SelectedTechnician.Id,
            WarehouseName = WarehouseName.Trim(),
            WarehouseId = includeWarehouseId ? SelectedTmsWarehouse!.WarehouseId : null,
            Lines = lines,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        };

        InfoBanner.Report(string.Empty);
        return true;
    }

    private string BuildSuggestedFileName()
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"Заявка-инструмент_{stamp}.docx";
    }

    private static string FormatTmsStatus(string status) => ToolRequisitionLabels.FormatTmsStatus(status);

    private static string ResolveError(DataError? error)
    {
        if (error is null)
            return ResourceStrings.Get("ToolRequisition_Error_SaveFailed");

        var localized = ResourceStrings.Get(error.MessageKey);
        if (error.MessageKey.StartsWith("TmsIntegration_", StringComparison.Ordinal))
        {
            var headline = localized != error.MessageKey ? localized : ResourceStrings.Get("ToolRequisition_Error_TmsFailed");
            return string.IsNullOrWhiteSpace(error.Detail) ? headline : $"{headline} {error.Detail}";
        }

        if (localized != error.MessageKey)
        {
            if (error.MessageKey == "DataError_Unknown" && !string.IsNullOrWhiteSpace(error.Detail))
                return error.Detail;

            return localized;
        }

        if (!string.IsNullOrWhiteSpace(error.Detail))
            return error.Detail;

        return ResourceStrings.Get("ToolRequisition_Error_SaveFailed");
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

        RealtimeUiRefresh.EnqueueDebounced("tool-requisition-form-links", RefreshTmsLinksAsync);
    }
}
