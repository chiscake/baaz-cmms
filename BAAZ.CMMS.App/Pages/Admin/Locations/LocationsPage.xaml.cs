using System;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.Locations;

public sealed partial class LocationsPage : Page
{
    public LocationsViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<LocationsViewModel, LocationRow> _crud;
    private readonly LocationPickerEditorSync _parentPickerSync;

    public LocationsPage()
    {
        ViewModel = App.Services.GetRequiredService<LocationsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<LocationsViewModel, LocationRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<LocationRow>
            {
                ResourcePrefix = "Locations",
                ArchiveRowAsync = row => ViewModel.SetRowArchivedAsync(row, row.IsActive),
                DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
                GetRowDisplayName = row => row.FullPath,
                GetArchiveContextMenuLabel = row => row.IsActive
                    ? ResourceStrings.Get("Locations_Context_ArchiveBranch")
                    : ResourceStrings.Get("Locations_Context_RestoreBranch"),
            });
        _crud.Wire();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _parentPickerSync = new LocationPickerEditorSync(
            this,
            () => EditorParentPicker,
            () => ViewModel.IsEditorOpen,
            () => ViewModel.ParentTreeVersion,
            () => ViewModel.ParentTreeRoots,
            () => ViewModel.ParentLocationFullPaths,
            () => ViewModel.EditorParentId,
            id => ViewModel.EditorParentId = id,
            "LocationsPage");
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocationsViewModel.IsEditorOpen)
            or nameof(LocationsViewModel.ParentTreeVersion))
        {
            _parentPickerSync.QueueSync();
        }
        else if (e.PropertyName is nameof(LocationsViewModel.EditorParentId))
        {
            _parentPickerSync.OnVmSelectedIdChanged();
        }
    }

    private void EditorParentPicker_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => _parentPickerSync.QueueSync();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _crud.LoadAsync();
    }
}
