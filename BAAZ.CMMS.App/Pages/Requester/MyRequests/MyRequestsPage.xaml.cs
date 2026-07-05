using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Requester.MyRequests;

public sealed partial class MyRequestsPage : Page
{
    public MyRequestsViewModel ViewModel { get; }

    public MyRequestsPage()
    {
        ViewModel = App.Services.GetRequiredService<MyRequestsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnPageLoadedAsync(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.UnsubscribeRealtime();
    }

    private void BrowseList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RequestBrowseItem item)
            _ = ViewModel.SelectItemCommand.ExecuteAsync(item);
    }
}
