using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceSchedulePage : Page
{
    private bool _syncingViewSelector;

    public MaintenanceScheduleViewModel ViewModel { get; }

    public MaintenanceSchedulePage()
    {
        ViewModel = App.Services.GetRequiredService<MaintenanceScheduleViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
        SyncViewSelectorFromViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RestorePrefs();
        SyncViewSelectorFromViewModel();
        await ViewModel.OnPageLoadedAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.UnsubscribeRealtime();
    }

    private void ViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_syncingViewSelector || sender.SelectedItem is null)
            return;

        var index = sender.Items.IndexOf(sender.SelectedItem);
        if (index < 0)
            return;

        ViewModel.SelectedViewIndex = index;
    }

    private void SyncViewSelectorFromViewModel()
    {
        if (ViewModel.SelectedViewIndex < 0 || ViewModel.SelectedViewIndex >= ViewSelector.Items.Count)
            return;

        if (ViewSelector.Items.IndexOf(ViewSelector.SelectedItem) == ViewModel.SelectedViewIndex)
            return;

        _syncingViewSelector = true;
        try
        {
            ViewSelector.SelectedItem = ViewSelector.Items[ViewModel.SelectedViewIndex];
        }
        finally
        {
            _syncingViewSelector = false;
        }
    }
}
