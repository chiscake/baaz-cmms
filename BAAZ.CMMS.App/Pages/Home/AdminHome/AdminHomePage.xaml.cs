using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Home.AdminHome;

public sealed partial class AdminHomePage : Page
{
    public AdminHomeViewModel ViewModel { get; }

    public AdminHomePage()
    {
        ViewModel = App.Services.GetRequiredService<AdminHomeViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }
}
