using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceScheduleCard : UserControl
{
    public static readonly DependencyProperty ActionLayoutProperty =
        DependencyProperty.Register(
            nameof(ActionLayout),
            typeof(MaintenanceScheduleCardActionLayout),
            typeof(MaintenanceScheduleCard),
            new PropertyMetadata(
                MaintenanceScheduleCardActionLayout.Compact,
                static (d, _) => ((MaintenanceScheduleCard)d).ApplyActionLayout()));

    public MaintenanceScheduleCardActionLayout ActionLayout
    {
        get => (MaintenanceScheduleCardActionLayout)GetValue(ActionLayoutProperty);
        set => SetValue(ActionLayoutProperty, value);
    }

    public MaintenanceScheduleCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
        ActualThemeChanged += (_, _) => WireActions();
        ApplyActionLayout();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => WireActions();

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => WireActions();

    private void ApplyActionLayout()
    {
        var isCompact = ActionLayout == MaintenanceScheduleCardActionLayout.Compact;
        CompactLayout.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
        ExpandedLayout.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
    }

    private void WireActions()
    {
        if (DataContext is not MaintenanceScheduleRow row)
            return;

        var brush = StatusBadgePalette.ResolveBackgroundBrush(row.StatusBadgeBackgroundKey, ActualTheme);
        CompactStatusDot.Fill = brush;
        ExpandedStatusDot.Fill = brush;

        if (ActionLayout != MaintenanceScheduleCardActionLayout.Compact)
            return;

        var page = row.Page;
        if (row.CanStartWork)
        {
            PrimaryActionButton.Content = page.ActionStartWork;
            PrimaryActionButton.Command = page.StartWorkCommand;
            PrimaryActionButton.CommandParameter = row;
            PrimaryActionButton.Visibility = Visibility.Visible;
        }
        else if (row.CanSubmitWorkReport)
        {
            PrimaryActionButton.Content = page.ActionSubmitWorkReport;
            PrimaryActionButton.Command = page.SubmitWorkReportCommand;
            PrimaryActionButton.CommandParameter = row;
            PrimaryActionButton.Visibility = Visibility.Visible;
        }
        else
        {
            PrimaryActionButton.Content = page.ActionDetails;
            PrimaryActionButton.Command = page.ShowDetailsCommand;
            PrimaryActionButton.CommandParameter = row;
            PrimaryActionButton.Visibility = Visibility.Visible;
        }

        MoreActionsButton.Flyout = BuildMoreActionsFlyout(row, page);
    }

    private static MenuFlyout BuildMoreActionsFlyout(MaintenanceScheduleRow row, MaintenanceScheduleViewModel page)
    {
        var flyout = new MenuFlyout();

        flyout.Items.Add(CreateItem(page.ActionDetails, page.ShowDetailsCommand, row));

        if (page.IsAdmin)
        {
            flyout.Items.Add(CreateItem(page.ActionNorms, page.OpenNormsCommand, row));
        }

        // Дублируется основной кнопкой
        // if (row.CanStartWork)
        // {
        //     flyout.Items.Add(CreateItem(page.ActionStartWork, page.StartWorkCommand, row));
        // }

        // Дублируется основной кнопкой
        // if (row.CanSubmitWorkReport)
        // {
        //     flyout.Items.Add(CreateItem(page.ActionSubmitWorkReport, page.SubmitWorkReportCommand, row));
        // }

        if (row.CanCreateMaterialRequisition)
        {
            flyout.Items.Add(CreateItem(page.ActionMaterialRequisition, page.OpenMaterialRequisitionCommand, row));
        }

        flyout.Items.Add(CreateItem(page.ActionExportPprWorkOrder, page.ExportPprWorkOrderCommand, row));

        if (row.CanCreateToolRequisition)
        {
            flyout.Items.Add(CreateItem(page.ActionToolRequisition, page.OpenToolRequisitionCommand, row));
        }

        if (row.CanCancel)
        {
            flyout.Items.Add(CreateItem(page.ActionCancel, page.CancelCommand, row));
        }

        if (row.CanMarkOverdue)
        {
            flyout.Items.Add(CreateItem(page.ActionMarkOverdue, page.MarkOverdueCommand, row));
        }

        return flyout;
    }

    private static MenuFlyoutItem CreateItem(string label, System.Windows.Input.ICommand command, MaintenanceScheduleRow row)
    {
        var item = new MenuFlyoutItem { Text = label, Command = command, CommandParameter = row };
        return item;
    }
}
