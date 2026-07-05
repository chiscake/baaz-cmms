using System;
using System.Diagnostics;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.AssetRegistry;

public sealed partial class AssetRegistryPage : Page
{
    public AssetRegistryViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<AssetRegistryViewModel, AssetRow> _crud;
    private readonly LocationPickerEditorSync _locationPickerSync;

    public AssetRegistryPage()
    {
        ViewModel = App.Services.GetRequiredService<AssetRegistryViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<AssetRegistryViewModel, AssetRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<AssetRow>
            {
                ResourcePrefix = "Assets",
                ArchiveRowAsync = row => ViewModel.SetRowDecommissionedAsync(row, row.IsActive),
                DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
                GetArchiveContextMenuLabel = row => row.IsActive
                    ? ResourceStrings.Get("Assets_Context_Decommission")
                    : ResourceStrings.Get("Assets_Context_Restore"),
                GetArchiveRowTitleKey = (_, _, archiving) =>
                    archiving ? "Assets_Decommission_Title" : "Assets_Restore_Title",
                GetArchiveRowMessageKey = (_, _, archiving) =>
                    archiving ? "Assets_Decommission_Message" : "Assets_Restore_Message",
                GetArchiveBulkTitleKey = (anyActive, anyInactive) =>
                    anyInactive && !anyActive
                        ? "Assets_RestoreBulk_Title"
                        : "Assets_DecommissionBulk_Title",
                GetArchiveBulkMessageKey = (anyActive, anyInactive) =>
                    anyInactive && !anyActive
                        ? "Assets_RestoreBulk_Message"
                        : "Assets_DecommissionBulk_Message",
            });
        _crud.Wire();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _locationPickerSync = new LocationPickerEditorSync(
            this,
            () => EditorLocationPicker,
            () => ViewModel.IsEditorOpen,
            () => ViewModel.LocationTreeVersion,
            () => ViewModel.LocationTreeRoots,
            () => ViewModel.LocationFullPaths,
            () => ViewModel.EditorLocationId,
            id => ViewModel.EditorLocationId = id,
            "AssetRegistryPage");
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AssetRegistryViewModel.IsEditorOpen))
        {
            Debug.WriteLine(
                $"[AssetRegistryPage] IsEditorOpen={ViewModel.IsEditorOpen}, " +
                $"EditingRow={ViewModel.EditingRow?.Id}, IsNew={ViewModel.IsNewRecord}");
            _locationPickerSync.QueueSync();
        }
        else if (e.PropertyName is nameof(AssetRegistryViewModel.LocationTreeVersion))
        {
            _locationPickerSync.QueueSync();
        }
        else if (e.PropertyName is nameof(AssetRegistryViewModel.EditorLocationId))
        {
            _locationPickerSync.OnVmSelectedIdChanged();
        }
    }

    private void EditorLocationPicker_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Debug.WriteLine("[AssetRegistryPage] EditorLocationPicker Loaded");
        _locationPickerSync.QueueSync();
    }

    private void EditorField_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => Debug.WriteLine($"[AssetRegistryPage] Editor field Loaded: {(sender as FrameworkElement)?.Name ?? sender.GetType().Name}");

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is AssetRegistryNavigationArgs args)
            await ViewModel.ApplyNavigationArgsAsync(args);
        else
            await _crud.LoadAsync();
    }
}
