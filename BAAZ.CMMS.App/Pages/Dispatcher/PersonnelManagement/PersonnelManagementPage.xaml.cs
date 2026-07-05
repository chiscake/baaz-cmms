using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using BAAZ.CMMS.App.Controls.CrudWorkbench;

namespace BAAZ.CMMS.App.Pages.Dispatcher.PersonnelManagement;

public sealed partial class PersonnelManagementPage : Page
{
    public PersonnelManagementViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<PersonnelManagementViewModel, PersonnelRow> _crud;

    public PersonnelManagementPage()
    {
        ViewModel = App.Services.GetRequiredService<PersonnelManagementViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<PersonnelManagementViewModel, PersonnelRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<PersonnelRow>
            {
                ResourcePrefix = "Personnel",
                BulkArchiveConfirmMode = CrudBulkArchiveConfirmMode.Never,
                ConfirmArchiveRow = false,
                ArchiveRowAsync = row => ViewModel.SetRowActiveAsync(row, !row.IsActive),
                DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
                GetRowDisplayName = row => row.FullName,
            });
        _crud.Wire();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _crud.LoadAsync();
    }
}
