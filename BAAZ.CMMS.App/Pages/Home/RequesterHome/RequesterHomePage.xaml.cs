using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Home.RequesterHome;

public sealed partial class RequesterHomePage : Page
{
    public RequesterHomeViewModel ViewModel { get; }

    public RequesterHomePage()
    {
        ViewModel = App.Services.GetRequiredService<RequesterHomeViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }
}
