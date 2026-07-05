using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using BAAZ.CMMS.App.Controls.CrudWorkbench;

namespace BAAZ.CMMS.App.Pages.Admin.RepairDepartments;

public sealed partial class RepairDepartmentsPage : Page
{
    public RepairDepartmentsViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<RepairDepartmentsViewModel, RepairDepartmentRow> _crud;

    public RepairDepartmentsPage()
    {
        ViewModel = App.Services.GetRequiredService<RepairDepartmentsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<RepairDepartmentsViewModel, RepairDepartmentRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<RepairDepartmentRow>
            {
                ResourcePrefix = "RepairDepartments",
                ArchiveRowAsync = row => ViewModel.SetRowArchivedAsync(row, row.IsActive),
                DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
                GetRowDisplayName = row => row.Name,
            });
        _crud.Wire();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _crud.LoadAsync();
    }
}
