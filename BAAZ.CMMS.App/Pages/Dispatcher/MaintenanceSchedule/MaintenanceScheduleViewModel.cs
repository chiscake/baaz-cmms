using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.MaintenanceTypePicker;
using BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Models;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;
using BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;
using BAAZ.CMMS.App.Services.Notifications;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceScheduleViewModel : PageViewModelBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAssetRepository _assetRepository;
    private readonly IRepairDepartmentCatalogService _repairDepartmentCatalogService;
    private readonly ITechnicianCatalogService _technicianCatalogService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly INavBadgeService _navBadgeService;
    private readonly ILocationTreeCache _locationTreeCache;

    private bool _realtimeSubscribed;
    private int _realtimeReloadSuppressCount;
    private IReadOnlyList<MaintenanceScheduleItem> _allItems = [];

    public MaintenanceScheduleViewModel(
        IMaintenanceService maintenanceService,
        IAssetRepository assetRepository,
        IRepairDepartmentCatalogService repairDepartmentCatalogService,
        ITechnicianCatalogService technicianCatalogService,
        IAuthService authService,
        INavigationService navigationService,
        IRealtimeNotificationService realtimeService,
        INavBadgeService navBadgeService,
        ILocationTreeCache locationTreeCache)
    {
        _maintenanceService = maintenanceService;
        _assetRepository = assetRepository;
        _repairDepartmentCatalogService = repairDepartmentCatalogService;
        _technicianCatalogService = technicianCatalogService;
        _authService = authService;
        _navigationService = navigationService;
        _realtimeService = realtimeService;
        _navBadgeService = navBadgeService;
        _locationTreeCache = locationTreeCache;

        _timelineScale = new ScheduleTimelineScale(ScheduleZoomPreset.Month, DateOnly.FromDateTime(DateTime.Today));
        RestorePrefs();
        SyncTimelineFromScale();

        MaintenanceTypeFilterOptions.Add(new MaintenanceScheduleTypeFilterOption
        {
            MaintenanceType = null,
            Label = FilterAllLabel,
        });
        foreach (var type in MaintenanceTypeLabels.All)
        {
            MaintenanceTypeFilterOptions.Add(new MaintenanceScheduleTypeFilterOption
            {
                MaintenanceType = type,
                Label = MaintenanceTypeLabels.Get(type),
            });
        }

        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = null,
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_All"),
        });
        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = "scheduled",
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_Scheduled"),
        });
        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = "overdue",
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_Overdue"),
        });
        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = "in_progress",
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_InProgress"),
        });
        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = "completed",
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_Completed"),
        });
        StatusFilterOptions.Add(new MaintenanceScheduleStatusFilterOption
        {
            Status = "cancelled",
            Label = ResourceStrings.Get("MaintenanceSchedule_Tab_Cancelled"),
        });
    }

    public override string PageTitle => ResourceStrings.Get("Nav_MaintenanceSchedule");

    public string ActionCancel => ResourceStrings.Get("MaintenanceSchedule_Action_Cancel");
    public string ActionStartWork => ResourceStrings.Get("MaintenanceSchedule_Action_StartWork");
    public string ActionMaterialRequisition => ResourceStrings.Get("MaintenanceSchedule_Action_MaterialRequisition");
    public string ActionToolRequisition => ResourceStrings.Get("MaintenanceSchedule_Action_ToolRequisition");
    public string ActionMarkOverdue => ResourceStrings.Get("MaintenanceSchedule_Action_MarkOverdue");
    public string ActionDetails => ResourceStrings.Get("MaintenanceSchedule_Action_Details");
    public string ActionNorms => ResourceStrings.Get("MaintenanceSchedule_Action_Norms");
    public string ActionAdd => ResourceStrings.Get("MaintenanceSchedule_Action_Add");
    public string ActionGenerate => ResourceStrings.Get("MaintenanceSchedule_Action_Generate");
    public string ActionCancelAll => ResourceStrings.Get("MaintenanceSchedule_Action_CancelAll");
    public string ActionSubmitWorkReport => ResourceStrings.Get("MaintenanceSchedule_Action_SubmitWorkReport");

    public string EmptyText => ResourceStrings.Get("MaintenanceSchedule_Empty");

    public string FilterAssetLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_Asset");
    public string FilterTypeLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_Type");
    public string FilterDepartmentLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_Department");
    public string FilterStatusLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_Status");
    public string FilterSearchLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_Search");
    public string FilterPlaceholder => ResourceStrings.Get("MaintenanceSchedule_Filter_Search_Placeholder");
    public string SortLabel => ResourceStrings.Get("MaintenanceSchedule_Sort_Label");
    public string FilterAllLabel => ResourceStrings.Get("MaintenanceSchedule_Filter_All");

    public string ViewListLabel => ResourceStrings.Get("MaintenanceSchedule_View_List");

    public string ViewChartLabel => ResourceStrings.Get("MaintenanceSchedule_View_Chart");

    public string ChartStubText => ResourceStrings.Get("MaintenanceSchedule_View_Chart_Stub");

    public IReadOnlyList<string> SortLabels { get; } =
    [
        ResourceStrings.Get("MaintenanceSchedule_Sort_PlannedDateAsc"),
        ResourceStrings.Get("MaintenanceSchedule_Sort_PlannedDateDesc"),
        ResourceStrings.Get("MaintenanceSchedule_Sort_AssetName"),
        ResourceStrings.Get("MaintenanceSchedule_Sort_AssetNumber"),
        ResourceStrings.Get("MaintenanceSchedule_Sort_Status"),
        ResourceStrings.Get("MaintenanceSchedule_Sort_MaintenanceType"),
    ];

    public ObservableCollection<MaintenanceScheduleStatusFilterOption> StatusFilterOptions { get; } = [];

    public ObservableCollection<MaintenanceScheduleAssetFilterOption> AssetFilterOptions { get; } = [];

    public ObservableCollection<MaintenanceScheduleTypeFilterOption> MaintenanceTypeFilterOptions { get; } = [];

    public ObservableCollection<MaintenanceScheduleDepartmentFilterOption> DepartmentFilterOptions { get; } = [];

    public bool IsAdmin => _authService.CurrentProfile?.Role == UserRole.Admin;

    public bool CanCreate =>
        _authService.CurrentProfile?.Role is UserRole.Admin or UserRole.Dispatcher;

    public ObservableCollection<MaintenanceScheduleRow> Rows { get; } = [];

    [ObservableProperty]
    public partial int SelectedStatusFilterIndex { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChartPanelList))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasRows { get; set; }

    [ObservableProperty]
    public partial bool IsActionBusy { get; set; }

    [ObservableProperty]
    public partial int SelectedAssetFilterIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedMaintenanceTypeFilterIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedDepartmentFilterIndex { get; set; }

    [ObservableProperty]
    public partial int SelectedSortIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    [NotifyPropertyChangedFor(nameof(IsChartView))]
    [NotifyPropertyChangedFor(nameof(ShowChartPanelList))]
    public partial int SelectedViewIndex { get; set; }

    public bool IsListView => SelectedViewIndex == (int)MaintenanceScheduleViewMode.List;

    public bool IsChartView => SelectedViewIndex == (int)MaintenanceScheduleViewMode.Chart;

    public bool ShowEmpty => !IsLoading && !HasRows;

    partial void OnSelectedStatusFilterIndexChanged(int value) => ApplyFiltersAndSort();

    partial void OnFilterTextChanged(string value) => ApplyFiltersAndSort();

    partial void OnSelectedAssetFilterIndexChanged(int value) => ApplyFiltersAndSort();

    partial void OnSelectedMaintenanceTypeFilterIndexChanged(int value) => ApplyFiltersAndSort();

    partial void OnSelectedDepartmentFilterIndexChanged(int value) => ApplyFiltersAndSort();

    partial void OnSelectedSortIndexChanged(int value) => ApplyFiltersAndSort();

    public async Task OnPageLoadedAsync()
    {
        SubscribeRealtime();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CancelAsync(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanCancel || IsActionBusy)
            return;

        var confirmed = await AppDialogHelper.ConfirmAsync(
            ResourceStrings.Get("MaintenanceSchedule_Confirm_Cancel_Title"),
            ResourceStrings.Get("MaintenanceSchedule_Confirm_Cancel_Message"),
            App.MainWindow);

        if (!confirmed)
            return;

        IsActionBusy = true;
        try
        {
            var ok = await _maintenanceService.CancelScheduleItemAsync(row.Id);
            if (!ok)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartWorkAsync(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanStartWork || IsActionBusy)
            return;

        IsActionBusy = true;
        try
        {
            var ok = await _maintenanceService.StartScheduleWorkAsync(row.Id);
            if (!ok)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_StartWork"), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_StartWork"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private void OpenMaterialRequisition(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanCreateMaterialRequisition)
            return;

        _navigationService.NavigateTo(
            "MaterialRequisition",
            new MaterialRequisitionNavigationArgs { ScheduleId = row.Id });
    }

    [RelayCommand]
    private void OpenToolRequisition(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanCreateToolRequisition)
            return;

        _navigationService.NavigateTo(
            "ToolRequisition",
            new ToolRequisitionNavigationArgs { ScheduleId = row.Id });
    }

    [RelayCommand]
    private async Task MarkOverdueAsync(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanMarkOverdue || IsActionBusy)
            return;

        var confirmed = await AppDialogHelper.ConfirmAsync(
            ResourceStrings.Get("MaintenanceSchedule_Confirm_Overdue_Title"),
            ResourceStrings.Get("MaintenanceSchedule_Confirm_Overdue_Message"),
            App.MainWindow);

        if (!confirmed)
            return;

        IsActionBusy = true;
        try
        {
            var ok = await _maintenanceService.MarkScheduleOverdueAsync(row.Id);
            if (!ok)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private void OpenNorms(MaintenanceScheduleRow? row)
    {
        if (row is null)
            return;

        _navigationService.NavigateTo(
            "MaintenanceNorms",
            new MaintenanceNormsNavigationArgs(AssetId: row.AssetId));
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(MaintenanceScheduleRow? row)
    {
        if (row is null || !CanCreate || IsActionBusy)
            return;

        IsActionBusy = true;
        try
        {
            var result = await _maintenanceService.GetAssetNormsDetailAsync(row.AssetId);
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Details_LoadError"), InfoBarSeverity.Error);
                return;
            }

            var slot = result.Value!.Slots.FirstOrDefault(s =>
                string.Equals(s.MaintenanceType, row.MaintenanceType, StringComparison.Ordinal));

            var reports = await _maintenanceService.GetWorkReportsForScheduleAsync(row.Id);
            var dialog = BuildDetailsDialog(row, slot, reports);
            await dialog.ShowAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Details_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private async Task SubmitWorkReportAsync(MaintenanceScheduleRow? row)
    {
        if (row is null || !row.CanSubmitWorkReport || IsActionBusy)
            return;

        var profile = _authService.CurrentProfile;
        if (profile is null)
            return;

        var ownDepartmentId = profile.RepairDepartmentId;
        var pendingDeptIds = row.DepartmentIds
            .Where(id => !row.ReportedDepartmentIds.Contains(id))
            .ToList();

        ComboBox? departmentCombo = null;
        Guid? defaultDepartmentId = null;

        if (IsAdmin)
        {
            var deptResult = await _repairDepartmentCatalogService.GetRepairDepartmentsAsync();
            if (!deptResult.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            var pendingDepts = deptResult.Value!
                .Where(d => pendingDeptIds.Contains(d.Id))
                .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (pendingDepts.Count == 0)
                return;

            departmentCombo = new ComboBox
            {
                ItemsSource = pendingDepts,
                DisplayMemberPath = "Name",
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            defaultDepartmentId = pendingDepts[0].Id;
        }
        else
        {
            defaultDepartmentId = ownDepartmentId;
        }

        async Task<IReadOnlyList<BAAZ.CMMS.Core.Models.TechnicianListItem>> LoadTechniciansAsync(Guid deptId)
        {
            var techResult = await _technicianCatalogService.GetTechniciansAsync();
            if (!techResult.IsSuccess)
                return [];

            return techResult.Value!
                .Where(t => t.IsActive && t.RepairDepartmentId == deptId)
                .OrderBy(t => t.FullName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        if (defaultDepartmentId is null)
        {
            InfoBanner.Report(ResourceStrings.Get("WorkReport_Error_DepartmentRequired"), InfoBarSeverity.Warning);
            return;
        }

        var technicians = await LoadTechniciansAsync(defaultDepartmentId.Value);
        var technicianCombo = new ComboBox
        {
            ItemsSource = technicians,
            DisplayMemberPath = "FullName",
            PlaceholderText = ResourceStrings.Get("Common_SelectTechnician"),
            SelectedIndex = technicians.Count > 0 ? 0 : -1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        if (departmentCombo is not null)
        {
            departmentCombo.SelectionChanged += async (_, _) =>
            {
                if (departmentCombo.SelectedItem is BAAZ.CMMS.Core.Models.RepairDepartmentListItem dept)
                {
                    var deptTechnicians = await LoadTechniciansAsync(dept.Id);
                    technicianCombo.ItemsSource = deptTechnicians;
                    technicianCombo.SelectedIndex = deptTechnicians.Count > 0 ? 0 : -1;
                }
            };
        }

        var workPerformedBox = new TextBox
        {
            Header = ResourceStrings.Get("RequestDetail_WorkPerformed_Label"),
            PlaceholderText = ResourceStrings.Get("RequestDetail_WorkPerformed_Placeholder"),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 80,
        };
        var durationBox = new NumberBox
        {
            Header = ResourceStrings.Get("RequestDetail_Duration_Label"),
            MinWidth = 120,
            MaxWidth = 160,
            HorizontalAlignment = HorizontalAlignment.Left,
            Minimum = 0,
            SmallChange = 1,
            LargeChange = 5,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Value = 1,
        };
        var partsBox = new TextBox
        {
            Header = ResourceStrings.Get("RequestDetail_PartsUsed_Label"),
            PlaceholderText = ResourceStrings.Get("RequestDetail_PartsUsed_Placeholder"),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 70,
        };
        var defectsBox = new TextBox
        {
            Header = ResourceStrings.Get("RequestDetail_DefectsFound_Label"),
            PlaceholderText = ResourceStrings.Get("RequestDetail_DefectsFound_Placeholder"),
        };
        var notesBox = new TextBox
        {
            Header = ResourceStrings.Get("RequestDetail_Notes_Label"),
            PlaceholderText = ResourceStrings.Get("RequestDetail_Notes_Placeholder"),
        };

        var validationBar = new InfoBar
        {
            IsOpen = false,
            IsClosable = false,
            Severity = InfoBarSeverity.Error,
        };

        var stack = new StackPanel { Spacing = 12, MinWidth = 360 };
        stack.Children.Add(validationBar);
        stack.Children.Add(new TextBlock
        {
            Text = $"{row.AssetName} ({row.AssetNumber}) • {row.MaintenanceTypeLabel}",
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        if (departmentCombo is not null)
        {
            stack.Children.Add(new TextBlock
            {
                Text = ResourceStrings.Get("MaintenanceSchedule_Create_Department_Label"),
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            });
            stack.Children.Add(departmentCombo);
        }

        stack.Children.Add(new TextBlock
        {
            Text = ResourceStrings.Get("RequestDetail_Action_AssignTechnician"),
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
        });
        stack.Children.Add(technicianCombo);
        stack.Children.Add(workPerformedBox);
        stack.Children.Add(durationBox);
        stack.Children.Add(partsBox);
        stack.Children.Add(defectsBox);
        stack.Children.Add(notesBox);

        void ClearDialogError() => validationBar.IsOpen = false;

        void ShowDialogError(string message)
        {
            validationBar.Message = message;
            validationBar.Severity = InfoBarSeverity.Error;
            validationBar.IsOpen = true;
        }

        void WireClearOnChange(TextBox box) => box.TextChanged += (_, _) => ClearDialogError();
        WireClearOnChange(workPerformedBox);
        WireClearOnChange(partsBox);
        WireClearOnChange(defectsBox);
        WireClearOnChange(notesBox);
        durationBox.ValueChanged += (_, _) => ClearDialogError();
        technicianCombo.SelectionChanged += (_, _) => ClearDialogError();
        if (departmentCombo is not null)
            departmentCombo.SelectionChanged += (_, _) => ClearDialogError();

        var submitSucceeded = false;
        var dialog = new ContentDialog
        {
            Title = ResourceStrings.Get("MaintenanceSchedule_SubmitWorkReport_Title"),
            Content = stack,
            PrimaryButtonText = ResourceStrings.Get("RequestDetail_Action_SubmitWorkReport"),
            CloseButtonText = ResourceStrings.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content.XamlRoot,
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                var technician = technicianCombo.SelectedItem as BAAZ.CMMS.Core.Models.TechnicianListItem;
                var department = departmentCombo?.SelectedItem as BAAZ.CMMS.Core.Models.RepairDepartmentListItem;

                var validationError = WorkReportFormValidation.Validate(
                    technicianCombo.SelectedItem is BAAZ.CMMS.Core.Models.TechnicianListItem,
                    workPerformedBox.Text,
                    durationBox.Value,
                    department,
                    departmentRequired: IsAdmin);
                if (validationError is not null)
                {
                    ShowDialogError(validationError);
                    args.Cancel = true;
                    return;
                }

                var duration = (decimal)durationBox.Value;

                Guid? selectedDepartmentId = IsAdmin ? department?.Id : ownDepartmentId;

                IsActionBusy = true;
                var createResult = await _maintenanceService.CreateWorkReportAsync(new WorkReportInput
                {
                    ScheduleId = row.Id,
                    RepairDepartmentId = selectedDepartmentId,
                    TechnicianId = technician!.Id,
                    WorkPerformed = workPerformedBox.Text,
                    ActualDurationHours = duration,
                    PartsUsed = string.IsNullOrWhiteSpace(partsBox.Text) ? null : partsBox.Text,
                    DefectsFound = string.IsNullOrWhiteSpace(defectsBox.Text) ? null : defectsBox.Text,
                    Notes = string.IsNullOrWhiteSpace(notesBox.Text) ? null : notesBox.Text,
                });

                if (!createResult.IsSuccess)
                {
                    ShowDialogError(WorkReportFormValidation.ResolveError(
                        createResult.Error?.MessageKey,
                        createResult.Error?.Detail,
                        "MaintenanceSchedule_Error_Action"));
                    args.Cancel = true;
                    return;
                }

                submitSucceeded = true;
            }
            catch
            {
                ShowDialogError(ResourceStrings.Get("MaintenanceSchedule_Error_Action"));
                args.Cancel = true;
            }
            finally
            {
                IsActionBusy = false;
                deferral.Complete();
            }
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary || !submitSucceeded)
            return;

        InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_SubmitWorkReport_Success"), InfoBarSeverity.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!CanCreate || IsActionBusy)
            return;

        var assetsResult = await _assetRepository.ListAsync(includeDecommissioned: false);
        if (!assetsResult.IsSuccess || assetsResult.Value!.Count == 0)
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_NoAssets"), InfoBarSeverity.Warning);
            return;
        }

        var assets = assetsResult.Value!
            .OrderBy(a => a.AssetNumber, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var assetCombo = new ComboBox
        {
            ItemsSource = assets,
            DisplayMemberPath = "AssetNumber",
            SelectedIndex = 0,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };

        var typePicker = new MaintenanceTypeCardPicker
        {
            Items = MaintenanceTypePickerItemsBuilder.BuildDefault(),
            SelectedKey = MaintenanceTypeLabels.All[0],
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };

        assetCombo.SelectionChanged += async (_, _) =>
        {
            if (assetCombo.SelectedItem is BAAZ.CMMS.Core.Data.Models.AssetModel selectedAsset)
                await UpdateCreateDialogTypePickerAsync(typePicker, selectedAsset.Id);
        };

        if (assets[0] is { } firstAsset)
            await UpdateCreateDialogTypePickerAsync(typePicker, firstAsset.Id);

        var datePicker = new CalendarDatePicker
        {
            Date = DateTimeOffset.Now.Date.AddDays(7),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
        };

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = ResourceStrings.Get("MaintenanceSchedule_Create_Asset"),
            Style = Microsoft.UI.Xaml.Application.Current.Resources["CaptionTextBlockStyle"] as Microsoft.UI.Xaml.Style,
        });
        stack.Children.Add(assetCombo);
        stack.Children.Add(new TextBlock
        {
            Text = ResourceStrings.Get("MaintenanceSchedule_Create_Type"),
            Style = Microsoft.UI.Xaml.Application.Current.Resources["CaptionTextBlockStyle"] as Microsoft.UI.Xaml.Style,
        });
        stack.Children.Add(typePicker);
        stack.Children.Add(new TextBlock
        {
            Text = ResourceStrings.Get("MaintenanceSchedule_Create_Date"),
            Style = Microsoft.UI.Xaml.Application.Current.Resources["CaptionTextBlockStyle"] as Microsoft.UI.Xaml.Style,
        });
        stack.Children.Add(datePicker);

        List<(CheckBox Box, Guid Id)>? departmentCheckboxes = null;
        if (IsAdmin)
        {
            var deptResult = await _repairDepartmentCatalogService.GetRepairDepartmentsAsync();
            if (!deptResult.IsSuccess || deptResult.Value!.Count == 0)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_NoDepartments"), InfoBarSeverity.Warning);
                return;
            }

            departmentCheckboxes = [];
            var deptPanel = new StackPanel { Spacing = 4 };
            foreach (var dept in deptResult.Value!.OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var checkBox = new CheckBox { Content = dept.Name };
                departmentCheckboxes.Add((checkBox, dept.Id));
                deptPanel.Children.Add(checkBox);
            }

            stack.Children.Add(new TextBlock
            {
                Text = ResourceStrings.Get("MaintenanceSchedule_Create_Department_Label"),
                Style = Microsoft.UI.Xaml.Application.Current.Resources["CaptionTextBlockStyle"] as Microsoft.UI.Xaml.Style,
            });
            stack.Children.Add(deptPanel);
        }

        var dialog = new ContentDialog
        {
            Title = ResourceStrings.Get("MaintenanceSchedule_Create_Title"),
            Content = stack,
            PrimaryButtonText = ResourceStrings.Get("Common_Ok"),
            CloseButtonText = ResourceStrings.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (assetCombo.SelectedItem is not BAAZ.CMMS.Core.Data.Models.AssetModel asset
            || string.IsNullOrWhiteSpace(typePicker.SelectedKey)
            || datePicker.Date is null)
        {
            return;
        }

        var maintenanceType = typePicker.SelectedKey;
        var plannedDate = DateOnly.FromDateTime(datePicker.Date.Value.DateTime);

        IReadOnlyList<Guid>? selectedDepartmentIds = null;
        if (IsAdmin)
        {
            selectedDepartmentIds = departmentCheckboxes!
                .Where(x => x.Box.IsChecked == true)
                .Select(x => x.Id)
                .ToList();

            if (selectedDepartmentIds.Count == 0)
            {
                InfoBanner.Report(
                    ResourceStrings.Get("MaintenanceSchedule_Error_DepartmentRequired"),
                    InfoBarSeverity.Warning);
                return;
            }
        }

        IsActionBusy = true;
        try
        {
            var createResult = await _maintenanceService.CreateScheduleEntryAsync(new CreateScheduleInput
            {
                AssetId = asset.Id,
                MaintenanceType = maintenanceType,
                PlannedDate = plannedDate,
                DepartmentIds = selectedDepartmentIds,
            });

            if (!createResult.IsSuccess)
            {
                InfoBanner.Report(
                    ResolveCreateError(createResult.Error?.MessageKey, createResult.Error?.Detail),
                    InfoBarSeverity.Error);
                return;
            }

            InfoBanner.Report(string.Empty);
            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private async Task GeneratePprAsync()
    {
        if (!IsAdmin || IsActionBusy)
            return;

        IsActionBusy = true;
        InfoBanner.Report(string.Empty);
        _realtimeReloadSuppressCount++;
        try
        {
            var result = await _maintenanceService.GeneratePprScheduleAsync();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Generate"), InfoBarSeverity.Error);
                return;
            }

            InfoBanner.Report(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("MaintenanceSchedule_Generate_Success"),
                    result.Value),
                InfoBarSeverity.Success);

            await LoadAsync(markOverdue: false);
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Generate"), InfoBarSeverity.Error);
        }
        finally
        {
            _realtimeReloadSuppressCount--;
            IsActionBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAllOpenAsync()
    {
        if (!IsAdmin || IsActionBusy)
            return;

        IsActionBusy = true;
        InfoBanner.Report(string.Empty);
        try
        {
            var items = await _maintenanceService.GetScheduleAsync();
            var openCount = items.Count(i => i.Status is "scheduled" or "overdue" or "in_progress");
            if (openCount == 0)
            {
                InfoBanner.Report(
                    ResourceStrings.Get("MaintenanceSchedule_CancelAll_None"),
                    InfoBarSeverity.Informational);
                return;
            }

            var confirmed = await AppDialogHelper.ConfirmAsync(
                ResourceStrings.Get("MaintenanceSchedule_Confirm_CancelAll_Title"),
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("MaintenanceSchedule_Confirm_CancelAll_Message"),
                    openCount),
                App.MainWindow);

            if (!confirmed)
                return;

            var result = await _maintenanceService.CancelAllOpenScheduleItemsAsync();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
                return;
            }

            InfoBanner.Report(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("MaintenanceSchedule_CancelAll_Success"),
                    result.Value),
                InfoBarSeverity.Success);

            await LoadAsync();
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("MaintenanceSchedule_Error_Action"), InfoBarSeverity.Error);
        }
        finally
        {
            IsActionBusy = false;
        }
    }

    private async Task LoadAsync(bool markOverdue = true)
    {
        await RunDataRefreshAsync(async cancellationToken =>
        {
            _allItems = await _maintenanceService.GetScheduleAsync(cancellationToken, markOverdue);
            RebuildAssetFilterOptions();
            RebuildDepartmentFilterOptions();
            ApplyFiltersAndSort(rebuildChart: false);
            SyncNavBadgeFromSchedule();
            OnPropertyChanged(nameof(ShowEmpty));

            if (IsChartView)
                await RebuildChartStateAsync();
        });
    }

    private void SyncNavBadgeFromSchedule()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var count = MaintenanceScheduleNavBadgeCount.ComputeFromItems(_allItems, today);
        _navBadgeService.SetCount(NavItemIds.DispatcherMaintenanceSchedule, count);
    }

    private void RebuildAssetFilterOptions()
    {
        var previousAssetId = GetSelectedAssetId();
        AssetFilterOptions.Clear();
        AssetFilterOptions.Add(new MaintenanceScheduleAssetFilterOption
        {
            AssetId = null,
            Label = FilterAllLabel,
        });

        foreach (var group in _allItems
                     .GroupBy(i => i.AssetId)
                     .OrderBy(g => g.First().AssetNumber, StringComparer.CurrentCultureIgnoreCase))
        {
            var first = group.First();
            AssetFilterOptions.Add(new MaintenanceScheduleAssetFilterOption
            {
                AssetId = first.AssetId,
                Label = $"{first.AssetNumber} — {first.AssetName}",
            });
        }

        SelectedAssetFilterIndex = FindAssetFilterIndex(previousAssetId);
    }

    private void RebuildDepartmentFilterOptions()
    {
        var previousDepartmentId = GetSelectedDepartmentId();
        DepartmentFilterOptions.Clear();

        if (!IsAdmin)
        {
            SelectedDepartmentFilterIndex = 0;
            return;
        }

        DepartmentFilterOptions.Add(new MaintenanceScheduleDepartmentFilterOption
        {
            DepartmentId = null,
            Label = FilterAllLabel,
        });

        var departments = new Dictionary<Guid, string>();
        foreach (var item in _allItems)
        {
            for (var i = 0; i < item.DepartmentIds.Count; i++)
            {
                var deptId = item.DepartmentIds[i];
                if (departments.ContainsKey(deptId))
                    continue;

                var name = i < item.DepartmentNames.Count
                    ? item.DepartmentNames[i]
                    : deptId.ToString();
                departments[deptId] = name;
            }
        }

        foreach (var pair in departments.OrderBy(p => p.Value, StringComparer.CurrentCultureIgnoreCase))
        {
            DepartmentFilterOptions.Add(new MaintenanceScheduleDepartmentFilterOption
            {
                DepartmentId = pair.Key,
                Label = pair.Value,
            });
        }

        SelectedDepartmentFilterIndex = FindDepartmentFilterIndex(previousDepartmentId);
    }

    private void ApplyFiltersAndSort(bool rebuildChart = true)
    {
        IEnumerable<MaintenanceScheduleItem> filtered = _allItems;

        if (GetSelectedStatus() is { } status)
            filtered = filtered.Where(i => string.Equals(i.Status, status, StringComparison.Ordinal));

        var needle = FilterText?.Trim();
        if (!string.IsNullOrWhiteSpace(needle))
        {
            filtered = filtered.Where(i =>
                Contains(i.AssetName, needle)
                || Contains(i.AssetNumber, needle));
        }

        if (GetSelectedAssetId() is Guid assetId)
            filtered = filtered.Where(i => i.AssetId == assetId);

        if (GetSelectedMaintenanceType() is { } maintenanceType)
        {
            filtered = filtered.Where(i =>
                string.Equals(i.MaintenanceType, maintenanceType, StringComparison.Ordinal));
        }

        if (IsAdmin && GetSelectedDepartmentId() is Guid departmentId)
        {
            filtered = filtered.Where(i => i.DepartmentIds.Contains(departmentId));
        }

        filtered = ApplySort(filtered);

        Rows.Clear();
        var ownDepartmentId = _authService.CurrentProfile?.RepairDepartmentId;
        foreach (var item in filtered)
            Rows.Add(MaintenanceScheduleRow.FromItem(item, this, ownDepartmentId, IsAdmin));

        HasRows = Rows.Count > 0;
        OnPropertyChanged(nameof(ShowEmpty));
        if (rebuildChart && IsChartView && !IsLoading)
            _ = RebuildChartStateAsync();
    }

    private IEnumerable<MaintenanceScheduleItem> ApplySort(IEnumerable<MaintenanceScheduleItem> source)
    {
        var sortOption = SelectedSortIndex switch
        {
            1 => MaintenanceScheduleSortOption.PlannedDateDesc,
            2 => MaintenanceScheduleSortOption.AssetName,
            3 => MaintenanceScheduleSortOption.AssetNumber,
            4 => MaintenanceScheduleSortOption.Status,
            5 => MaintenanceScheduleSortOption.MaintenanceType,
            _ => MaintenanceScheduleSortOption.PlannedDateAsc,
        };

        return sortOption switch
        {
            MaintenanceScheduleSortOption.PlannedDateDesc =>
                source.OrderByDescending(i => i.PlannedDate),
            MaintenanceScheduleSortOption.AssetName =>
                source.OrderBy(i => i.AssetName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(i => i.PlannedDate),
            MaintenanceScheduleSortOption.AssetNumber =>
                source.OrderBy(i => i.AssetNumber, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(i => i.PlannedDate),
            MaintenanceScheduleSortOption.Status =>
                source.OrderBy(i => StatusSortKey(i.Status))
                    .ThenBy(i => i.PlannedDate),
            MaintenanceScheduleSortOption.MaintenanceType =>
                source.OrderBy(i => MaintenanceTypeSortKey(i.MaintenanceType))
                    .ThenBy(i => i.PlannedDate),
            _ => source.OrderBy(i => i.PlannedDate),
        };
    }

    private Guid? GetSelectedAssetId() =>
        SelectedAssetFilterIndex > 0 && SelectedAssetFilterIndex < AssetFilterOptions.Count
            ? AssetFilterOptions[SelectedAssetFilterIndex].AssetId
            : null;

    private string? GetSelectedMaintenanceType() =>
        SelectedMaintenanceTypeFilterIndex > 0
        && SelectedMaintenanceTypeFilterIndex < MaintenanceTypeFilterOptions.Count
            ? MaintenanceTypeFilterOptions[SelectedMaintenanceTypeFilterIndex].MaintenanceType
            : null;

    private string? GetSelectedStatus() =>
        SelectedStatusFilterIndex > 0 && SelectedStatusFilterIndex < StatusFilterOptions.Count
            ? StatusFilterOptions[SelectedStatusFilterIndex].Status
            : null;

    private Guid? GetSelectedDepartmentId() =>
        SelectedDepartmentFilterIndex > 0 && SelectedDepartmentFilterIndex < DepartmentFilterOptions.Count
            ? DepartmentFilterOptions[SelectedDepartmentFilterIndex].DepartmentId
            : null;

    private int FindAssetFilterIndex(Guid? assetId)
    {
        for (var i = 0; i < AssetFilterOptions.Count; i++)
        {
            if (AssetFilterOptions[i].AssetId == assetId)
                return i;
        }

        return 0;
    }

    private int FindDepartmentFilterIndex(Guid? departmentId)
    {
        for (var i = 0; i < DepartmentFilterOptions.Count; i++)
        {
            if (DepartmentFilterOptions[i].DepartmentId == departmentId)
                return i;
        }

        return 0;
    }

    private static int StatusSortKey(string status) => status switch
    {
        "overdue" => 0,
        "scheduled" => 1,
        "completed" => 2,
        "cancelled" => 3,
        _ => 99,
    };

    private static bool Contains(string? haystack, string needle) =>
        haystack?.Contains(needle, StringComparison.CurrentCultureIgnoreCase) == true;

    private static int MaintenanceTypeSortKey(string type)
    {
        for (var i = 0; i < MaintenanceTypeLabels.All.Count; i++)
        {
            if (string.Equals(MaintenanceTypeLabels.All[i], type, StringComparison.Ordinal))
                return i;
        }

        return 99;
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

    private async Task UpdateCreateDialogTypePickerAsync(MaintenanceTypeCardPicker picker, Guid assetId)
    {
        var previousKey = picker.SelectedKey;
        var result = await _maintenanceService.GetAssetNormsDetailAsync(assetId);

        picker.Items = result.IsSuccess
            ? MaintenanceTypePickerItemsBuilder.BuildFromSlots(result.Value!.Slots)
            : MaintenanceTypePickerItemsBuilder.BuildDefault();

        var validKeys = MaintenanceTypeLabels.All;
        picker.SelectedKey = validKeys.Contains(previousKey, StringComparer.Ordinal)
            ? previousKey
            : validKeys[0];
    }

    private static string ResolveCreateError(string? messageKey, string? fallback)
    {
        var businessMessage = TryResolveBusinessRuleError(fallback);
        if (businessMessage is not null)
            return businessMessage;

        if (!string.IsNullOrWhiteSpace(messageKey))
        {
            var localized = ResourceStrings.Get(messageKey);
            if (!string.Equals(localized, messageKey, StringComparison.Ordinal))
                return localized;
        }

        businessMessage = TryResolveBusinessRuleError(messageKey);
        if (businessMessage is not null)
            return businessMessage;

        return ResourceStrings.Get("MaintenanceSchedule_Error_Action");
    }

    private static string? TryResolveBusinessRuleError(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (text.Contains("PENDING_SCHEDULE_EXISTS", StringComparison.OrdinalIgnoreCase))
            return ResourceStrings.Get("MaintenanceSchedule_Error_PendingExists");

        if (text.Contains("ASSET_NOT_AVAILABLE", StringComparison.OrdinalIgnoreCase))
            return ResourceStrings.Get("MaintenanceSchedule_Error_AssetUnavailable");

        return null;
    }

    private static ContentDialog BuildDetailsDialog(
        MaintenanceScheduleRow row,
        EffectiveNormSlot? slot,
        IReadOnlyList<WorkReportItem> reports)
    {
        var content = new StackPanel { Spacing = 12, MinWidth = 360 };
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Asset"),
            $"{row.AssetName} ({row.AssetNumber})"));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Type"),
            row.MaintenanceTypeLabel));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_PlannedDate"),
            row.PlannedDateText));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Status"),
            row.StatusLabel));
        if (row.ReportsProgressText is not null)
        {
            content.Children.Add(CreateDetailRow(
                ResourceStrings.Get("MaintenanceSchedule_Details_ReportsProgress"),
                row.ReportsProgressText));
        }
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Interval"),
            FormatDetailsInterval(slot)));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Source"),
            FormatDetailsSource(slot)));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Departments"),
            row.DepartmentsText));
        content.Children.Add(CreateDetailRow(
            ResourceStrings.Get("MaintenanceSchedule_Details_Description"),
            FormatDetailsDescription(slot),
            multiline: true));

        content.Children.Add(new TextBlock
        {
            Text = ResourceStrings.Get("RequestDetail_Section_WorkReports"),
            Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
            Margin = new Thickness(0, 8, 0, 0),
        });

        if (reports.Count == 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = ResourceStrings.Get("RequestDetail_WorkReports_Empty"),
                Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            });
        }
        else
        {
            foreach (var report in reports)
            {
                var item = WorkReportDisplayItem.From(report);
                var reportPanel = new StackPanel
                {
                    Spacing = 2,
                    Padding = new Thickness(0, 4, 0, 8),
                };
                reportPanel.Children.Add(new TextBlock
                {
                    Text = $"{item.RepairDepartmentName} • {item.TechnicianName} • {item.DurationText}",
                    Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                });
                reportPanel.Children.Add(new TextBlock
                {
                    Text = item.WorkPerformed,
                    TextWrapping = TextWrapping.WrapWholeWords,
                });
                content.Children.Add(reportPanel);
            }
        }

        return new ContentDialog
        {
            Title = ResourceStrings.Get("MaintenanceSchedule_Details_Title"),
            Content = content,
            CloseButtonText = ResourceStrings.Get("Common_Ok"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow?.Content.XamlRoot,
        };
    }

    private static UIElement CreateDetailRow(string label, string value, bool multiline = false)
    {
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            IsTextSelectionEnabled = multiline,
        });
        return stack;
    }

    private static string FormatDetailsInterval(EffectiveNormSlot? slot)
    {
        if (slot?.EffectiveIntervalDays is int days)
        {
            var formatKey = slot.IsIntervalOverridden
                ? "MaintenanceNorms_Audit_IntervalOverridden"
                : "MaintenanceNorms_Audit_IntervalPreset";
            return string.Format(CultureInfo.CurrentCulture, ResourceStrings.Get(formatKey), days);
        }

        return ResourceStrings.Get("MaintenanceSchedule_Details_Interval_NotSet");
    }

    private static string FormatDetailsDescription(EffectiveNormSlot? slot)
    {
        if (!string.IsNullOrWhiteSpace(slot?.EffectiveDescription))
            return slot.EffectiveDescription;

        return ResourceStrings.Get("MaintenanceSchedule_Details_Description_Empty");
    }

    private static string FormatDetailsSource(EffectiveNormSlot? slot)
    {
        if (slot?.OverrideNormId is not null)
            return ResourceStrings.Get("MaintenanceSchedule_Details_Source_Override");

        if (slot?.PresetIntervalDays is not null || !string.IsNullOrWhiteSpace(slot?.PresetDescription))
            return ResourceStrings.Get("MaintenanceSchedule_Details_Source_Preset");

        return ResourceStrings.Get("Common_None");
    }
}
