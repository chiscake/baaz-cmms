using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed partial class ToolRequisitionPage : Page
{
    public ToolRequisitionViewModel ViewModel { get; }

    public ToolRequisitionPage()
    {
        ViewModel = App.Services.GetRequiredService<ToolRequisitionViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.SubscribeRealtime();
        await ViewModel.OnPageLoadedAsync(e.Parameter);
        SyncChannelSelector();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.UnsubscribeRealtime();
        base.OnNavigatedFrom(e);
    }

    private void ChannelSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var index = sender.SelectedItem is not null ? sender.Items.IndexOf(sender.SelectedItem) : -1;
        if (index >= 0 && index != ViewModel.SelectedChannelIndex)
            ViewModel.SelectedChannelIndex = index;
    }

    private void SyncChannelSelector()
    {
        var index = ViewModel.SelectedChannelIndex;
        if (index < 0 || index >= ChannelSelector.Items.Count)
            return;

        ChannelSelector.SelectedItem = ChannelSelector.Items[index];
    }
}
