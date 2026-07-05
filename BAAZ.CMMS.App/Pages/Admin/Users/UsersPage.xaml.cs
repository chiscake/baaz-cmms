using System;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.Users;

public sealed partial class UsersPage : Page
{
    public UsersViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<UsersViewModel, UserRow> _crud;
    private readonly LocationPickerEditorSync _locationPickerSync;
    private readonly LocationScopePickerEditorSync _scopePickerSync;

    public UsersPage()
    {
        ViewModel = App.Services.GetRequiredService<UsersViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<UsersViewModel, UserRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<UserRow>
            {
                ResourcePrefix = "Users",
                ConfirmArchiveRow = false,
                ArchiveRowAsync = row => ViewModel.BanRowAsync(row),
                DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
                GetRowDisplayName = row => row.FullName,
                GetArchiveContextMenuLabel = row => row.IsActive
                    ? ResourceStrings.Get("Users_Context_Ban")
                    : ResourceStrings.Get("Users_Context_Unban"),
                CanEditRow = row => ViewModel.CanEditRow(row),
                CanMutateRow = row => ViewModel.CanMutateRow(row),
                BulkArchiveAsync = ConfirmBulkBanAsync,
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
            "UsersPage");
        _scopePickerSync = new LocationScopePickerEditorSync(
            this,
            () => EditorLocationScopePicker,
            () => ViewModel.IsEditorOpen && ViewModel.ShowScopeEditor,
            () => ViewModel.LocationTreeVersion,
            () => ViewModel.ScopeTreeProjection,
            () => ViewModel.LocationTreeRoots,
            () => ViewModel.LocationFullPaths,
            () => ViewModel.EditorScopeLocationIds,
            ids => ViewModel.SetEditorScopeLocationIds(ids),
            "UsersPage");
        _scopePickerSync.EnsurePickerSubscribed();
    }

    private async Task ConfirmBulkBanAsync()
    {
        var selected = ViewModel.GetSelectableSelectedRowsPublic();
        if (selected.Count == 0)
            return;

        var anyActive = selected.Any(r => r.IsActive);
        var title = anyActive
            ? ResourceStrings.Get("Users_BanBulk_Title")
            : ResourceStrings.Get("Users_UnbanBulk_Title");
        var message = anyActive
            ? string.Format(ResourceStrings.Get("Users_BanBulk_Message"), selected.Count)
            : string.Format(ResourceStrings.Get("Users_UnbanBulk_Message"), selected.Count);

        var confirmed = await AppDialogHelper.ConfirmAsync(title, message, App.MainWindow);
        if (!confirmed)
            return;

        if (ViewModel.BulkArchiveCommand.CanExecute(null))
            await ViewModel.BulkArchiveCommand.ExecuteAsync(null);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UsersViewModel.IsEditorOpen)
            or nameof(UsersViewModel.LocationTreeVersion)
            or nameof(UsersViewModel.ShowScopeEditor))
        {
            _locationPickerSync.QueueSync();
            _scopePickerSync.QueueSync();
        }
        else if (e.PropertyName is nameof(UsersViewModel.EditorLocationId))
        {
            _locationPickerSync.OnVmSelectedIdChanged();
        }
        else if (e.PropertyName is nameof(UsersViewModel.EditorScopeLocationIds))
        {
            _scopePickerSync.OnVmScopeIdsChanged();
        }
    }

    private void EditorLocationPicker_Loaded(object sender, RoutedEventArgs e)
        => _locationPickerSync.QueueSync();

    private void EditorLocationScopePicker_Loaded(object sender, RoutedEventArgs e)
    {
        _scopePickerSync.EnsurePickerSubscribed();
        _scopePickerSync.QueueSync();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _crud.LoadAsync();
    }

    private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        => ViewModel.GeneratePassword();
}
