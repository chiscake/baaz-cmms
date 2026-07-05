using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Controls.PageLayout;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceScheduleChartView : UserControl
{
    public MaintenanceScheduleChartView()
    {
        InitializeComponent();
        PanelColumn.MaxWidth = PageLayoutMaxWidthValues.Small640Pixels;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DataContext is MaintenanceScheduleViewModel vm && vm.SplitPaneStarWeight > 0)
            TimelineColumn.Width = new GridLength(vm.SplitPaneStarWeight, GridUnitType.Star);

        SplitPane.ManipulationCompleted += (_, _) => SaveSplitPaneWeight();
    }

    private void SaveSplitPaneWeight()
    {
        if (DataContext is not MaintenanceScheduleViewModel vm)
            return;

        if (ActualWidth <= 0)
            return;

        var timelineWidth = TimelineColumn.ActualWidth;
        var panelWidth = PanelColumn.ActualWidth;
        if (panelWidth <= 0)
            return;

        vm.SaveSplitPaneWeight(timelineWidth / panelWidth);
    }
}
