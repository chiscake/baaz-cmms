using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Home.DispatcherHome;

public sealed partial class DispatcherHomePage : Page
{
    public DispatcherHomeViewModel ViewModel { get; }

    public DispatcherHomePage()
    {
        ViewModel = App.Services.GetRequiredService<DispatcherHomeViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SubscribeRealtime();
        await ViewModel.LoadAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.UnsubscribeRealtime();
        base.OnNavigatedFrom(e);
    }
}
