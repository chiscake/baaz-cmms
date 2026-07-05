using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.WorkReports;

public sealed partial class WorkReportsPage : Page
{
    public WorkReportsViewModel ViewModel { get; }

    public WorkReportsPage()
    {
        ViewModel = App.Services.GetRequiredService<WorkReportsViewModel>();
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
