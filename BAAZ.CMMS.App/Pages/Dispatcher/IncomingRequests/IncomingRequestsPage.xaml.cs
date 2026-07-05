using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;

public sealed partial class IncomingRequestsPage : Page
{
    public IncomingRequestsViewModel ViewModel { get; }

    public IncomingRequestsPage()
    {
        ViewModel = App.Services.GetRequiredService<IncomingRequestsViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnPageLoadedAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.UnsubscribeRealtime();
    }
}
