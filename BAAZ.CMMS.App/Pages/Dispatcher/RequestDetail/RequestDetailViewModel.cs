using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Models;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using BAAZ.CMMS.Core.Services.Requisitions;
using BAAZ.CMMS.Core.Services.TmsIssuance;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;

public sealed partial class RequestDetailViewModel : PageViewModelBase
{
    private static readonly (string Value, string ResourceKey)[] RepairZoneOptions =
    [
        ("on_site", "RepairZone_OnSite"),
        ("workshop", "RepairZone_Workshop"),
        ("external", "RepairZone_External"),
    ];

    private readonly IRequestService _requestService;
    private readonly IAuthService _authService;
    private readonly ITechnicianCatalogService _technicianCatalogService;
    private readonly IRepairDepartmentCatalogService _repairDepartmentCatalogService;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly INavigationService _navigationService;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;

    private Guid _requestId;
    private RequestDetailItem? _detail;
    private bool _realtimeSubscribed;

    public RequestDetailViewModel(
        IRequestService requestService,
        IAuthService authService,
        ITechnicianCatalogService technicianCatalogService,
        IRepairDepartmentCatalogService repairDepartmentCatalogService,
        IRealtimeNotificationService realtimeService,
        INavigationService navigationService,
        ITmsToolRequisitionService tmsToolRequisitionService)
    {
        _requestService = requestService;
        _authService = authService;
        _technicianCatalogService = technicianCatalogService;
        _repairDepartmentCatalogService = repairDepartmentCatalogService;
        _realtimeService = realtimeService;
        _navigationService = navigationService;
        _tmsToolRequisitionService = tmsToolRequisitionService;
    }

    public override string PageTitle => ResourceStrings.Get("RequestDetail_Title");

    public string LabelNumber => ResourceStrings.Get("MyRequests_Detail_Number");
    public string LabelType => ResourceStrings.Get("MyRequests_Detail_Type");
    public string LabelPriority => ResourceStrings.Get("MyRequests_Detail_Priority");
    public string LabelRepairZone => ResourceStrings.Get("MyRequests_Detail_RepairZone");
    public string LabelAsset => ResourceStrings.Get("MyRequests_Detail_Asset");
    public string LabelLocation => ResourceStrings.Get("MyRequests_Detail_Location");
    public string LabelDescription => ResourceStrings.Get("MyRequests_Detail_Description");
    public string LabelRequester => ResourceStrings.Get("RequestDetail_Label_Requester");
    public string LabelCreatedAt => ResourceStrings.Get("MyRequests_Detail_CreatedAt");
    public string LabelUpdatedAt => ResourceStrings.Get("MyRequests_Detail_UpdatedAt");

    public string SectionDepartments => ResourceStrings.Get("RequestDetail_Section_Departments");
    public string SectionActions => ResourceStrings.Get("RequestDetail_Section_Actions");
    public string SectionWorkReports => ResourceStrings.Get("RequestDetail_Section_WorkReports");
    public string SectionHistory => ResourceStrings.Get("MyRequests_History_Title");

    public string ActionAssignTechnician => ResourceStrings.Get("RequestDetail_Action_AssignTechnician");
    public string ActionStartWork => ResourceStrings.Get("RequestDetail_Action_StartWork");
    public string ActionChangeZone => ResourceStrings.Get("RequestDetail_Action_ChangeZone");
    public string ActionApply => ResourceStrings.Get("RequestDetail_Action_Apply");
    public string ActionTransferDepartment => ResourceStrings.Get("RequestDetail_Action_TransferDepartment");
    public string ActionAddDepartment => ResourceStrings.Get("RequestDetail_Action_AddDepartment");
    public string ActionSubmitWorkReport => ResourceStrings.Get("RequestDetail_Action_SubmitWorkReport");
    public string ActionCloseRequest => ResourceStrings.Get("RequestDetail_Action_CloseRequest");
    public string ActionMaterialRequisition => ResourceStrings.Get("RequestDetail_Action_MaterialRequisition");
    public string ActionToolRequisition => ResourceStrings.Get("RequestDetail_Action_ToolRequisition");
    public string SectionToolRequisitions => ResourceStrings.Get("RequestDetail_Section_ToolRequisitions");
    public string ActionAccept => ResourceStrings.Get("IncomingRequests_Action_Accept");
    public string ActionReject => ResourceStrings.Get("IncomingRequests_Action_Reject");

