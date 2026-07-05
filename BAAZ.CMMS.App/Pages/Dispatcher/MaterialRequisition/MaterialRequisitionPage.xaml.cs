using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed partial class MaterialRequisitionPage : Page
{
    public MaterialRequisitionViewModel ViewModel { get; }

    public MaterialRequisitionPage()
    {
        ViewModel = App.Services.GetRequiredService<MaterialRequisitionViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnPageLoadedAsync(e.Parameter);
    }
}
