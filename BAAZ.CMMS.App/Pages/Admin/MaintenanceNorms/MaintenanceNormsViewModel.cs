using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;

/// <summary>
/// Нормативы ТО (UC-A5): 3 вкладки — «По оборудованию» (индивидуальные override поверх
/// пресета категории), «Категории» (пресеты ТО-1/ТО-2/КР) и «Все нормативы» (аудит).
/// </summary>
public sealed partial class MaintenanceNormsViewModel : PageViewModelBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAssetCatalogService _assetCatalogService;
    private readonly IRepairDepartmentCatalogService _repairDepartmentCatalogService;

    private readonly List<AssetPickerRow> _allAssets = [];
    private readonly List<RepairDepartmentListItem> _repairDepartments = [];

    private bool _isInitialized;
    private bool _isCreatingCategory;
    private Guid? _editingCategoryId;

    public MaintenanceNormsViewModel(
        IMaintenanceService maintenanceService,
        IAssetCatalogService assetCatalogService,
        IRepairDepartmentCatalogService repairDepartmentCatalogService)
    {
        _maintenanceService = maintenanceService;
        _assetCatalogService = assetCatalogService;
        _repairDepartmentCatalogService = repairDepartmentCatalogService;
    }

    public override string PageTitle => ResourceStrings.Get("Nav_MaintenanceNorms");

    private static readonly TimeSpan SavedBannerAutoDismiss = TimeSpan.FromSeconds(5);

    private void ReportSavedSuccess() =>
        InfoBanner.Report(
            ResourceStrings.Get("MaintenanceNorms_Saved"),
            InfoBarSeverity.Success,
            SavedBannerAutoDismiss);

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    public string LabelAssetsTab => ResourceStrings.Get("MaintenanceNorms_Tab_Assets");

    public string LabelCategoriesTab => ResourceStrings.Get("MaintenanceNorms_Tab_Categories");

    public string LabelAuditTab => ResourceStrings.Get("MaintenanceNorms_Tab_Audit");

    // ---------------------------------------------------------------
    // Вкладка «По оборудованию»
    // ---------------------------------------------------------------

    public string AssetSearchPlaceholder => ResourceStrings.Get("MaintenanceNorms_Asset_SearchPlaceholder");

    public string SaveAssetLabel => ResourceStrings.Get("MaintenanceNorms_Save");

    public string SelectFromListHint => ResourceStrings.Get("MaintenanceNorms_SelectFromList");

    public bool ShowAssetEmptySelection => !IsAssetSelected;

    public ObservableCollection<AssetPickerRow> AssetSuggestions { get; } = [];

    [ObservableProperty]
    public partial string AssetSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AssetPickerRow? SelectedAssetRow { get; set; }

    [ObservableProperty]
    public partial bool IsAssetSelected { get; set; }

    partial void OnIsAssetSelectedChanged(bool value) =>
        OnPropertyChanged(nameof(ShowAssetEmptySelection));

    [ObservableProperty]
    public partial string AssetHeaderText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AssetCategoryText { get; set; } = string.Empty;

    public ObservableCollection<AssetSlotEditor> AssetSlots { get; } = [];

    private Guid? _currentAssetId;

    // ---------------------------------------------------------------
    // Вкладка «Категории»
    // ---------------------------------------------------------------

    public ObservableCollection<CategoryRow> Categories { get; } = [];

    [ObservableProperty]
    public partial CategoryRow? SelectedCategoryRow { get; set; }

    [ObservableProperty]
    public partial bool IsCategoryEditorVisible { get; set; }

    public bool ShowCategoryEmptySelection => !IsCategoryEditorVisible;

    partial void OnIsCategoryEditorVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(ShowCategoryEmptySelection));

    [ObservableProperty]
    public partial string CategoryEditorName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryEditorDescription { get; set; } = string.Empty;

    public ObservableCollection<CategorySlotEditor> CategorySlots { get; } = [];

    public string AddCategoryLabel => ResourceStrings.Get("EquipmentCategory_Add");

    public string CategoryNameLabel => ResourceStrings.Get("EquipmentCategory_Name");

    public string CategoryDescriptionLabel => ResourceStrings.Get("EquipmentCategory_Description");

    public string SaveCategoryLabel => ResourceStrings.Get("MaintenanceNorms_Save");

    // ---------------------------------------------------------------
    // Вкладка «Все нормативы»
    // ---------------------------------------------------------------

    public ObservableCollection<AuditAssetGroup> AuditGroups { get; } = [];

    // ---------------------------------------------------------------
    // Инициализация / deep-link
    // ---------------------------------------------------------------

    public async Task InitializeAsync(object? navigationParameter)
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            await LoadRepairDepartmentsAsync();
            await LoadAssetsAsync();
            await LoadCategoriesAsync();
            await LoadAuditAsync();
        }

        if (navigationParameter is MaintenanceNormsNavigationArgs args)
            await ApplyNavigationArgsAsync(args);
    }

    private async Task ApplyNavigationArgsAsync(MaintenanceNormsNavigationArgs args)
    {
        if (args.AssetId is Guid assetId)
        {
            SelectedTabIndex = 0;
            var row = _allAssets.FirstOrDefault(a => a.Id == assetId);
            if (row is not null)
            {
                EnsureAssetVisibleInList(assetId);
                await SelectAssetAsync(row);
            }
        }
        else if (args.CategoryId is Guid categoryId)
        {
            SelectedTabIndex = 1;
            var row = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (row is not null)
                await SelectCategoryAsync(row);
        }
    }

    private async Task LoadRepairDepartmentsAsync()
    {
        var result = await _repairDepartmentCatalogService.GetRepairDepartmentsAsync();
        if (result.IsSuccess)
        {
            _repairDepartments.Clear();
            _repairDepartments.AddRange(result.Value!.OrderBy(d => d.Name));
        }
    }

    // ---------------------------------------------------------------
    // «По оборудованию»
    // ---------------------------------------------------------------

    private async Task LoadAssetsAsync()
    {
        var result = await _assetCatalogService.GetAssetsAdminAsync(includeDecommissioned: true);
        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        _allAssets.Clear();
        foreach (var asset in result.Value!.OrderBy(a => a.AssetNumber))
        {
            var locationSuffix = string.IsNullOrWhiteSpace(asset.LocationName) ? string.Empty : $" — {asset.LocationName}";
            _allAssets.Add(new AssetPickerRow
            {
                Id = asset.Id,
                DisplayName = $"{asset.AssetNumber} — {asset.Name}{locationSuffix}",
            });
        }

        RefreshAssetList();
    }

    partial void OnAssetSearchTextChanged(string value) =>
        RebuildAssetSuggestions(GetAssetListIncludeId());

    public void RefreshAssetList() => RebuildAssetSuggestions(GetAssetListIncludeId());

    /// <summary>Гарантирует, что объект deep-link виден в master-списке (сброс фильтра + вне top-50).</summary>
    public void EnsureAssetVisibleInList(Guid assetId)
    {
        if (!string.IsNullOrWhiteSpace(AssetSearchText))
            AssetSearchText = string.Empty;

        RebuildAssetSuggestions(assetId);
    }

    private Guid? GetAssetListIncludeId() =>
        SelectedAssetRow?.Id ?? _currentAssetId;

    private void RebuildAssetSuggestions(Guid? forceIncludeAssetId = null)
    {
        var query = AssetSearchText?.Trim() ?? string.Empty;

        var source = query.Length == 0
            ? _allAssets.AsEnumerable()
            : _allAssets.Where(a => a.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase));

        var filtered = source.Take(50).ToList();
        var includeId = forceIncludeAssetId ?? GetAssetListIncludeId();
        if (includeId is Guid id && filtered.All(a => a.Id != id))
        {
            var extra = _allAssets.FirstOrDefault(a => a.Id == id);
            if (extra is not null)
                filtered.Add(extra);
        }

        if (AssetSuggestions.Count == filtered.Count &&
            filtered.Zip(AssetSuggestions, (next, current) => next.Id == current.Id).All(equal => equal))
        {
            return;
        }

        AssetSuggestions.Clear();
        foreach (var asset in filtered)
            AssetSuggestions.Add(asset);
    }

    public async Task SelectAssetAsync(AssetPickerRow row)
    {
        _currentAssetId = row.Id;
        SelectedAssetRow = row;

        var result = await _maintenanceService.GetAssetNormsDetailAsync(row.Id);
        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        var detail = result.Value!;
        AssetHeaderText = $"{detail.AssetNumber} — {detail.AssetName}";
        AssetCategoryText = detail.CategoryName is null
            ? ResourceStrings.Get("MaintenanceNorms_Asset_NoCategory")
            : detail.CategoryName;

        AssetSlots.Clear();
        foreach (var slot in detail.Slots)
        {
            var hasOverride = slot.OverrideNormId is not null;
            var editor = new AssetSlotEditor
            {
                MaintenanceType = slot.MaintenanceType,
                Header = MaintenanceTypeLabels.Get(slot.MaintenanceType),
                PresetSummary = BuildPresetSummary(slot),
                HasOverride = hasOverride,
                IsExpanded = hasOverride,
                IntervalDaysText = (slot.OverrideIntervalDays ?? slot.PresetIntervalDays)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Description = slot.OverrideDescription ?? string.Empty,
                StatusSummary = BuildStatusSummary(slot),
                OriginalIntervalDays = slot.OverrideIntervalDays,
                PresetDepartmentIds = slot.PresetDepartmentIds.ToHashSet(),
            };

            var checkedIds = slot.OverrideDepartments ? slot.OverrideDepartmentIds : slot.PresetDepartmentIds;
            foreach (var dept in _repairDepartments)
                editor.Departments.Add(new CheckableItem { Id = dept.Id, Name = dept.Name, IsChecked = checkedIds.Contains(dept.Id) });

            if (slot.PendingSchedule is { } pending)
            {
                editor.IsPendingScheduleVisible = true;
                var statusLabel = MaintenanceTypeLabels.ScheduleStatus(pending.Status);
                editor.PendingScheduleTooltip = string.Format(
                    CultureInfo.CurrentCulture,
                    ResourceStrings.Get("MaintenanceNorms_PendingSchedule_Tooltip"),
                    pending.PlannedDate.ToString("d", CultureInfo.CurrentCulture),
                    statusLabel);
            }

            AssetSlots.Add(editor);
        }

        IsAssetSelected = true;
    }

    private static string BuildPresetSummary(EffectiveNormSlot slot)
    {
        if (slot.PresetIntervalDays is null)
            return ResourceStrings.Get("MaintenanceNorms_Preset_NotSet");

        return string.Format(
            CultureInfo.CurrentCulture,
            ResourceStrings.Get("MaintenanceNorms_Preset_Summary"),
            slot.PresetIntervalDays);
    }

    private static string BuildStatusSummary(EffectiveNormSlot slot)
    {
        var last = slot.LastMaintenanceDate?.ToString("d", CultureInfo.CurrentCulture)
            ?? ResourceStrings.Get("MaintenanceNorms_Status_Never");
        var next = slot.NextMaintenanceDate?.ToString("d", CultureInfo.CurrentCulture)
            ?? "—";

        return string.Format(
            CultureInfo.CurrentCulture,
            ResourceStrings.Get("MaintenanceNorms_Status_Summary"),
            last,
            next);
    }

    [RelayCommand]
    private void ResetSlotToPreset(AssetSlotEditor slot)
    {
        slot.HasOverride = false;
        slot.IntervalDaysText = string.Empty;
        slot.Description = string.Empty;
        slot.ResetDepartmentsToPreset();
    }

    [RelayCommand]
    private async Task SaveAssetNormsAsync()
    {
        if (_currentAssetId is not Guid assetId)
            return;

        var slots = new List<AssetNormSlotInput>();

        foreach (var slot in AssetSlots)
        {
            if (!slot.HasOverride)
            {
                slots.Add(new AssetNormSlotInput { MaintenanceType = slot.MaintenanceType, HasOverride = false });
                continue;
            }

            if (!int.TryParse(slot.IntervalDaysText, out var interval) || interval <= 0)
            {
                InfoBanner.Report(ResourceStrings.Get("MaintenanceNorms_Validation_IntervalRequired"), InfoBarSeverity.Warning);
                return;
            }

            NormChangePolicy? policy = null;
            var intervalChanged = slot.OriginalIntervalDays is not null && slot.OriginalIntervalDays != interval;
            if (slot.IsPendingScheduleVisible && intervalChanged)
            {
                policy = await ShowNormChangePolicyDialogAsync(slot);
                if (policy is null)
                    return; // пользователь отменил save
            }

            slots.Add(new AssetNormSlotInput
            {
                MaintenanceType = slot.MaintenanceType,
                HasOverride = true,
                IntervalDays = interval,
                Description = string.IsNullOrWhiteSpace(slot.Description) ? null : slot.Description.Trim(),
                OverrideDepartments = slot.HasDepartmentOverride,
                DepartmentIds = slot.SelectedDepartmentIds,
                Policy = policy,
            });
        }

        var result = await _maintenanceService.SaveAssetNormOverridesAsync(
            new AssetNormOverridesInput { AssetId = assetId, Slots = slots });

        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        ReportSavedSuccess();
        await SelectAssetAsync(SelectedAssetRow!);
        await LoadAuditAsync();
    }

    private static async Task<NormChangePolicy?> ShowNormChangePolicyDialogAsync(AssetSlotEditor slot)
    {
        var options = new[]
        {
            (Policy: NormChangePolicy.RecalculatePending, Label: ResourceStrings.Get("MaintenanceNorms_Policy_RecalculatePending")),
            (Policy: NormChangePolicy.NextCycleOnly, Label: ResourceStrings.Get("MaintenanceNorms_Policy_NextCycleOnly")),
            (Policy: NormChangePolicy.NormOnly, Label: ResourceStrings.Get("MaintenanceNorms_Policy_NormOnly")),
        };

        var stack = new StackPanel { Spacing = 8 };
        var radios = new List<RadioButton>();
        foreach (var (policy, label) in options)
        {
            var radio = new RadioButton { Content = label, Tag = policy, GroupName = "NormChangePolicy" };
            radios.Add(radio);
            stack.Children.Add(radio);
        }

        radios[0].IsChecked = true;

        var dialog = new ContentDialog
        {
            Title = string.Format(
                CultureInfo.CurrentCulture,
                ResourceStrings.Get("MaintenanceNorms_Policy_Title"),
                slot.Header),
            Content = stack,
            PrimaryButtonText = ResourceStrings.Get("Common_Ok"),
            CloseButtonText = ResourceStrings.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        var selected = radios.FirstOrDefault(r => r.IsChecked == true);
        return selected?.Tag as NormChangePolicy?;
    }

    // ---------------------------------------------------------------
    // «Категории»
    // ---------------------------------------------------------------

    private async Task LoadCategoriesAsync()
    {
        var result = await _maintenanceService.GetCategoriesAsync(includeInactive: false);
        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        Categories.Clear();
        foreach (var category in result.Value!.OrderBy(c => c.Name))
            Categories.Add(new CategoryRow { Id = category.Id, Name = category.Name, Description = category.Description });
    }

    [RelayCommand]
    private void AddCategory()
    {
        _isCreatingCategory = true;
        _editingCategoryId = null;
        SelectedCategoryRow = null;
        CategoryEditorName = string.Empty;
        CategoryEditorDescription = string.Empty;
        BuildEmptyCategorySlots();
        IsCategoryEditorVisible = true;
    }

    public async Task SelectCategoryAsync(CategoryRow row)
    {
        _isCreatingCategory = false;
        _editingCategoryId = row.Id;
        SelectedCategoryRow = row;
        CategoryEditorName = row.Name;
        CategoryEditorDescription = row.Description ?? string.Empty;

        var result = await _maintenanceService.GetCategoryNormsAsync(row.Id);
        CategorySlots.Clear();
        if (result.IsSuccess)
        {
            foreach (var slot in result.Value!)
            {
                var editor = new CategorySlotEditor
                {
                    MaintenanceType = slot.MaintenanceType,
                    Header = MaintenanceTypeLabels.Get(slot.MaintenanceType),
                    IsEnabled = slot.IsEnabled,
                    IsExpanded = slot.IsEnabled,
                    IntervalDaysText = slot.IntervalDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    Description = slot.Description ?? string.Empty,
                };

                foreach (var dept in _repairDepartments)
                    editor.Departments.Add(new CheckableItem { Id = dept.Id, Name = dept.Name, IsChecked = slot.DepartmentIds.Contains(dept.Id) });

                CategorySlots.Add(editor);
            }
        }

        IsCategoryEditorVisible = true;
    }

    private void BuildEmptyCategorySlots()
    {
        CategorySlots.Clear();
        foreach (var type in MaintenanceTypeLabels.All)
        {
            var editor = new CategorySlotEditor
            {
                MaintenanceType = type,
                Header = MaintenanceTypeLabels.Get(type),
            };
            foreach (var dept in _repairDepartments)
                editor.Departments.Add(new CheckableItem { Id = dept.Id, Name = dept.Name });

            CategorySlots.Add(editor);
        }
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryEditorName))
        {
            InfoBanner.Report(ResourceStrings.Get("EquipmentCategory_Validation_NameRequired"), InfoBarSeverity.Warning);
            return;
        }

        var input = new EquipmentCategoryEditInput
        {
            Name = CategoryEditorName.Trim(),
            Description = string.IsNullOrWhiteSpace(CategoryEditorDescription) ? null : CategoryEditorDescription.Trim(),
        };

        Guid categoryId;
        if (_isCreatingCategory)
        {
            var created = await _maintenanceService.CreateCategoryAsync(input);
            if (!created.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(created.Error!.MessageKey), InfoBarSeverity.Error);
                return;
            }
            categoryId = created.Value!.Id;
        }
        else if (_editingCategoryId is Guid existingId)
        {
            var updated = await _maintenanceService.UpdateCategoryAsync(existingId, input);
            if (!updated.IsSuccess)
            {
                InfoBanner.Report(ResolveErrorMessage(updated.Error!.MessageKey), InfoBarSeverity.Error);
                return;
            }
            categoryId = existingId;
        }
        else
        {
            return;
        }

        var slotInputs = CategorySlots.Select(s =>
        {
            int? interval = null;
            if (s.IsEnabled && int.TryParse(s.IntervalDaysText, out var parsed))
                interval = parsed;

            return new CategoryNormSlotInput
            {
                MaintenanceType = s.MaintenanceType,
                IsEnabled = s.IsEnabled,
                IntervalDays = interval,
                Description = string.IsNullOrWhiteSpace(s.Description) ? null : s.Description.Trim(),
                DepartmentIds = s.Departments.Where(d => d.IsChecked).Select(d => d.Id).ToList(),
            };
        }).ToList();

        if (slotInputs.Any(s => s.IsEnabled && s.IntervalDays is null))
        {
            InfoBanner.Report(ResourceStrings.Get("EquipmentCategory_Validation_IntervalRequired"), InfoBarSeverity.Warning);
            return;
        }

        var savedSlots = await _maintenanceService.SaveCategoryNormsAsync(categoryId, slotInputs);
        if (!savedSlots.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(savedSlots.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        ReportSavedSuccess();
        await LoadCategoriesAsync();
        var row = Categories.FirstOrDefault(c => c.Id == categoryId);
        if (row is not null)
            await SelectCategoryAsync(row);
    }

    // ---------------------------------------------------------------
    // «Все нормативы»
    // ---------------------------------------------------------------

    private async Task LoadAuditAsync()
    {
        var result = await _maintenanceService.GetAllEffectiveNormsAsync();
        if (!result.IsSuccess)
        {
            InfoBanner.Report(ResolveErrorMessage(result.Error!.MessageKey), InfoBarSeverity.Error);
            return;
        }

        AuditGroups.Clear();
        foreach (var group in result.Value!.GroupBy(i => i.AssetId))
        {
            var first = group.First();
            var header = first.CategoryName is null
                ? $"{first.AssetNumber} — {first.AssetName}"
                : $"{first.AssetNumber} — {first.AssetName} ({first.CategoryName})";

            var items = new ObservableCollection<AuditNormRow>(group.Select(i => new AuditNormRow
            {
                MaintenanceTypeLabel = MaintenanceTypeLabels.Get(i.MaintenanceType),
                IntervalSummary = i.IsIntervalOverridden
                    ? string.Format(CultureInfo.CurrentCulture, ResourceStrings.Get("MaintenanceNorms_Audit_IntervalOverridden"), i.IntervalDays)
                    : string.Format(CultureInfo.CurrentCulture, ResourceStrings.Get("MaintenanceNorms_Audit_IntervalPreset"), i.IntervalDays),
                NextMaintenanceText = i.NextMaintenanceDate?.ToString("d", CultureInfo.CurrentCulture),
            }));

            AuditGroups.Add(new AuditAssetGroup { AssetId = group.Key, Header = header, Items = items });
        }
    }

    [RelayCommand]
    private async Task OpenAssetFromAuditAsync(Guid assetId)
    {
        var row = _allAssets.FirstOrDefault(a => a.Id == assetId);
        if (row is null)
            return;

        SelectedTabIndex = 0;
        EnsureAssetVisibleInList(assetId);
        await SelectAssetAsync(row);
    }

    private static string ResolveErrorMessage(string key)
    {
        var value = ResourceStrings.Get(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }
}
