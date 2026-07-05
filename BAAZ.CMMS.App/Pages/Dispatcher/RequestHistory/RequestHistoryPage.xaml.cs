using BAAZ.CMMS.App.Pages.Requester.MyRequests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestHistory;

public sealed partial class RequestHistoryPage : Page
{
    public RequestHistoryViewModel ViewModel { get; }

    public RequestHistoryPage()
    {
        ViewModel = App.Services.GetRequiredService<RequestHistoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnPageLoadedAsync();
    }

    private void BrowseList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RequestBrowseItem item)
            _ = ViewModel.SelectItemCommand.ExecuteAsync(item);
    }
}