    public string ActionsEmptyText => ResourceStrings.Get("RequestDetail_Actions_Empty");

    public string ContractorNamePlaceholder => ResourceStrings.Get("RequestDetail_ContractorName_Placeholder");
    public string WorkPerformedLabel => ResourceStrings.Get("RequestDetail_WorkPerformed_Label");
    public string WorkPerformedPlaceholder => ResourceStrings.Get("RequestDetail_WorkPerformed_Placeholder");
    public string DurationLabel => ResourceStrings.Get("RequestDetail_Duration_Label");
    public string PartsUsedLabel => ResourceStrings.Get("RequestDetail_PartsUsed_Label");
    public string PartsUsedPlaceholder => ResourceStrings.Get("RequestDetail_PartsUsed_Placeholder");
    public string DefectsFoundLabel => ResourceStrings.Get("RequestDetail_DefectsFound_Label");
    public string DefectsFoundPlaceholder => ResourceStrings.Get("RequestDetail_DefectsFound_Placeholder");
    public string NotesLabel => ResourceStrings.Get("RequestDetail_Notes_Label");
    public string NotesPlaceholder => ResourceStrings.Get("RequestDetail_Notes_Placeholder");
    public string AssignedTechnicianLabel => ResourceStrings.Get("RequestDetail_AssignedTechnician_Label");
    public string WorkReportDepartmentLabel => ResourceStrings.Get("RequestDetail_WorkReport_Department_Label");
    public string TechnicianPickerPlaceholder => ResourceStrings.Get("Common_SelectTechnician");
    public string DepartmentPickerPlaceholder => ResourceStrings.Get("Common_SelectDepartment");
    public string RepairZonePickerPlaceholder => ResourceStrings.Get("Common_SelectRepairZone");
    public string MaintenanceTypeLabel => ResourceStrings.Get("RequestDetail_MaintenanceType_Label");
    public string MaintenanceTypeHint => ResourceStrings.Get("RequestDetail_MaintenanceType_Hint");
    public string MaintenanceTypeTo1Label => MaintenanceTypeLabels.Get("to1");
    public string MaintenanceTypeTo2Label => MaintenanceTypeLabels.Get("to2");
    public string MaintenanceTypeKrLabel => MaintenanceTypeLabels.Get("kr");

    public string HistoryEmptyText => ResourceStrings.Get("MyRequests_History_Empty");
    public string WorkReportsEmptyText => ResourceStrings.Get("RequestDetail_WorkReports_Empty");

    public IReadOnlyList<string> RepairZoneLabels { get; } =
        RepairZoneOptions.Select(o => ResourceStrings.Get(o.ResourceKey)).ToList();

    public ObservableCollection<RequestDepartmentRow> Departments { get; } = [];

    public ObservableCollection<WorkReportDisplayItem> WorkReports { get; } = [];

    public ObservableCollection<RequestHistoryDisplayItem> HistoryItems { get; } = [];

    public ObservableCollection<TmsLinkDisplayItem> ToolRequisitionLinks { get; } = [];

    public ObservableCollection<PickerOption> TechnicianOptions { get; } = [];

    public ObservableCollection<DepartmentTechnicianPickerOption> DepartmentTechnicianOptions { get; } = [];

    public bool UseDepartmentTechnicianPicker =>
        _authService.CurrentProfile?.Role == UserRole.Admin;

    public bool UseOwnDepartmentTechnicianPicker => !UseDepartmentTechnicianPicker;

    public ObservableCollection<PickerOption> TransferDepartmentOptions { get; } = [];

    public ObservableCollection<PickerOption> AddDepartmentOptions { get; } = [];

    public ObservableCollection<PickerOption> WorkReportDepartmentOptions { get; } = [];

