using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed partial class ToolRequisitionHistoryPage : Page
{
    public ToolRequisitionHistoryViewModel ViewModel { get; }

    public ToolRequisitionHistoryPage()
    {
        ViewModel = App.Services.GetRequiredService<ToolRequisitionHistoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SubscribeRealtime();
        await ViewModel.OnPageLoadedAsync(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.UnsubscribeRealtime();
        base.OnNavigatedFrom(e);
    }

    private void BrowseList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolRequisitionBrowseItem item)
            _ = ViewModel.SelectItemCommand.ExecuteAsync(item);
    }
}
