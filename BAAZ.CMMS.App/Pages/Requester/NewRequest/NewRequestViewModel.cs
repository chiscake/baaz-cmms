using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using WinUI.UtilsLibrary.Contracts;
using WinUI.UtilsLibrary.Controls.PageHeader;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Requester.NewRequest;

public enum NewRequestSubjectMode
{
    Asset = 0,
    Location = 1,
}

public sealed partial class NewRequestViewModel : PageViewModelBase
{
    private const int MaxAssetSuggestions = 20;

    private static readonly string[] TypeValues = ["breakdown", "service", "inspection"];
    private static readonly string[] PriorityValues = ["low", "normal", "high", "critical"];
    private static readonly (string Value, string ResourceKey)[] RepairZoneOptions =
    [
        ("on_site", "RepairZone_OnSite"),
        ("workshop", "RepairZone_Workshop"),
        ("external", "RepairZone_External"),
    ];

    private readonly IRequesterAssetCatalog _requesterAssetCatalog;
    private readonly IRepairDepartmentCatalogService _repairDepartmentCatalogService;
    private readonly IAuthService _authService;
    private readonly IRequestService _requestService;
    private readonly INavigationService _navigationService;

    private readonly List<AssetPickerRow> _allAssets = [];
    private readonly List<RepairDepartmentPickerRow> _repairDepartments = [];

    private AssetPickerRow? _selectedAsset;
    private bool _syncingAssetText;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _locationDescription = string.Empty;
    private string _assetSearchText = string.Empty;
    private int _selectedTypeIndex;
    private int _selectedPriorityIndex = 1;
    private int _selectedRepairDepartmentIndex = -1;
    private int _selectedRepairZoneIndex;
    private string _contractorNameText = string.Empty;
    private bool _isRepairZoneExternal;
    private bool _isSubmitting;
    private bool _isLoadingAssets;
    private NewRequestSubjectMode _subjectMode = NewRequestSubjectMode.Asset;

    public NewRequestViewModel(
        IRequesterAssetCatalog requesterAssetCatalog,
        IRepairDepartmentCatalogService repairDepartmentCatalogService,
        IAuthService authService,
        IRequestService requestService,
        INavigationService navigationService)
    {
        _requesterAssetCatalog = requesterAssetCatalog;
        _repairDepartmentCatalogService = repairDepartmentCatalogService;
        _authService = authService;
        _requestService = requestService;
        _navigationService = navigationService;
        AssetSuggestions = [];
    }

    public override string PageTitle => ResourceStrings.Get("Nav_NewRequest");

    public string LabelRepairDepartment => ResourceStrings.Get("NewRequest_Label_RepairDepartment");

    public string RepairDepartmentPlaceholder => ResourceStrings.Get("NewRequest_RepairDepartment_Placeholder");

    public string LabelPriority => ResourceStrings.Get("NewRequest_Label_Priority");

    public string LabelType => ResourceStrings.Get("NewRequest_Label_Type");

    public string LabelAsset => ResourceStrings.Get("NewRequest_Label_Asset");

    public string AssetSearchPlaceholder => ResourceStrings.Get("NewRequest_Asset_Placeholder");

    public string LabelTitle => ResourceStrings.Get("NewRequest_Label_Title");

    public string LabelDescription => ResourceStrings.Get("NewRequest_Label_Description");

    public string LabelLocation => ResourceStrings.Get("NewRequest_Label_Location");

    public string LabelSubject => ResourceStrings.Get("NewRequest_Subject_Label");

    public string SubjectModeAssetLabel => ResourceStrings.Get("NewRequest_Label_Asset");

    public string SubjectModeLocationLabel => ResourceStrings.Get("NewRequest_Subject_Location");

    public string LocationPlaceholder => ResourceStrings.Get("NewRequest_Location_Placeholder");

    public string SubmitButtonText => ResourceStrings.Get("NewRequest_Submit");

    public bool IsAdmin => _authService.CurrentProfile?.Role == UserRole.Admin;

    public bool ShowRepairZoneSection => IsAdmin;

    public string LabelRepairZone => ResourceStrings.Get("MyRequests_Detail_RepairZone");

    public string RepairZonePickerPlaceholder => ResourceStrings.Get("Common_SelectRepairZone");

    public string LabelContractorName => ResourceStrings.Get("MyRequests_Detail_ContractorName");

    public string ContractorNamePlaceholder => ResourceStrings.Get("RequestDetail_ContractorName_Placeholder");

    public IReadOnlyList<string> RepairZoneLabels { get; } =
        RepairZoneOptions.Select(o => ResourceStrings.Get(o.ResourceKey)).ToList();

    public bool IsAssetSubjectMode => _subjectMode == NewRequestSubjectMode.Asset;

    public bool IsLocationSubjectMode => _subjectMode == NewRequestSubjectMode.Location;

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

    public IReadOnlyList<string> RepairDepartmentLabels =>
        _repairDepartments.Select(d => d.Name).ToList();