    public bool ShowWorkReportDepartmentPicker =>
        UseDepartmentTechnicianPicker && CanSubmitWorkReport && WorkReportDepartmentOptions.Count > 1;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

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
    public partial string DetailAsset { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailLocation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailRequester { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailCreatedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailUpdatedAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailRepairZone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailContractorName { get; set; } = string.Empty;

    public bool HasContractorName => !string.IsNullOrWhiteSpace(DetailContractorName);

    [ObservableProperty]
    public partial bool HasDepartments { get; set; }

    [ObservableProperty]
    public partial bool HasWorkReports { get; set; }

    public bool ShowWorkReportsEmpty => !HasWorkReports;

    public bool HasHistoryItems => HistoryItems.Count > 0;

    public bool ShowHistoryEmpty => !HasHistoryItems;

    [ObservableProperty]
    public partial bool CanAssignTechnician { get; set; }

    [ObservableProperty]
    public partial bool CanStartWork { get; set; }

    [ObservableProperty]
    public partial bool CanChangeZone { get; set; }

    [ObservableProperty]
    public partial bool CanTransferDepartment { get; set; }

    [ObservableProperty]
    public partial bool CanAddDepartment { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowWorkReportDepartmentPicker))]
    public partial bool CanSubmitWorkReport { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRequisitionActions))]
    public partial bool CanCreateMaterialRequisition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRequisitionActions))]
    public partial bool CanCreateToolRequisition { get; set; }

    public bool ShowRequisitionActions =>
        CanCreateMaterialRequisition || CanCreateToolRequisition;

    public bool HasToolRequisitionLinks => ToolRequisitionLinks.Count > 0;

    [ObservableProperty]
    public partial bool CanCloseRequest { get; set; }

    [ObservableProperty]
    public partial bool CanAccept { get; set; }

    [ObservableProperty]
    public partial bool CanReject { get; set; }

    [ObservableProperty]
    public partial bool ShowDepartmentReportedBanner { get; set; }

    public string DepartmentReportedBannerText =>
        ResourceStrings.Get("RequestDetail_Banner_DepartmentReported");

    public bool ShowNoActions =>
        !CanAccept && !CanReject && !CanAssignTechnician && !CanStartWork && !CanChangeZone
        && !CanTransferDepartment && !CanAddDepartment && !CanSubmitWorkReport && !CanCloseRequest
        && !CanCreateMaterialRequisition && !CanCreateToolRequisition;

    [ObservableProperty]
    public partial PickerOption? SelectedTechnician { get; set; }

    [ObservableProperty]
    public partial DepartmentTechnicianPickerOption? SelectedDepartmentTechnician { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowContractorNameEditor))]
    public partial int SelectedRepairZoneIndex { get; set; } = -1;

    public bool ShowContractorNameEditor =>
        SelectedRepairZoneIndex >= 0
        && SelectedRepairZoneIndex < RepairZoneOptions.Length
        && RepairZoneOptions[SelectedRepairZoneIndex].Value == "external";

    [ObservableProperty]
    public partial string ContractorNameText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PickerOption? SelectedTransferDepartment { get; set; }

    [ObservableProperty]
    public partial PickerOption? SelectedAddDepartment { get; set; }

    [ObservableProperty]
    public partial PickerOption? SelectedAddDepartmentTechnician { get; set; }

    [ObservableProperty]
    public partial string WorkPerformedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double DurationHours { get; set; } = 1;

    [ObservableProperty]
    public partial string PartsUsedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DefectsFoundText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NotesText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsActionBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowWorkReportDepartmentPicker))]
    public partial bool ShowMaintenanceTypePicker { get; set; }

    [ObservableProperty]
    public partial bool MaintenanceTypeTo1Selected { get; set; }

    [ObservableProperty]
    public partial bool MaintenanceTypeTo2Selected { get; set; }

    [ObservableProperty]
    public partial bool MaintenanceTypeKrSelected { get; set; }

    [ObservableProperty]
    public partial PickerOption? SelectedWorkReportDepartment { get; set; }

    [ObservableProperty]
    public partial string AssignedTechnicianDisplay { get; set; } = string.Empty;

    partial void OnHasWorkReportsChanged(bool value) => OnPropertyChanged(nameof(ShowWorkReportsEmpty));

    public async Task OnPageLoadedAsync(object? navigationParameter)
    {
        if (navigationParameter is RequestDetailNavigationArgs args)
            _requestId = args.RequestId;
        else if (navigationParameter is Guid id)
            _requestId = id;
        else
            return;

        SubscribeRealtime();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task AssignTechnicianAsync()
    {
        if (IsActionBusy)
            return;

        if (UseDepartmentTechnicianPicker)
        {
            if (SelectedDepartmentTechnician is null)
                return;

            await RunActionAsync(() => _requestService.AssignRequestAsync(
                _requestId,
                SelectedDepartmentTechnician.TechnicianId,
                _authService.CurrentProfile?.Id ?? Guid.Empty,
                SelectedDepartmentTechnician.RepairDepartmentId));
            return;
        }

        if (SelectedTechnician is null)
            return;

        await RunActionAsync(() => _requestService.AssignRequestAsync(
            _requestId, SelectedTechnician.Id, _authService.CurrentProfile?.Id ?? Guid.Empty));
    }

    [RelayCommand]
    private async Task StartWorkAsync()
    {
        if (IsActionBusy)
            return;

        await RunActionAsync(() => _requestService.StartWorkAsync(_requestId));
    }

    [RelayCommand]
    private void OpenMaterialRequisition()
    {
        if (!CanCreateMaterialRequisition)
            return;

        _navigationService.NavigateTo(
            "MaterialRequisition",
            new MaterialRequisitionNavigationArgs { RequestId = _requestId });
    }

    [RelayCommand]
    private void OpenToolRequisition()
    {
        if (!CanCreateToolRequisition)
            return;

        _navigationService.NavigateTo(
            "ToolRequisition",
            new ToolRequisitionNavigationArgs { RequestId = _requestId });
    }

    [RelayCommand]
    private async Task ChangeZoneAsync()
    {
        if (IsActionBusy || SelectedRepairZoneIndex < 0 || SelectedRepairZoneIndex >= RepairZoneOptions.Length)
        {
            InfoBanner.Report(ResourceStrings.Get("RequestDetail_Error_RepairZoneRequired"), InfoBarSeverity.Error);
            return;
        }

        var zone = RepairZoneOptions[SelectedRepairZoneIndex].Value;
        var contractorName = zone == "external" && !string.IsNullOrWhiteSpace(ContractorNameText)
            ? ContractorNameText.Trim()
            : null;

        await RunActionAsync(() => _requestService.UpdateRepairZoneAsync(_requestId, zone, contractorName));
    }

    [RelayCommand]
    private async Task TransferDepartmentAsync()
    {
        if (SelectedTransferDepartment is null || IsActionBusy)
            return;

        var confirmed = await AppDialogHelper.ConfirmAsync(
            ResourceStrings.Get("RequestDetail_Confirm_Transfer_Title"),
            ResourceStrings.Get("RequestDetail_Confirm_Transfer_Message"),
            App.MainWindow);

        if (!confirmed)
            return;

        await RunActionAsync(() => _requestService.TransferDepartmentAsync(_requestId, SelectedTransferDepartment.Id));
    }

    [RelayCommand]
    private async Task AddDepartmentAsync()
    {
        if (SelectedAddDepartment is null || IsActionBusy)
            return;

        await RunActionAsync(() => _requestService.AddDepartmentAsync(
            _requestId, SelectedAddDepartment.Id, SelectedAddDepartmentTechnician?.Id));
    }

    [RelayCommand]
    private async Task SubmitWorkReportAsync()
    {
        if (IsActionBusy || _detail is null)
            return;

        var profile = _authService.CurrentProfile;
        if (profile is null)
            return;

        var repairDepartmentId = ResolveWorkReportDepartmentId();
        var assignedTechnicianId = ResolveWorkReportTechnicianId(repairDepartmentId);

        var validationError = WorkReportFormValidation.Validate(
            assignedTechnicianId is not null,
            WorkPerformedText,
            DurationHours);
        if (validationError is not null)
        {
            InfoBanner.Report(validationError, InfoBarSeverity.Error);
            return;
        }

        if (repairDepartmentId is null)
        {
            InfoBanner.Report(ResourceStrings.Get("WorkReport_Error_DepartmentRequired"), InfoBarSeverity.Error);
            return;
        }

        var duration = (decimal)DurationHours;
        var maintenanceTypes = ResolveSelectedMaintenanceTypes();

        var input = new WorkReportInput
        {
            TechnicianId = assignedTechnicianId!.Value,
            WorkPerformed = WorkPerformedText,
            ActualDurationHours = duration,
            PartsUsed = string.IsNullOrWhiteSpace(PartsUsedText) ? null : PartsUsedText,
            DefectsFound = string.IsNullOrWhiteSpace(DefectsFoundText) ? null : DefectsFoundText,
            Notes = string.IsNullOrWhiteSpace(NotesText) ? null : NotesText,
            MaintenanceTypes = maintenanceTypes.Count > 0 ? maintenanceTypes : null,
        };

        IsActionBusy = true;
        try
        {
            var ok = await _requestService.CreateWorkReportAsync(_requestId, repairDepartmentId.Value, profile.Id, input);
            if (!ok)
            {
                InfoBanner.Report(ResourceStrings.Get("RequestDetail_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            WorkPerformedText = string.Empty;
            DurationHours = 1;
            PartsUsedText = string.Empty;
            DefectsFoundText = string.Empty;
            NotesText = string.Empty;
            MaintenanceTypeTo1Selected = false;
            MaintenanceTypeTo2Selected = false;
            MaintenanceTypeKrSelected = false;

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("RequestDetail_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    partial void OnWorkPerformedTextChanged(string value) => ClearWorkReportFormBanner();

    partial void OnDurationHoursChanged(double value) => ClearWorkReportFormBanner();

    partial void OnSelectedWorkReportDepartmentChanged(PickerOption? value)
    {
        UpdateWorkReportAssignedTechnician();
        ClearWorkReportFormBanner();
    }

    partial void OnMaintenanceTypeTo1SelectedChanged(bool value) => ClearWorkReportFormBanner();

    partial void OnMaintenanceTypeTo2SelectedChanged(bool value) => ClearWorkReportFormBanner();

    partial void OnMaintenanceTypeKrSelectedChanged(bool value) => ClearWorkReportFormBanner();

    private void ClearWorkReportFormBanner() => InfoBanner.Report(string.Empty);

    [RelayCommand]
    private async Task CloseRequestAsync()
    {
        if (IsActionBusy)
            return;

        await RunActionAsync(() => _requestService.CloseRequestAsStaffAsync(_requestId));
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (!CanAccept || IsActionBusy)
            return;

        await RunActionAsync(() => _requestService.AcceptRequestAsync(
            _requestId,
            UseDepartmentTechnicianPicker
                ? SelectedDepartmentTechnician?.TechnicianId
                : SelectedTechnician?.Id));
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        if (!CanReject || IsActionBusy)
            return;

        var profile = _authService.CurrentProfile;
        if (profile is null)
            return;

        var comment = await AppDialogHelper.PromptTextAsync(
            ResourceStrings.Get("IncomingRequests_Confirm_Reject_Title"),
            ResourceStrings.Get("IncomingRequests_Confirm_Reject_Placeholder"),
            App.MainWindow,
            required: true);

        if (comment is null)
            return;

        await RunActionAsync(() => _requestService.RejectRequestAsync(_requestId, profile.Id, comment));
    }

    private IReadOnlyList<string> ResolveSelectedMaintenanceTypes()
    {
        var types = new List<string>(3);
        if (MaintenanceTypeTo1Selected)
            types.Add("to1");
        if (MaintenanceTypeTo2Selected)
            types.Add("to2");
        if (MaintenanceTypeKrSelected)
            types.Add("kr");
        return types;
    }

    private Guid? ResolveWorkReportDepartmentId()
    {
        if (UseDepartmentTechnicianPicker)
            return SelectedWorkReportDepartment?.Id;

        return _authService.CurrentProfile?.RepairDepartmentId;
    }

    private Guid? ResolveWorkReportTechnicianId(Guid? repairDepartmentId)
    {
        if (_detail is null || repairDepartmentId is null)
            return null;

        return _detail.Departments
            .FirstOrDefault(d => d.RepairDepartmentId == repairDepartmentId.Value)?
            .AssigneeId;
    }

    private void UpdateWorkReportAssignedTechnician()
    {
        var repairDepartmentId = ResolveWorkReportDepartmentId();
        if (_detail is null || repairDepartmentId is null)
        {
            AssignedTechnicianDisplay = RequestDetailDisplayHelper.UnassignedAssigneeText;
            return;
        }

        var department = _detail.Departments.FirstOrDefault(d => d.RepairDepartmentId == repairDepartmentId);
        AssignedTechnicianDisplay = RequestDetailDisplayHelper.FormatAssigneeName(department?.AssigneeName);
    }

    private void LoadWorkReportDepartmentOptions(IReadOnlyList<RequestDepartmentItem> pendingDepartments)
    {
        WorkReportDepartmentOptions.Clear();
        foreach (var department in pendingDepartments.OrderBy(d => d.DepartmentName, StringComparer.CurrentCultureIgnoreCase))
        {
            WorkReportDepartmentOptions.Add(new PickerOption
            {
                Id = department.RepairDepartmentId,
                Name = department.DepartmentName,
            });
        }

        SelectedWorkReportDepartment = WorkReportDepartmentOptions.FirstOrDefault();
        OnPropertyChanged(nameof(ShowWorkReportDepartmentPicker));
        UpdateWorkReportAssignedTechnician();
    }

    private async Task RunActionAsync(Func<Task<DataResult>> action)
    {
        IsActionBusy = true;
        try
        {
            var result = await action();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResolveActionError(result.Error), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("RequestDetail_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    private static string ResolveActionError(DataError? error)
    {
        if (error is null)
            return ResourceStrings.Get("RequestDetail_Error_Action");

        var localized = ResourceStrings.Get(error.MessageKey);
        return localized == error.MessageKey && !string.IsNullOrWhiteSpace(error.Detail)
            ? error.Detail
            : localized;
    }

    private async Task LoadTechnicianPickerOptionsAsync(
        RequestDetailItem detail,
        IReadOnlySet<Guid> reportedDeptIds)
    {
        var ownDepartmentId = _authService.CurrentProfile?.RepairDepartmentId;
        var isAdmin = _authService.CurrentProfile?.Role == UserRole.Admin;

        SelectedTechnician = null;
        SelectedDepartmentTechnician = null;
        OnPropertyChanged(nameof(UseDepartmentTechnicianPicker));
        OnPropertyChanged(nameof(UseOwnDepartmentTechnicianPicker));

        var technicians = await _technicianCatalogService.GetTechniciansAsync();

        TechnicianOptions.Clear();
        DepartmentTechnicianOptions.Clear();
        if (!technicians.IsSuccess)
            return;

        var activeTechnicians = technicians.Value!.Where(t => t.IsActive).ToList();

        if (isAdmin)
        {
            var involvedDepartmentIds = detail.Departments
                .Select(d => d.RepairDepartmentId)
                .ToHashSet();

            if (involvedDepartmentIds.Count == 0 && detail.TargetRepairDepartmentId is Guid targetDepartmentId)
                involvedDepartmentIds.Add(targetDepartmentId);

            foreach (var technician in activeTechnicians
                         .Where(t => t.RepairDepartmentId is Guid departmentId
                             && involvedDepartmentIds.Contains(departmentId)
                             && !reportedDeptIds.Contains(departmentId))
                         .DistinctBy(t => t.Id)
                         .OrderBy(t => t.RepairDepartmentName)
                         .ThenBy(t => t.FullName))
            {
                var departmentName = technician.RepairDepartmentName
                    ?? detail.Departments.FirstOrDefault(d => d.RepairDepartmentId == technician.RepairDepartmentId)?.DepartmentName
                    ?? detail.TargetRepairDepartmentName
                    ?? ResourceStrings.Get("Common_None");

                DepartmentTechnicianOptions.Add(new DepartmentTechnicianPickerOption
                {
                    RepairDepartmentId = technician.RepairDepartmentId!.Value,
                    TechnicianId = technician.Id,
                    DisplayName = $"{departmentName} — {technician.FullName}",
                });
            }
        }
        else
        {
            foreach (var technician in activeTechnicians
                         .Where(t => t.RepairDepartmentId == ownDepartmentId)
                         .DistinctBy(t => t.Id))
                TechnicianOptions.Add(new PickerOption { Id = technician.Id, Name = technician.FullName });
        }

        var departments = await _repairDepartmentCatalogService.GetRepairDepartmentsAsync();
        TransferDepartmentOptions.Clear();
        AddDepartmentOptions.Clear();
        if (departments.IsSuccess)
        {
            foreach (var department in departments.Value!.Where(d => d.Id != ownDepartmentId))
            {
                TransferDepartmentOptions.Add(new PickerOption { Id = department.Id, Name = department.Name });
                AddDepartmentOptions.Add(new PickerOption { Id = department.Id, Name = department.Name });
            }
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var detail = await _requestService.GetRequestByIdAsync(_requestId);
            if (detail is null)
            {
                HasDetail = false;
                return;
            }

            _detail = detail;

            DetailTitle = detail.Title;
            DetailNumber = detail.RequestNumber;
            DetailStatusLabel = RequestStatusHelper.GetLabel(detail.Status);
            var statusBadge = StatusBadgeFactory.ForRequest(detail.Status);
            DetailStatusBackgroundKey = statusBadge.BackgroundKey;
            DetailStatusForegroundKey = statusBadge.ForegroundKey;
            DetailType = RequestEnumLabels.Type(detail.Type);
            DetailPriority = RequestEnumLabels.Priority(detail.Priority);
            DetailAsset = RequestDetailDisplayHelper.FormatAsset(detail);
            DetailLocation = detail.LocationDescription;
            DetailDescription = detail.Description;
            DetailRequester = detail.RequesterName ?? ResourceStrings.Get("Common_None");
            DetailCreatedAt = DateTimeDisplayHelper.Format(detail.CreatedAt);
            DetailUpdatedAt = DateTimeDisplayHelper.Format(detail.UpdatedAt);
            DetailRepairZone = RequestEnumLabels.RepairZone(detail.RepairZone);
            DetailContractorName = detail.ContractorName ?? string.Empty;
            OnPropertyChanged(nameof(HasContractorName));
            ApplyRepairZonePickerFromDetail(detail);
            HasDetail = true;

            var ownDepartmentId = _authService.CurrentProfile?.RepairDepartmentId;
            var isAdmin = _authService.CurrentProfile?.Role == UserRole.Admin;
            var isTargetDepartment = isAdmin
                || (ownDepartmentId is not null && detail.TargetRepairDepartmentId == ownDepartmentId);
            var ownDepartmentInvolved = isAdmin || detail.Departments.Any(d => d.RepairDepartmentId == ownDepartmentId);

            Departments.Clear();
            if (detail.Departments.Count > 0)
            {
                foreach (var department in detail.Departments)
                    Departments.Add(RequestDepartmentRow.From(department, ownDepartmentId));
            }
            else if (!string.IsNullOrWhiteSpace(detail.TargetRepairDepartmentName))
            {
                var pendingText = ResourceStrings.Get("RequestDepartment_TargetPending");
                Departments.Add(new RequestDepartmentRow
                {
                    RepairDepartmentId = detail.TargetRepairDepartmentId ?? Guid.Empty,
                    DepartmentName = detail.TargetRepairDepartmentName,
                    AssigneeText = pendingText,
                    StatusIconGlyph = RequestDepartmentRow.ResolveStatusIconGlyph(pendingText),
                    IsOwnDepartment = isTargetDepartment,
                });
            }

            HasDepartments = Departments.Count > 0;

            var reports = await _requestService.GetWorkReportsForRequestAsync(_requestId);
            WorkReports.Clear();
            foreach (var report in reports)
                WorkReports.Add(WorkReportDisplayItem.From(report));
            HasWorkReports = WorkReports.Count > 0;

            var reportedDeptIds = reports.Select(r => r.RepairDepartmentId).ToHashSet();
            var pendingReportDepartments = detail.Departments
                .Where(d => !reportedDeptIds.Contains(d.RepairDepartmentId))
                .ToList();

            await LoadTechnicianPickerOptionsAsync(detail, reportedDeptIds);

            var ownDepartmentHasReported = ownDepartmentId is not null
                && reportedDeptIds.Contains(ownDepartmentId.Value);

            var isAccepted = detail.Status == "accepted";
            var isAssignableStatus = detail.Status is "accepted" or "in_progress";
            var allDepartmentsHaveAssignee = detail.Departments.Count > 0
                && detail.Departments.All(d => d.AssigneeId is not null);

            var ownCanPrepare = ownDepartmentInvolved
                && isAccepted
                && !ownDepartmentHasReported;
            var ownCanExecute = ownDepartmentInvolved
                && detail.Status == "in_progress"
                && !ownDepartmentHasReported;

            if (isAdmin)
            {
                CanAssignTechnician = isAssignableStatus && pendingReportDepartments.Count > 0;
                CanChangeZone = isAccepted;
                CanTransferDepartment = isAccepted;
                CanAddDepartment = isAssignableStatus;
                CanStartWork = isAccepted && allDepartmentsHaveAssignee;
            }
            else
            {
                CanAssignTechnician = isAssignableStatus && ownDepartmentInvolved && !ownDepartmentHasReported;
                CanChangeZone = ownCanPrepare;
                CanTransferDepartment = ownCanPrepare;
                CanAddDepartment = isAssignableStatus && ownDepartmentInvolved && !ownDepartmentHasReported;
                CanStartWork = ownCanPrepare && allDepartmentsHaveAssignee;
            }

            CanSubmitWorkReport = detail.Status == "in_progress" && (
                isAdmin
                    ? pendingReportDepartments.Count > 0
                    : ownCanExecute);
            ShowMaintenanceTypePicker = CanSubmitWorkReport && detail.AssetId is not null;
            if (CanSubmitWorkReport)
            {
                if (isAdmin)
                    LoadWorkReportDepartmentOptions(pendingReportDepartments);
                else
                {
                    WorkReportDepartmentOptions.Clear();
                    UpdateWorkReportAssignedTechnician();
                    OnPropertyChanged(nameof(ShowWorkReportDepartmentPicker));
                }
            }
            else
            {
                WorkReportDepartmentOptions.Clear();
                AssignedTechnicianDisplay = string.Empty;
                OnPropertyChanged(nameof(ShowWorkReportDepartmentPicker));
            }

            MaintenanceTypeTo1Selected = false;
            MaintenanceTypeTo2Selected = false;
            MaintenanceTypeKrSelected = false;
            CanCreateMaterialRequisition = ownDepartmentInvolved
                && WorkOrderRequisitionPolicy.AllowsMaterialRequisition(detail.Status)
                && !ownDepartmentHasReported;
            CanCreateToolRequisition = ownDepartmentInvolved
                && WorkOrderRequisitionPolicy.AllowsToolRequisition(detail.Status)
                && !ownDepartmentHasReported;
            CanCloseRequest = ownDepartmentInvolved && detail.Status == "completed";
            ShowDepartmentReportedBanner = !isAdmin
                && ownDepartmentHasReported
                && detail.Status is "in_progress" or "completed";
            CanAccept = detail.Status == "new" && isTargetDepartment;
            CanReject = detail.Status == "new" && isTargetDepartment;
            OnPropertyChanged(nameof(ShowNoActions));

            await LoadToolRequisitionLinksAsync();
            await LoadHistoryAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
            HasDetail = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyRepairZonePickerFromDetail(RequestDetailItem detail)
    {
        var zoneIndex = Array.FindIndex(RepairZoneOptions, option => option.Value == detail.RepairZone);
        SelectedRepairZoneIndex = zoneIndex >= 0 ? zoneIndex : 0;
        ContractorNameText = detail.ContractorName ?? string.Empty;
    }

    private async Task LoadToolRequisitionLinksAsync()
    {
        ToolRequisitionLinks.Clear();
        OnPropertyChanged(nameof(HasToolRequisitionLinks));

        var result = await _tmsToolRequisitionService.ListLocalByWorkOrderAsync(new TmsWorkOrderRef
        {
            Kind = TmsWorkOrderKind.Request,
            Id = _requestId,
        });

        if (!result.IsSuccess || result.Value is null)
            return;

        foreach (var link in result.Value.OrderByDescending(l => l.LastSyncedAt ?? l.CreatedAt))
        {
            ToolRequisitionLinks.Add(new TmsLinkDisplayItem
            {
                WarehouseName = link.WarehouseName ?? "—",
                Status = link.LastKnownStatus,
                StatusLabel = ToolRequisitionLabels.FormatTmsStatus(link.LastKnownStatus),
                LastSyncedAt = link.LastSyncedAt ?? link.CreatedAt,
            });
        }

        OnPropertyChanged(nameof(HasToolRequisitionLinks));
    }

    private async Task LoadHistoryAsync()
    {
        HistoryItems.Clear();
        var history = await _requestService.GetStatusHistoryAsync(_requestId);
        foreach (var item in history)
            HistoryItems.Add(RequestHistoryDisplayItem.From(item));

        OnPropertyChanged(nameof(HasHistoryItems));
        OnPropertyChanged(nameof(ShowHistoryEmpty));
    }

    private void SubscribeRealtime()
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
        if (e.Table is not "requests" and not "request_repair_departments" and not "work_reports")
            return;

        RealtimeUiRefresh.Enqueue(LoadAsync);
    }
}
