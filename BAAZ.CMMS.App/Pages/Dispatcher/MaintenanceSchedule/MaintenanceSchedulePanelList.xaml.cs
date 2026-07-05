using System;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

public sealed partial class MaintenanceSchedulePanelList : UserControl
{
    private MaintenanceScheduleViewModel? _vm;
    private DispatcherTimer? _highlightTimer;

    public MaintenanceSchedulePanelList()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MaintenanceScheduleViewModel vm)
            Attach(vm);
        else
            DataContextChanged += OnDataContextChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MaintenanceScheduleViewModel vm)
            Attach(vm);
    }

    private void Attach(MaintenanceScheduleViewModel vm)
    {
        Detach();
        _vm = vm;
        _vm.ScrollToRowRequested += OnScrollToRowRequested;
        _vm.ScrollToDateRequested += OnScrollToDateRequested;
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void Detach()
    {
        if (_vm is null)
            return;

        _vm.ScrollToRowRequested -= OnScrollToRowRequested;
        _vm.ScrollToDateRequested -= OnScrollToDateRequested;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = null;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MaintenanceScheduleViewModel.HighlightedRowId)
            && _vm?.HighlightedRowId is Guid id)
        {
            HighlightRow(id);
        }
    }

    private void OnScrollToRowRequested(object? sender, Guid scheduleId) => ScrollToRow(scheduleId);

    private void OnScrollToDateRequested(object? sender, DateOnly date)
    {
        var row = _vm?.PanelRows.FirstOrDefault(r => r.PlannedDate == date);
        if (row is not null)
            ScrollToRow(row.Id);
    }

    private void ScrollToRow(Guid scheduleId)
    {
        var row = _vm?.PanelRows.FirstOrDefault(r => r.Id == scheduleId);
        if (row is null)
            return;

        PanelList.ScrollIntoView(row, ScrollIntoViewAlignment.Leading);
        HighlightRow(scheduleId);
    }

    private void HighlightRow(Guid scheduleId)
    {
        ListViewItem? highlighted = null;
        var accentBrush = Application.Current.Resources["AccentFillColorDefaultBrush"] as Brush;
        var subtleBrush = Application.Current.Resources["SubtleFillColorSecondaryBrush"] as Brush;

        foreach (var item in PanelList.Items)
        {
            if (PanelList.ContainerFromItem(item) is not ListViewItem container)
                continue;

            container.ApplyTemplate();
            var strip = FindHighlightStrip(container);
            var isMatch = item is MaintenanceScheduleRow row && row.Id == scheduleId;

            if (isMatch)
            {
                container.Background = subtleBrush;
                if (strip is not null)
                    strip.Background = accentBrush;
                highlighted = container;
            }
            else
            {
                ClearRowHighlight(container, strip);
            }
        }

        _highlightTimer?.Stop();
        if (highlighted is null)
            return;

        _highlightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _highlightTimer.Tick += (_, _) =>
        {
            _highlightTimer.Stop();
            ClearRowHighlight(highlighted, FindHighlightStrip(highlighted));
        };
        _highlightTimer.Start();
    }

    private static void ClearRowHighlight(ListViewItem container, Border? strip)
    {
        container.ClearValue(BackgroundProperty);
        strip?.ClearValue(Border.BackgroundProperty);
    }

    private static Border? FindHighlightStrip(ListViewItem item)
    {
        if (VisualTreeHelper.GetChildrenCount(item) == 0)
            return null;

        if (VisualTreeHelper.GetChild(item, 0) is not Grid grid)
            return null;

        foreach (var child in grid.Children)
        {
            if (child is Border { Name: "HighlightStrip" } strip)
                return strip;
        }

        return null;
    }
}
