using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.CrudWorkbench;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.AllRequests;

public sealed partial class AllRequestsPage : Page
{
    public AllRequestsViewModel ViewModel { get; }

    private readonly CrudCatalogPageWireup<AllRequestsViewModel, AdminRequestRow> _crud;

    public AllRequestsPage()
    {
        ViewModel = App.Services.GetRequiredService<AllRequestsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        _crud = new CrudCatalogPageWireup<AllRequestsViewModel, AdminRequestRow>(
            ViewModel,
            Workbench,
            new CrudCatalogPageOptions<AdminRequestRow>
            {
                ResourcePrefix = "AllRequests",
                ArchiveRowAsync = _ => Task.CompletedTask,
                DeleteRowAsync = _ => Task.CompletedTask,
            });
        _crud.Wire();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await _crud.LoadAsync();
    }
}