    public ObservableCollection<AssetPickerRow> AssetSuggestions { get; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string LocationDescription
    {
        get => _locationDescription;
        set => SetProperty(ref _locationDescription, value);
    }

    public string AssetSearchText
    {
        get => _assetSearchText;
        set => SetProperty(ref _assetSearchText, value);
    }

    public int SelectedTypeIndex
    {
        get => _selectedTypeIndex;
        set => SetProperty(ref _selectedTypeIndex, value);
    }

    public int SelectedPriorityIndex
    {
        get => _selectedPriorityIndex;
        set => SetProperty(ref _selectedPriorityIndex, value);
    }

    public int SelectedRepairDepartmentIndex
    {
        get => _selectedRepairDepartmentIndex;
        set => SetProperty(ref _selectedRepairDepartmentIndex, value);
    }

    public int SelectedRepairZoneIndex
    {
        get => _selectedRepairZoneIndex;
        set
        {
            if (SetProperty(ref _selectedRepairZoneIndex, value))
            {
                IsRepairZoneExternal = value >= 0
                    && value < RepairZoneOptions.Length
                    && RepairZoneOptions[value].Value == "external";
            }
        }
    }

    public string ContractorNameText
    {
        get => _contractorNameText;
        set => SetProperty(ref _contractorNameText, value);
    }

    public bool IsRepairZoneExternal
    {
        get => _isRepairZoneExternal;
        private set => SetProperty(ref _isRepairZoneExternal, value);
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        private set => SetProperty(ref _isSubmitting, value);
    }

    public bool IsLoadingAssets
    {
        get => _isLoadingAssets;
        private set => SetProperty(ref _isLoadingAssets, value);
    }

    public string? SelectedAssetLocationPath => _selectedAsset?.LocationPath;

    public async Task LoadAsync()
    {
        if (IsLoadingAssets || _allAssets.Count > 0)
        {
            return;
        }

        await LoadAssetsCoreAsync();
    }

    public async Task InitializeAsync(object? navigationParameter)
    {
        await LoadAssetsCoreAsync();
        await LoadRepairDepartmentsAsync();

        if (navigationParameter is NewRequestNavigationArgs { PreselectedAssetId: Guid assetId })
        {
            SetSubjectMode(NewRequestSubjectMode.Asset);
            var row = _allAssets.FirstOrDefault(a => a.Id == assetId);
            if (row is not null)
            {
                SelectAssetSuggestion(row);
            }
        }
    }

    private async Task LoadAssetsCoreAsync()
    {
        if (IsLoadingAssets || _allAssets.Count > 0)
        {
            return;
        }

        IsLoadingAssets = true;
        InfoBanner.Report(string.Empty);

        try
        {
            var result = await _requesterAssetCatalog.GetActiveScopedAssetsAsync();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
                return;
            }

            _allAssets.Clear();
            foreach (var item in result.Value!)
            {
                var asset = item.Asset;
                var locationSuffix = string.IsNullOrWhiteSpace(asset.LocationName)
                    ? string.Empty
                    : $" — {asset.LocationName}";

                _allAssets.Add(new AssetPickerRow
                {
                    Id = asset.Id,
                    DisplayName = $"{asset.AssetNumber} — {asset.Name}{locationSuffix}",
                    LocationName = asset.LocationName,
                    LocationPath = item.LocationFullPath,
                });
            }
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
        finally
        {
            IsLoadingAssets = false;
        }
    }

    private async Task LoadRepairDepartmentsAsync()
    {
        if (_repairDepartments.Count > 0)
            return;

        try
        {
            var result = await _repairDepartmentCatalogService.GetRepairDepartmentsAsync();
            if (!result.IsSuccess)
            {
                InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
                return;
            }

            _repairDepartments.Clear();
            foreach (var department in result.Value!.OrderBy(d => d.Name))
            {
                _repairDepartments.Add(new RepairDepartmentPickerRow
                {
                    Id = department.Id,
                    Name = department.Name,
                });
            }

            OnPropertyChanged(nameof(RepairDepartmentLabels));
            if (_repairDepartments.Count == 1)
                SelectedRepairDepartmentIndex = 0;
        }
        catch
        {
            InfoBanner.Report(ResourceStrings.Get("Common_LoadError"), InfoBarSeverity.Error);
        }
    }

    public void OnAssetSearchTextChanged(string text)
    {
        if (!_syncingAssetText
            && _selectedAsset is not null
            && !string.Equals(text, _selectedAsset.DisplayName, StringComparison.Ordinal))
        {
            _selectedAsset = null;
            OnPropertyChanged(nameof(SelectedAssetLocationPath));
        }

        UpdateAssetSuggestions(text);
    }

    public void SelectAssetSuggestion(AssetPickerRow row)
    {
        _selectedAsset = row;
        _syncingAssetText = true;
        AssetSearchText = row.DisplayName;
        _syncingAssetText = false;
        OnPropertyChanged(nameof(SelectedAssetLocationPath));
    }

    public void SetSubjectMode(NewRequestSubjectMode mode)
    {
        if (_subjectMode == mode)
        {
            return;
        }

        _subjectMode = mode;

        if (mode == NewRequestSubjectMode.Asset)
        {
            LocationDescription = string.Empty;
        }
        else
        {
            ClearAssetSelection();
        }

        OnPropertyChanged(nameof(IsAssetSubjectMode));
        OnPropertyChanged(nameof(IsLocationSubjectMode));
    }

    public bool TrySelectFirstSuggestion()
    {
        if (AssetSuggestions.Count == 0)
        {
            return false;
        }

        SelectAssetSuggestion(AssetSuggestions[0]);
        return true;
    }

    public void ResetForm()
    {
        Title = string.Empty;
        Description = string.Empty;
        LocationDescription = string.Empty;
        SelectedTypeIndex = 0;
        SelectedPriorityIndex = 1;
        SelectedRepairDepartmentIndex = _repairDepartments.Count == 1 ? 0 : -1;
        SelectedRepairZoneIndex = 0;
        ContractorNameText = string.Empty;
        SetSubjectMode(NewRequestSubjectMode.Asset);
        ClearAssetSelection();
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        var profile = _authService.CurrentProfile;
        if (profile is null)
        {
            InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_NotAuthenticated"), InfoBarSeverity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_Validation"), InfoBarSeverity.Warning);
            return;
        }

