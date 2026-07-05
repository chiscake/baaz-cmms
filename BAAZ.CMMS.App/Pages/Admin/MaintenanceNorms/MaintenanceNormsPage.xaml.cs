using System;

using BAAZ.CMMS.App.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;

public sealed partial class MaintenanceNormsPage : Page
{
    private const int MaxScrollIntoViewAttempts = 40;

    private bool _isProgrammaticAssetSelection;
    private bool _isProgrammaticCategorySelection;

    public MaintenanceNormsViewModel ViewModel { get; }

    public MaintenanceNormsPage()
    {
        ViewModel = App.Services.GetRequiredService<MaintenanceNormsViewModel>();
        InitializeComponent();
    }

    public static string OverrideLabel => ResourceStrings.Get("MaintenanceNorms_Slot_Override");

    public static string IntervalLabel => ResourceStrings.Get("MaintenanceNorms_Slot_Interval");

    public static string DescriptionLabel => ResourceStrings.Get("MaintenanceNorms_Slot_Description");

    public static string ResetToPresetLabel => ResourceStrings.Get("MaintenanceNorms_Slot_ResetToPreset");

    public static string EnabledLabel => ResourceStrings.Get("MaintenanceNorms_Slot_Enabled");

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync(e.Parameter);

        if (ViewModel.SelectedTabIndex != TabSelector.Items.IndexOf(TabSelector.SelectedItem))
        {
            TabSelector.SelectedItem = TabSelector.Items[ViewModel.SelectedTabIndex];
            ApplyTabVisibility(ViewModel.SelectedTabIndex);
        }

        SyncAssetListSelection(ViewModel.SelectedAssetRow);
        SyncCategoryListSelection(ViewModel.SelectedCategoryRow);
    }

    private void TabSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var index = sender.Items.IndexOf(sender.SelectedItem);
        ViewModel.SelectedTabIndex = index;
        ApplyTabVisibility(index);
    }

    private void ApplyTabVisibility(int index)
    {
        AssetsTabPanel.Visibility = index == 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        CategoriesTabPanel.Visibility = index == 1 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        AuditTabPanel.Visibility = index == 2 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private async void AssetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isProgrammaticAssetSelection)
            return;

        if (e.AddedItems.Count > 0 && e.AddedItems[0] is AssetPickerRow row)
            await ViewModel.SelectAssetAsync(row);
    }

    private async void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isProgrammaticCategorySelection)
            return;

        if (e.AddedItems.Count > 0 && e.AddedItems[0] is CategoryRow row)
            await ViewModel.SelectCategoryAsync(row);
    }

    private void SyncAssetListSelection(AssetPickerRow? row)
    {
        _isProgrammaticAssetSelection = true;
        try
        {
            AssetListView.SelectedItem = row;
        }
        finally
        {
            _isProgrammaticAssetSelection = false;
        }

        if (row is not null)
            QueueScrollIntoView(AssetListView, row);
    }

    private void SyncCategoryListSelection(CategoryRow? row)
    {
        _isProgrammaticCategorySelection = true;
        try
        {
            CategoryListView.SelectedItem = row;
        }
        finally
        {
            _isProgrammaticCategorySelection = false;
        }

        if (row is not null)
            QueueScrollIntoView(CategoryListView, row);
    }

    private void QueueScrollIntoView(ListView listView, object item, int attempt = 0)
    {
        if (attempt >= MaxScrollIntoViewAttempts)
            return;

        if (listView.ContainerFromItem(item) is not null)
        {
            listView.ScrollIntoView(item);
            return;
        }

        _ = DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => QueueScrollIntoView(listView, item, attempt + 1));
    }
}
