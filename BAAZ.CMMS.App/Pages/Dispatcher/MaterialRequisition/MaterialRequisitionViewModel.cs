using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Services;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using BAAZ.CMMS.Core.Services.MaterialRequisition;
using BAAZ.CMMS.Core.Services.Requisitions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed partial class MaterialRequisitionViewModel : PageViewModelBase
{
    private static readonly string[] UnitPresets = ["шт", "кг", "л", "м"];

    private readonly IMaterialRequisitionService _materialRequisitionService;
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly ITechnicianCatalogService _technicianCatalogService;
    private readonly IDocumentSaveLocationService _saveLocationService;
    private readonly IWindowsShellFileService _shellFileService;

    private Guid? _lockedRequestId;
    private Guid? _lockedScheduleId;
    private IReadOnlyList<TechnicianListItem> _technicians = [];

    public MaterialRequisitionViewModel(
        IMaterialRequisitionService materialRequisitionService,
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        ITechnicianCatalogService technicianCatalogService,
        IDocumentSaveLocationService saveLocationService,
        IWindowsShellFileService shellFileService)
    {
        _materialRequisitionService = materialRequisitionService;
        _requestService = requestService;
        _maintenanceService = maintenanceService;
        _technicianCatalogService = technicianCatalogService;
        _saveLocationService = saveLocationService;
        _shellFileService = shellFileService;

        AttachLine(new MaterialRequisitionLineRow());
        WarehouseName = ResourceStrings.Get("MaterialRequisition_Default_WarehouseName");
    }

    public override string PageTitle => ResourceStrings.Get("Nav_MaterialRequisition");

    public string SectionWorkOrder => ResourceStrings.Get("MaterialRequisition_Section_WorkOrder");
    public string SectionWarehouse => ResourceStrings.Get("MaterialRequisition_Section_Warehouse");
    public string SectionLines => ResourceStrings.Get("MaterialRequisition_Section_Lines");
    public string SectionLinesHint => ResourceStrings.Get("MaterialRequisition_Section_Lines_Hint");
    public string SectionNotes => ResourceStrings.Get("MaterialRequisition_Section_Notes");
    public string LabelWorkOrderKind => ResourceStrings.Get("MaterialRequisition_Label_WorkOrderKind");
    public string LabelWorkOrder => ResourceStrings.Get("MaterialRequisition_Label_WorkOrder");
    public string LabelTechnician => ResourceStrings.Get("MaterialRequisition_Label_Technician");
    public string LabelWarehouseName => ResourceStrings.Get("MaterialRequisition_Label_WarehouseName");
    public string LineNameHeader => ResourceStrings.Get("MaterialRequisition_Line_Name");
    public string LineSkuHeader => ResourceStrings.Get("MaterialRequisition_Line_Sku");
    public string LineQuantityHeader => ResourceStrings.Get("MaterialRequisition_Line_Quantity");
    public string LineUnitHeader => ResourceStrings.Get("MaterialRequisition_Line_Unit");
    public string LineNoteHeader => ResourceStrings.Get("MaterialRequisition_Line_LineNote");
    public string LineNamePlaceholder => ResourceStrings.Get("MaterialRequisition_Line_Name_Placeholder");
    public string LineSkuPlaceholder => ResourceStrings.Get("MaterialRequisition_Line_Sku_Placeholder");
    public string LineUnitPlaceholder => ResourceStrings.Get("MaterialRequisition_Line_Unit_Placeholder");
    public string LineNotePlaceholder => ResourceStrings.Get("MaterialRequisition_Line_LineNote_Placeholder");
    public string LineItemTitlePrefix => ResourceStrings.Get("MaterialRequisition_Line_ItemTitle");
    public string ActionAddLine => ResourceStrings.Get("MaterialRequisition_Action_AddLine");
    public string ActionRemoveLine => ResourceStrings.Get("MaterialRequisition_Action_RemoveLine");
    public string ActionSubmit => ResourceStrings.Get("Common_Action_GenerateDocx");
    public string NotesPlaceholder => ResourceStrings.Get("MaterialRequisition_Notes_Placeholder");
    public string WorkOrderKindPlaceholder => ResourceStrings.Get("Common_SelectWorkOrderKind");
    public string WorkOrderPlaceholder => ResourceStrings.Get("Common_SelectWorkOrder");
    public string TechnicianPlaceholder => ResourceStrings.Get("Common_SelectTechnician");

    public IReadOnlyList<string> WorkOrderKindLabels { get; } =
    [
        ResourceStrings.Get("MaterialRequisition_Kind_Request"),
        ResourceStrings.Get("MaterialRequisition_Kind_Schedule"),
    ];

    public IReadOnlyList<string> UnitOptions { get; } = UnitPresets;

    public ObservableCollection<MaterialRequisitionWorkOrderOption> WorkOrderOptions { get; } = [];

    public ObservableCollection<TechnicianListItem> TechnicianOptions { get; } = [];

    public ObservableCollection<MaterialRequisitionLineRow> Lines { get; } = [];

    [ObservableProperty]
    public partial int SelectedWorkOrderKindIndex { get; set; } = -1;

    [ObservableProperty]
    public partial MaterialRequisitionWorkOrderOption? SelectedWorkOrder { get; set; }

    [ObservableProperty]
    public partial TechnicianListItem? SelectedTechnician { get; set; }

    [ObservableProperty]
    public partial string WarehouseName { get; set; }

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

    public bool ShowWorkOrderPicker => !IsWorkOrderLocked;

    partial void OnIsSubmittingChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    partial void OnSelectedWorkOrderKindIndexChanged(int value)
    {
        if (!IsWorkOrderLocked)
            _ = LoadWorkOrderOptionsAsync();
    }

    partial void OnSelectedWorkOrderChanged(MaterialRequisitionWorkOrderOption? value)
    {
        UpdateContextSummary();
        TryPrefillTechnician(value);
    }

    partial void OnIsWorkOrderLockedChanged(bool value) =>
        OnPropertyChanged(nameof(ShowWorkOrderPicker));

    public async Task OnPageLoadedAsync(object? navigationParameter)
    {
        if (navigationParameter is MaterialRequisitionNavigationArgs args)
        {
            _lockedRequestId = args.RequestId;
            _lockedScheduleId = args.ScheduleId;
            IsWorkOrderLocked = args.RequestId is not null || args.ScheduleId is not null;
            if (args.RequestId is not null)
                SelectedWorkOrderKindIndex = 0;
            if (args.ScheduleId is not null)
                SelectedWorkOrderKindIndex = 1;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private void AddLine() => AttachLine(new MaterialRequisitionLineRow());

    [RelayCommand]
    private void RemoveLine(MaterialRequisitionLineRow? row)
    {
        if (row is null || Lines.Count <= 1)
            return;

        Lines.Remove(row);
        RefreshLineNumbers();
    }

    private void AttachLine(MaterialRequisitionLineRow row)
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

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsSubmitting || !TryBuildInput(out var input))
            return;

        var suggestedName = BuildSuggestedFileName();
        var targetPath = await _saveLocationService.PickDocxSavePathAsync(suggestedName);
        if (targetPath is null)
            return;

        IsSubmitting = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var result = await _materialRequisitionService.SubmitAsync(input, targetPath);
            if (!result.IsSuccess || result.Value is null)
            {
                InfoBanner.Report(ResolveError(result.Error), InfoBarSeverity.Error);
                return;
            }

            var open = await AppDialogHelper.ConfirmSuccessAsync(
                ResourceStrings.Get("MaterialRequisition_Submit_Success_Title"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("MaterialRequisition_Submit_Success_Message"),
                    result.Value.SavedFilePath),
                App.MainWindow);

            if (open)
                await _shellFileService.OpenFileAsync(result.Value.SavedFilePath);
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaterialRequisition_Error_SaveFailed"), InfoBarSeverity.Error);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
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

            await LoadWorkOrderOptionsAsync();
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

    private async Task LoadWorkOrderOptionsAsync()
    {
        WorkOrderOptions.Clear();
        SelectedWorkOrder = null;

        if (IsWorkOrderLocked)
        {
            if (_lockedRequestId is Guid requestId)
            {
                var detail = await _requestService.GetRequestByIdAsync(requestId);
                if (detail is not null)
                {
                    var option = new MaterialRequisitionWorkOrderOption
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
                    .FirstOrDefault(s => s.Id == scheduleId);
                if (item is not null)
                {
                    var option = new MaterialRequisitionWorkOrderOption
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
                WorkOrderOptions.Add(new MaterialRequisitionWorkOrderOption
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
            var schedule = await _maintenanceService.GetScheduleAsync();
            foreach (var item in schedule
                         .Where(s => WorkOrderRequisitionPolicy.AllowsMaterialRequisitionSchedule(s.Status))
                         .OrderBy(s => s.PlannedDate))
            {
                WorkOrderOptions.Add(new MaterialRequisitionWorkOrderOption
                {
                    IsRequest = false,
                    Id = item.Id,
                    DisplayText = $"{item.AssetNumber} — {item.AssetName} ({MaintenanceTypeLabels.Get(item.MaintenanceType)})",
                });
            }
        }
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
                ResourceStrings.Get("MaterialRequisition_Context_Request"),
                SelectedWorkOrder.DisplayText,
                SelectedWorkOrder.AssigneeName ?? ResourceStrings.Get("Common_None"))
            : SelectedWorkOrder.DisplayText;
    }

    private void TryPrefillTechnician(MaterialRequisitionWorkOrderOption? option)
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

    private bool TryBuildInput(out MaterialRequisitionInput input)
    {
        input = null!;

        if (SelectedWorkOrder is null)
        {
            InfoBanner.Report(ResourceStrings.Get("MaterialRequisition_Error_NoWorkOrder"), InfoBarSeverity.Error);
            return false;
        }

        if (SelectedTechnician is null)
        {
            InfoBanner.Report(ResourceStrings.Get("MaterialRequisition_Error_NoTechnician"), InfoBarSeverity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(WarehouseName))
        {
            InfoBanner.Report(ResourceStrings.Get("MaterialRequisition_Error_NoWarehouse"), InfoBarSeverity.Error);
            return false;
        }

        var lines = Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Name))
            .Select(l => new MaterialRequisitionLine
            {
                Sku = string.IsNullOrWhiteSpace(l.Sku) ? null : l.Sku.Trim(),
                Name = l.Name.Trim(),
                Quantity = (decimal)l.Quantity,
                Unit = string.IsNullOrWhiteSpace(l.Unit) ? "шт" : l.Unit.Trim(),
                LineNote = string.IsNullOrWhiteSpace(l.LineNote) ? null : l.LineNote.Trim(),
            })
            .ToList();

        if (lines.Count == 0 || lines.Any(l => l.Quantity <= 0))
        {
            InfoBanner.Report(ResourceStrings.Get("MaterialRequisition_Error_InvalidQuantity"), InfoBarSeverity.Error);
            return false;
        }

        input = new MaterialRequisitionInput
        {
            RequestId = SelectedWorkOrder.IsRequest ? SelectedWorkOrder.Id : null,
            ScheduleId = SelectedWorkOrder.IsRequest ? null : SelectedWorkOrder.Id,
            TechnicianId = SelectedTechnician.Id,
            WarehouseName = WarehouseName.Trim(),
            Lines = lines,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        };

        InfoBanner.Report(string.Empty);
        return true;
    }

    private string BuildSuggestedFileName()
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"Заявка-ТМЦ_{stamp}.docx";
    }

    private static string ResolveError(DataError? error)
    {
        if (error is null)
            return ResourceStrings.Get("MaterialRequisition_Error_SaveFailed");

        var localized = ResourceStrings.Get(error.MessageKey);
        return localized == error.MessageKey && !string.IsNullOrWhiteSpace(error.Detail)
            ? error.Detail
            : localized;
    }
}