        Guid? assetId;
        string locationDescription;

        if (IsAssetSubjectMode)
        {
            if (_selectedAsset is null)
            {
                InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_AssetOrLocation"), InfoBarSeverity.Warning);
                return;
            }

            assetId = _selectedAsset.Id;
            locationDescription = string.Empty;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(LocationDescription))
            {
                InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_AssetOrLocation"), InfoBarSeverity.Warning);
                return;
            }

            assetId = null;
            locationDescription = LocationDescription.Trim();
        }

        if (SelectedRepairDepartmentIndex < 0
            || SelectedRepairDepartmentIndex >= _repairDepartments.Count)
        {
            InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_RepairDepartment"), InfoBarSeverity.Warning);
            return;
        }

        var targetDepartmentId = _repairDepartments[SelectedRepairDepartmentIndex].Id;

        string? repairZone = null;
        string? contractorName = null;
        if (IsAdmin)
        {
            if (SelectedRepairZoneIndex < 0 || SelectedRepairZoneIndex >= RepairZoneOptions.Length)
            {
                InfoBanner.Report(ResourceStrings.Get("RequestDetail_Error_RepairZoneRequired"), InfoBarSeverity.Warning);
                return;
            }

            repairZone = RepairZoneOptions[SelectedRepairZoneIndex].Value;
            if (repairZone == "external")
            {
                contractorName = ContractorNameText.Trim();
                if (string.IsNullOrWhiteSpace(contractorName))
                {
                    InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_ContractorRequired"), InfoBarSeverity.Warning);
                    return;
                }
            }
        }

        IsSubmitting = true;

        try
        {
            var created = await _requestService.CreateRequestAsync(new CreateRequestInput
            {
                RequesterId = profile.Id,
                Type = TypeValues[SelectedTypeIndex],
                Priority = PriorityValues[SelectedPriorityIndex],
                Title = Title,
                Description = Description.Trim(),
                LocationDescription = locationDescription,
                AssetId = assetId,
                TargetRepairDepartmentId = targetDepartmentId,
                RepairZone = repairZone,
                ContractorName = contractorName,
            });

            if (created is null)
            {
                Debug.WriteLine(
                    "[NewRequest] SubmitAsync: CreateRequestAsync returned null " +
                    $"(requester={profile.Id}, type={TypeValues[SelectedTypeIndex]}, " +
                    $"assetId={assetId}, locationLen={locationDescription.Length})");
                InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_Submit"), InfoBarSeverity.Error);
                return;
            }

            _navigationService.NavigateToReplace(
                "MyRequests",
                new MyRequestsNavigationArgs(created.Id));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NewRequest] SubmitAsync exception: {ex}");
            InfoBanner.Report(ResourceStrings.Get("NewRequest_Error_Submit"), InfoBarSeverity.Error);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void UpdateAssetSuggestions(string? query)
    {
        if (_syncingAssetText)
        {
            return;
        }

        AssetSuggestions.Clear();

        var q = query?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(q))
        {
            return;
        }

        foreach (var asset in _allAssets
                     .Where(a => a.DisplayName.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                     .Take(MaxAssetSuggestions))
        {
            AssetSuggestions.Add(asset);
        }
    }

    private void ClearAssetSelection()
    {
        _selectedAsset = null;
        _syncingAssetText = true;
        AssetSearchText = string.Empty;
        _syncingAssetText = false;
        OnPropertyChanged(nameof(SelectedAssetLocationPath));
        AssetSuggestions.Clear();
    }
}

public sealed class AssetPickerRow
{
    public Guid Id { get; init; }

    public required string DisplayName { get; init; }

    public string? LocationName { get; init; }

    public string? LocationPath { get; init; }

    public override string ToString() => DisplayName;
}

public sealed class RepairDepartmentPickerRow
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public override string ToString() => Name;
}
