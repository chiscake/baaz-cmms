using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Windows.System;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

public sealed partial class MaintenanceScheduleTimelineControl : UserControl
{
    private const double RowHeight = 36;
    private const double HeaderHeight = 48;

    private MaintenanceScheduleViewModel? _vm;
    private bool _renderPending;
    private bool _suppressGridScrollChanged;
    private bool _suppressLabelsScrollChanged;
    private bool _suppressHeaderScrollChanged;
    private double _lastHeaderRightPad = double.NaN;
    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    public MaintenanceScheduleTimelineControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += (_, _) =>
        {
            ScheduleChartTheme.ClearBrushCache();
            ScheduleRenderAll();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        if (DataContext is MaintenanceScheduleViewModel vm)
            AttachViewModel(vm);
        else
            DataContextChanged += OnDataContextChanged;

        Root.KeyDown += OnKeyDown;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachViewModel();
        Root.KeyDown -= OnKeyDown;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MaintenanceScheduleViewModel vm)
            AttachViewModel(vm);
    }

    private void AttachViewModel(MaintenanceScheduleViewModel vm)
    {
        DetachViewModel();
        _vm = vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        LabelsRepeater.ElementPrepared += OnLabelElementPrepared;
        GridRepeater.ElementPrepared += OnGridElementPrepared;
        WireCommands();
        SyncZoomSelectorFromViewModel();
        RenderAll();
    }

    private void DetachViewModel()
    {
        if (_vm is null)
            return;

        LabelsRepeater.ElementPrepared -= OnLabelElementPrepared;
        GridRepeater.ElementPrepared -= OnGridElementPrepared;
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm = null;
    }

    private void WireCommands()
    {
        if (_vm is null)
            return;

        PrevButton.Content = _vm.ChartNavPrevLabel;
        NextButton.Content = _vm.ChartNavNextLabel;
        TodayButton.Content = _vm.ChartNavTodayLabel;

        PrevButton.Command = _vm.NavigatePrevCommand;
        NextButton.Command = _vm.NavigateNextCommand;
        TodayButton.Command = _vm.GoToTodayCommand;
    }

    private void ZoomSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_vm is null || sender.SelectedItem is null)
            return;

        var index = sender.Items.IndexOf(sender.SelectedItem);
        if (index < 0 || (int)_vm.SelectedZoomPreset == index)
            return;

        switch (index)
        {
            case 0:
                _vm.SetZoomWeekCommand.Execute(null);
                break;
            case 1:
                _vm.SetZoomMonthCommand.Execute(null);
                break;
            case 2:
                _vm.SetZoomQuarterCommand.Execute(null);
                break;
        }
    }

    private void SyncZoomSelectorFromViewModel()
    {
        if (_vm is null)
            return;

        var index = (int)_vm.SelectedZoomPreset;
        if (index < 0 || index >= ZoomSelector.Items.Count)
            return;

        if (ZoomSelector.Items.IndexOf(ZoomSelector.SelectedItem) == index)
            return;

        ZoomSelector.SelectedItem = ZoomSelector.Items[index];
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MaintenanceScheduleViewModel.IsChartLoading))
        {
            if (_vm is not null && !_vm.IsChartLoading)
                ScheduleRenderAll();
            return;
        }

        if (e.PropertyName is nameof(MaintenanceScheduleViewModel.HighlightedRowId))
        {
            RefreshRowRepeaterVisuals();
            return;
        }

        if (e.PropertyName is nameof(MaintenanceScheduleViewModel.SwimlaneRows)
            or nameof(MaintenanceScheduleViewModel.DayHeaders)
            or nameof(MaintenanceScheduleViewModel.HeatSegments)
            or nameof(MaintenanceScheduleViewModel.VisibleRangeText)
            or nameof(MaintenanceScheduleViewModel.TimelineTotalWidth)
            or nameof(MaintenanceScheduleViewModel.TodayLineLeft))
        {
            ScheduleRenderAll();
        }
        else if (e.PropertyName is nameof(MaintenanceScheduleViewModel.SelectedZoomPreset))
        {
            SyncZoomSelectorFromViewModel();
            ScheduleRenderAll();
        }
    }

    private void ScheduleRenderAll()
    {
        if (_vm?.IsChartLoading == true)
            return;

        if (_renderPending)
            return;

        _renderPending = true;
        var queue = _dispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        queue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            _renderPending = false;
            if (_vm?.IsChartLoading == true)
                return;

            RenderAll();
        });
    }

    private void RenderAll()
    {
        if (_vm is null)
            return;

        RangeText.Text = _vm.VisibleRangeText;
        RenderHeatMap();
        RenderLabelsHeader();
        RenderHeader();
        RenderLaneGridOverlay();
        UpdateRowRepeaters();
        UpdateScrollBarPadding(force: true);
    }

    private void UpdateRowRepeaters()
    {
        if (_vm is null)
            return;

        LabelsRepeater.ItemsSource = _vm.SwimlaneRows;
        GridRepeater.ItemsSource = _vm.SwimlaneRows;
    }

    private void RefreshRowRepeaterVisuals()
    {
        if (_vm is null)
            return;

        for (var i = 0; i < _vm.SwimlaneRows.Count; i++)
        {
            if (LabelsRepeater.TryGetElement(i) is Border labelBorder)
                UpdateLabelBorder(labelBorder, _vm.SwimlaneRows[i]);

            if (GridRepeater.TryGetElement(i) is Canvas laneCanvas)
            {
                ChartLaneVisuals.GetOrCreate(laneCanvas, this)
                    .Update(_vm.SwimlaneRows[i], _vm, _vm.TimelineTotalWidth, _vm.DayHeaders);
            }
        }
    }

    private void OnLabelElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (_vm is null || args.Index < 0 || args.Index >= _vm.SwimlaneRows.Count)
            return;

        if (args.Element is not Border border)
            return;

        border.HorizontalAlignment = HorizontalAlignment.Stretch;
        UpdateLabelBorder(border, _vm.SwimlaneRows[args.Index]);
    }

    private void OnGridElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (_vm is null || args.Index < 0 || args.Index >= _vm.SwimlaneRows.Count)
            return;

        if (args.Element is not Canvas canvas)
            return;

        var row = _vm.SwimlaneRows[args.Index];
        ChartLaneVisuals.GetOrCreate(canvas, this)
            .Update(row, _vm, _vm.TimelineTotalWidth, _vm.DayHeaders);
    }

    private void UpdateLabelBorder(Border border, ChartLaneRowVm row)
    {
        border.Height = RowHeight;
        border.HorizontalAlignment = HorizontalAlignment.Stretch;
        border.Background = GetRowBackground(row);
        border.BorderBrush = null;
        border.BorderThickness = new Thickness(0);
        border.Tag = row.Id;

        if (border.Child is not Grid contentGrid || border.DataContext is not LabelRowChrome chrome)
        {
            contentGrid = BuildLabelContentGrid(out chrome);
            border.Child = contentGrid;
            border.DataContext = chrome;
            border.PointerEntered -= LabelBorder_PointerEntered;
            border.PointerExited -= LabelBorder_PointerExited;
            border.PointerEntered += LabelBorder_PointerEntered;
            border.PointerExited += LabelBorder_PointerExited;
        }

        contentGrid.Padding = new Thickness(12 * row.IndentLevel, 0, 8, 0);
        chrome.Chevron.Visibility = row.Kind == ChartLaneRowKind.Location && row.HasChildren
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (chrome.Chevron.Visibility == Visibility.Visible)
            chrome.Chevron.Text = row.IsCollapsed ? "▸" : "▾";

        chrome.Label.Text = row.Label;
        chrome.Label.Style = row.Kind == ChartLaneRowKind.Location
            ? ChartPrimaryLabelStyle
            : ChartSecondaryLabelStyle;

        if (row.AssetId is Guid assetId)
        {
            chrome.Label.Tag = assetId;
            chrome.Label.PointerPressed -= AssetLabel_PointerPressed;
            chrome.Label.PointerPressed += AssetLabel_PointerPressed;
        }
        else
        {
            chrome.Label.Tag = null;
            chrome.Label.PointerPressed -= AssetLabel_PointerPressed;
        }

        if (row.Kind == ChartLaneRowKind.Location && row.HasChildren)
        {
            contentGrid.Background = ScheduleChartTheme.Transparent;
            contentGrid.Tag = row.Id;
            contentGrid.PointerPressed -= LocationRow_PointerPressed;
            contentGrid.PointerPressed += LocationRow_PointerPressed;
        }
        else
        {
            contentGrid.Background = null;
            contentGrid.Tag = null;
            contentGrid.PointerPressed -= LocationRow_PointerPressed;
        }
    }

    private Grid BuildLabelContentGrid(out LabelRowChrome chrome)
    {
        var contentGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        chrome = new LabelRowChrome
        {
            Chevron = new TextBlock
            {
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                Style = ChartSecondaryLabelStyle,
                IsHitTestVisible = false,
            },
            Label = new TextBlock(),
            Divider = new Border
            {
                Background = ThemeBrush(ScheduleChartTheme.Divider),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
            },
        };

        Grid.SetColumn(chrome.Chevron, 0);
        Grid.SetRowSpan(chrome.Chevron, 2);
        contentGrid.Children.Add(chrome.Chevron);

        Grid.SetColumn(chrome.Label, 1);
        Grid.SetRow(chrome.Label, 0);
        contentGrid.Children.Add(chrome.Label);

        Grid.SetColumnSpan(chrome.Divider, 2);
        Grid.SetRow(chrome.Divider, 1);
        contentGrid.Children.Add(chrome.Divider);

        return contentGrid;
    }

    private sealed class LabelRowChrome
    {
        public required TextBlock Chevron { get; init; }

        public required TextBlock Label { get; init; }

        public required Border Divider { get; init; }
    }

    private Brush GetRowBackground(ChartLaneRowVm row) =>
        IsRowSelected(row)
            ? ThemeBrush(ScheduleChartTheme.RowSelected)
            : ScheduleChartTheme.RowBackground(row.Kind, this);

    private Brush ThemeBrush(string key) => ScheduleChartTheme.Brush(key, this);

    private Style ChartPrimaryLabelStyle => (Style)Resources["ChartPrimaryLabelStyle"];
    private Style ChartSecondaryLabelStyle => (Style)Resources["ChartSecondaryLabelStyle"];
    private Style ChartHeaderCaptionStyle => (Style)Resources["ChartHeaderCaptionStyle"];
    private Style ChartHeaderAccentStyle => (Style)Resources["ChartHeaderAccentStyle"];

    private void RenderHeatMap()
    {
        HeatMapCanvas.Children.Clear();
        if (_vm is null)
            return;

        HeatMapCanvas.Width = _vm.TimelineTotalWidth;

        foreach (var segment in _vm.HeatSegments)
        {
            if (segment.StatusBrushKey is null)
                continue;

            var segmentBar = new Border
            {
                Width = segment.Width,
                Height = 8,
                Background = ResolveBrush(segment.StatusBrushKey),
                CornerRadius = new CornerRadius(3),
            };
            Canvas.SetLeft(segmentBar, segment.Left);
            Canvas.SetTop(segmentBar, 0);
            ToolTipService.SetToolTip(segmentBar, segment.Date.ToString("d"));
            segmentBar.PointerPressed += (_, _) => _vm.ScrollPanelToDateCommand.Execute(segment.Date);
            HeatMapCanvas.Children.Add(segmentBar);
        }
    }

    private void RenderLabelsHeader()
    {
        LabelsHeaderPanel.Children.Clear();
        if (_vm is null)
            return;

        LabelsHeaderPanel.Children.Add(new Border { Height = 8, IsHitTestVisible = false });
        LabelsHeaderPanel.Children.Add(CreateHeaderLabel(_vm.ChartLaneObjectHeader));
    }

    private void LabelBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Guid rowId || _vm is null)
            return;

        var row = _vm.SwimlaneRows.FirstOrDefault(r => r.Id == rowId);
        if (row is null || IsRowSelected(row))
            return;

        border.Background = ThemeBrush(ScheduleChartTheme.RowHover);
    }

    private void LabelBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Guid rowId || _vm is null)
            return;

        var row = _vm.SwimlaneRows.FirstOrDefault(r => r.Id == rowId);
        if (row is null)
            return;

        border.Background = GetRowBackground(row);
    }

    private Brush GetLabelRowBackground(ChartLaneRowVm row) => GetRowBackground(row);

    private bool IsRowSelected(ChartLaneRowVm row)
    {
        if (_vm?.HighlightedRowId is not Guid scheduleId)
            return false;

        return row.Kind == ChartLaneRowKind.Asset
            && row.Markers.Any(m => m.ScheduleId == scheduleId);
    }

    private void RenderHeader()
    {
        HeaderCanvas.Children.Clear();
        if (_vm is null)
            return;

        var totalWidth = _vm.TimelineTotalWidth;
        HeaderCanvas.Width = totalWidth;
        HeaderCanvas.Background = ThemeBrush(ScheduleChartTheme.HeaderBackground);

        AddWeekendColumns(HeaderCanvas, HeaderHeight);
        AddDayGridLines(HeaderCanvas, HeaderHeight, includeMonthDividers: true);

        foreach (var day in _vm.DayHeaders)
        {
            if (day.MonthLabel is not null)
            {
                var month = new TextBlock
                {
                    Text = day.MonthLabel,
                    Style = ChartHeaderCaptionStyle,
                };
                Canvas.SetLeft(month, day.Left + 4);
                Canvas.SetTop(month, 2);
                HeaderCanvas.Children.Add(month);
            }

            var isQuarter = _vm.SelectedZoomPreset == ScheduleZoomPreset.Quarter;
            var label = new TextBlock
            {
                Text = day.DayLabel,
                Style = day.IsToday ? ChartHeaderAccentStyle : ChartHeaderCaptionStyle,
            };

            if (isQuarter)
            {
                label.FontSize = _vm.DayHeaderNumberFontSize;
                label.Width = day.Width;
                label.TextAlignment = TextAlignment.Center;
                Canvas.SetLeft(label, day.Left);
                Canvas.SetTop(label, 27);
            }
            else
            {
                Canvas.SetLeft(label, day.Left + 8);
                Canvas.SetTop(label, 24);
            }

            HeaderCanvas.Children.Add(label);
        }

        if (!double.IsNaN(_vm.TodayLineLeft))
        {
            var todayLine = new Rectangle
            {
                Width = 2,
                Height = HeaderHeight,
                Fill = ThemeBrush(ScheduleChartTheme.TodayLine),
            };
            Canvas.SetLeft(todayLine, _vm.TodayLineLeft - 1);
            HeaderCanvas.Children.Add(todayLine);
        }

        var bottomBorder = new Rectangle
        {
            Width = totalWidth,
            Height = 1,
            Fill = ThemeBrush(ScheduleChartTheme.Divider),
        };
        Canvas.SetTop(bottomBorder, HeaderHeight - 1);
        HeaderCanvas.Children.Add(bottomBorder);
    }

    private void RenderLaneGridOverlay()
    {
        LaneGridOverlay.Children.Clear();
        if (_vm is null || _vm.SwimlaneRows.Count == 0)
        {
            LaneHost.Height = double.NaN;
            LaneHost.MinHeight = 0;
            return;
        }

        var totalWidth = _vm.TimelineTotalWidth;
        var totalHeight = RowHeight * _vm.SwimlaneRows.Count;
        LaneGridOverlay.Width = totalWidth;
        LaneGridOverlay.Height = totalHeight;
        LaneHost.Width = totalWidth;
        LaneHost.Height = totalHeight;
        LaneHost.MinHeight = totalHeight;

        AddDayGridLines(LaneGridOverlay, totalHeight, includeMonthDividers: true);
    }

    private void AddWeekendColumns(Canvas canvas, double height)
    {
        if (_vm is null)
            return;

        foreach (var day in _vm.DayHeaders.Where(d => d.IsWeekend))
        {
            var weekend = new Rectangle
            {
                Width = day.Width,
                Height = height,
                Fill = ScheduleChartTheme.BandBackground(this),
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(weekend, day.Left);
            canvas.Children.Add(weekend);
        }
    }

    private void AddDayGridLines(Canvas canvas, double height, bool includeMonthDividers)
    {
        if (_vm is null)
            return;

        foreach (var day in _vm.DayHeaders)
        {
            var isMonthStart = includeMonthDividers && day.MonthLabel is not null;
            var line = new Rectangle
            {
                Width = isMonthStart ? 1 : 1,
                Height = height,
                Fill = isMonthStart
                    ? ThemeBrush(ScheduleChartTheme.GridMonthLine)
                    : ThemeBrush(ScheduleChartTheme.GridDayLine),
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(line, day.Left);
            canvas.Children.Add(line);
        }
    }

    private double ComputeMarkerLeft(DateOnly date)
    {
        if (_vm is null)
            return 0;

        var index = date.DayNumber - _vm.VisibleRangeStart.DayNumber;
        return index * _vm.TimelineDayWidth + _vm.TimelineDayWidth / 2;
    }

    private UIElement CreateHeaderLabel(string text) =>
        new Border
        {
            Height = HeaderHeight,
            Padding = new Thickness(8, 0, 8, 0),
            Background = ThemeBrush(ScheduleChartTheme.HeaderBackground),
            BorderBrush = ThemeBrush(ScheduleChartTheme.Divider),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new TextBlock
            {
                Text = text,
                Style = ChartHeaderCaptionStyle,
            },
        };

    private void GridScroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_suppressGridScrollChanged)
        {
            _suppressGridScrollChanged = false;
            UpdateScrollBarPadding();
            return;
        }

        SyncVerticalScroll(GridScroll, LabelsScroll);
        SyncHorizontalScroll(GridScroll, HeaderScroll);
        UpdateScrollBarPadding();
    }

    private void LabelsScroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_suppressLabelsScrollChanged)
        {
            _suppressLabelsScrollChanged = false;
            return;
        }

        SyncVerticalScroll(LabelsScroll, GridScroll);
    }

    private void HeaderScroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_suppressHeaderScrollChanged)
        {
            _suppressHeaderScrollChanged = false;
            return;
        }

        SyncHorizontalScroll(HeaderScroll, GridScroll);
    }

    private void SyncVerticalScroll(ScrollViewer source, ScrollViewer target)
    {
        var offset = source.VerticalOffset;
        if (Math.Abs(target.VerticalOffset - offset) < 0.5)
            return;

        if (target == LabelsScroll)
            _suppressLabelsScrollChanged = true;
        else if (target == GridScroll)
            _suppressGridScrollChanged = true;

        target.ChangeView(null, offset, null, disableAnimation: true);
    }

    private void SyncHorizontalScroll(ScrollViewer source, ScrollViewer target)
    {
        var offset = source.HorizontalOffset;
        if (Math.Abs(target.HorizontalOffset - offset) < 0.5)
            return;

        if (target == HeaderScroll)
            _suppressHeaderScrollChanged = true;
        else if (target == GridScroll)
            _suppressGridScrollChanged = true;

        target.ChangeView(offset, null, null, disableAnimation: true);
    }

    private void UpdateScrollBarPadding(bool force = false)
    {
        double rightPad = 0;
        if (GridScroll.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            rightPad = GetVerticalScrollbarWidth(GridScroll);

        if (!force && Math.Abs(_lastHeaderRightPad - rightPad) < 0.5)
            return;

        _lastHeaderRightPad = rightPad;
        HeaderBorder.Padding = new Thickness(0, 0, rightPad, 0);
    }

    private static double GetVerticalScrollbarWidth(ScrollViewer scrollViewer)
    {
        int count = VisualTreeHelper.GetChildrenCount(scrollViewer);
        for (int i = 0; i < count; i++)
        {
            if (FindVerticalScrollBar(VisualTreeHelper.GetChild(scrollViewer, i)) is ScrollBar bar
                && bar.ActualWidth > 0)
                return bar.ActualWidth;
        }

        return 12;
    }

    private static ScrollBar? FindVerticalScrollBar(DependencyObject node)
    {
        if (node is ScrollBar bar && bar.Orientation == Orientation.Vertical)
            return bar;

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            var found = FindVerticalScrollBar(VisualTreeHelper.GetChild(node, i));
            if (found is not null)
                return found;
        }

        return null;
    }

    private void LocationRow_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id } && _vm is not null)
            _vm.ToggleLocationCollapseCommand.Execute(id);
    }

    private void AssetLabel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid assetId } && _vm is not null)
            _vm.OpenAssetCommand.Execute(assetId);
    }

    private async void Marker_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ChartMarkerVm marker } || _vm is null)
            return;

        if (marker.EventCount > 1 && marker.SameDayEvents.Count > 1)
        {
            var flyout = new MenuFlyout();
            foreach (var item in marker.SameDayEvents)
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = $"{item.MaintenanceTypeLabel} · {item.StatusLabel}",
                    Tag = item.ScheduleId,
                };
                menuItem.Click += (_, _) => _vm.HighlightAndScrollToRowCommand.Execute(item.ScheduleId);
                flyout.Items.Add(menuItem);
            }

            if (sender is FrameworkElement fe)
                flyout.ShowAt(fe);
            return;
        }

        _vm.HighlightAndScrollToRowCommand.Execute(marker.ScheduleId);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_vm is null)
            return;

        if (e.Key == VirtualKey.Left && InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            _vm.NavigatePrevCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Right && InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            _vm.NavigateNextCommand.Execute(null);
            e.Handled = true;
        }
    }

    private Brush ResolveBrush(string key) =>
        StatusBadgePalette.TryParseToken(key, out _)
            ? StatusBadgePalette.ResolveBackgroundBrush(key, ActualTheme)
            : ThemeBrush(key);

    /// <summary>Переиспользуемые элементы lane-canvas при scroll recycle.</summary>
    private sealed class ChartLaneVisuals
    {
        private readonly Rectangle _rowFill;
        private readonly Rectangle _bottomBorder;
        private readonly List<Rectangle> _weekendRects = [];
        private readonly Canvas _canvas;
        private readonly MaintenanceScheduleTimelineControl _owner;

        private ChartLaneVisuals(Canvas canvas, MaintenanceScheduleTimelineControl owner)
        {
            _canvas = canvas;
            _owner = owner;
            _rowFill = new Rectangle { IsHitTestVisible = false };
            _bottomBorder = new Rectangle { Height = 1, IsHitTestVisible = false };
        }

        public static ChartLaneVisuals GetOrCreate(Canvas canvas, MaintenanceScheduleTimelineControl owner)
        {
            if (canvas.Tag is ChartLaneVisuals existing)
                return existing;

            var visuals = new ChartLaneVisuals(canvas, owner);
            canvas.Tag = visuals;
            return visuals;
        }

        public void Update(
            ChartLaneRowVm row,
            MaintenanceScheduleViewModel vm,
            double totalWidth,
            IReadOnlyList<ChartDayHeaderVm> dayHeaders)
        {
            _canvas.Width = totalWidth;
            _canvas.Height = RowHeight;

            _rowFill.Width = totalWidth;
            _rowFill.Height = RowHeight;
            _rowFill.Fill = _owner.GetRowBackground(row);

            _bottomBorder.Width = totalWidth;
            _bottomBorder.Fill = _owner.ThemeBrush(ScheduleChartTheme.Divider);

            var weekendCount = row.Kind == ChartLaneRowKind.Asset
                ? dayHeaders.Count(d => d.IsWeekend)
                : 0;
            EnsureWeekendRects(weekendCount);

            _canvas.Children.Clear();
            _canvas.Children.Add(_rowFill);

            if (row.Kind == ChartLaneRowKind.Asset)
            {
                var weekendIndex = 0;
                foreach (var day in dayHeaders)
                {
                    if (!day.IsWeekend)
                        continue;

                    var rect = _weekendRects[weekendIndex++];
                    rect.Width = day.Width;
                    rect.Height = RowHeight;
                    rect.Fill = ScheduleChartTheme.BandBackground(_owner);
                    Canvas.SetLeft(rect, day.Left);
                    _canvas.Children.Add(rect);
                }
            }

            Canvas.SetTop(_bottomBorder, RowHeight - 1);
            _canvas.Children.Add(_bottomBorder);

            if (row.Kind != ChartLaneRowKind.Asset)
                return;

            var markerSize = vm.MarkerSize;
            foreach (var marker in row.Markers)
            {
                var left = _owner.ComputeMarkerLeft(marker.PlannedDate) - markerSize / 2;
                var ellipse = new Ellipse
                {
                    Width = markerSize,
                    Height = markerSize,
                    Fill = _owner.ResolveBrush(marker.StatusBrushKey),
                    Tag = marker,
                };
                Canvas.SetLeft(ellipse, left);
                Canvas.SetTop(ellipse, (RowHeight - markerSize) / 2);
                ToolTipService.SetToolTip(
                    ellipse,
                    $"{marker.AssetName}\n{marker.MaintenanceTypeLabel} · {marker.PlannedDate:d}\n{marker.StatusLabel}");
                ellipse.PointerPressed += _owner.Marker_PointerPressed;
                _canvas.Children.Add(ellipse);

                if (marker.EventCount <= 1)
                    continue;

                var countStyle = StatusBadgeFactory.ForChartMarkerCount();
                var badge = new Border
                {
                    Background = StatusBadgePalette.ResolveBackgroundBrush(countStyle.BackgroundKey, _owner.ActualTheme),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(4, 1, 4, 1),
                    Child = new TextBlock
                    {
                        Text = $"×{marker.EventCount}",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = StatusBadgePalette.ResolveForegroundBrush(countStyle.ForegroundKey, _owner.ActualTheme),
                    },
                    Tag = marker,
                };
                Canvas.SetLeft(badge, left + markerSize - 2);
                Canvas.SetTop(badge, 2);
                badge.PointerPressed += _owner.Marker_PointerPressed;
                _canvas.Children.Add(badge);
            }
        }

        private void EnsureWeekendRects(int count)
        {
            while (_weekendRects.Count < count)
                _weekendRects.Add(new Rectangle { IsHitTestVisible = false });
        }
    }
}
