using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using BAAZ.CMMS.App.Controls;
using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudDataGrid : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly CrudColumnWidthStore _widthStore = new();
    private Grid? _headerGrid;
    private Border? _headerBorder;
    private ScrollViewer? _bodyScrollViewer;
    private INotifyCollectionChanged? _columnsNotifier;
    private INotifyCollectionChanged? _itemsSourceNotifier;
    private bool _layoutSyncQueued;
    private bool _syncingHorizontalScroll;

    // Resize state
    private double _resizeStartX;
    private double _resizeStartWidth;
    private string? _resizingKey;
    private UIElement? _resizingHandle;

    /// <summary>Инверсия <see cref="IsLoading"/> — x:Bind не поддерживает оператор ! в этой версии WinUI.</summary>
    public bool IsNotLoading => !IsLoading;

    public CrudDataGrid()
    {
        InitializeComponent();
        _widthStore.WidthsChanged += OnWidthStoreChanged;

        RowsList.ContainerContentChanging += OnContainerContentChanging;
        Loaded += (_, _) =>
        {
            RebuildHeader();
            ScheduleLayoutSync();
            SyncListViewHeight();
        };
        SizeChanged += (_, _) =>
        {
            ScheduleLayoutSync();
            SyncListViewHeight();
        };
        ActualThemeChanged += (_, _) => ApplyGridLineTheme();
    }

    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable),
            typeof(CrudDataGrid), new PropertyMetadata(null, (d, e) =>
            {
                var g = (CrudDataGrid)d;
                g.AttachItemsSourceNotifier(e.NewValue);
                g.RowsList.ItemsSource = e.NewValue as IEnumerable;
                g.ScheduleLayoutSync();
            }));
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(ObservableCollection<CrudColumnDefinition>),
            typeof(CrudDataGrid), new PropertyMetadata(null, OnColumnsChanged));
    public ObservableCollection<CrudColumnDefinition>? Columns
    {
        get => (ObservableCollection<CrudColumnDefinition>?)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty IsAllSelectedProperty =
        DependencyProperty.Register(nameof(IsAllSelected), typeof(bool?),
            typeof(CrudDataGrid), new PropertyMetadata(false, (d, _) =>
                ((CrudDataGrid)d).OnIsAllSelectedChanged()));
    public bool? IsAllSelected
    {
        get => (bool?)GetValue(IsAllSelectedProperty);
        set => SetValue(IsAllSelectedProperty, value);
    }

    private void OnIsAllSelectedChanged()
    {
        if (_selectAllCheckBox is null) return;
        _syncingSelectAll = true;
        _selectAllCheckBox.IsChecked = IsAllSelected;
        _syncingSelectAll = false;
    }

    public static readonly DependencyProperty SortColumnKeyProperty =
        DependencyProperty.Register(nameof(SortColumnKey), typeof(string),
            typeof(CrudDataGrid), new PropertyMetadata(null, (d, _) =>
                ((CrudDataGrid)d).UpdateSortIndicators()));
    public string? SortColumnKey
    {
        get => (string?)GetValue(SortColumnKeyProperty);
        set => SetValue(SortColumnKeyProperty, value);
    }

    public static readonly DependencyProperty SortDirectionProperty =
        DependencyProperty.Register(nameof(SortDirection), typeof(SortDirection),
            typeof(CrudDataGrid), new PropertyMetadata(SortDirection.Ascending, (d, _) =>
                ((CrudDataGrid)d).UpdateSortIndicators()));
    public SortDirection SortDirection
    {
        get => (SortDirection)GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    public static readonly DependencyProperty ExpandRowToolTipProperty =
        DependencyProperty.Register(nameof(ExpandRowToolTip), typeof(string),
            typeof(CrudDataGrid), new PropertyMetadata(ResourceStrings.Get("CrudGrid_ExpandRow"), (d, _) =>
                ((CrudDataGrid)d).UpdateExpandToolTips()));

    public string ExpandRowToolTip
    {
        get => (string)GetValue(ExpandRowToolTipProperty);
        set => SetValue(ExpandRowToolTipProperty, value);
    }

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool),
            typeof(CrudDataGrid), new PropertyMetadata(false, OnIsLoadingChanged));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CrudDataGrid grid)
            grid.PropertyChanged?.Invoke(grid, new PropertyChangedEventArgs(nameof(IsNotLoading)));
    }

    // ── Events ───────────────────────────────────────────────────────────────

    public event EventHandler<EventArgs>? SelectAllClicked;
    public event EventHandler<CrudRowEventArgs>? RowExpandRequested;
    public event EventHandler<CrudRowEventArgs>? RowDoubleTapped;
    public event EventHandler<CrudCellContextEventArgs>? CellContextRequested;
    public event EventHandler<CrudCellContextEventArgs>? CellInlineEditRequested;
    public event EventHandler<CrudHeaderContextEventArgs>? HeaderContextRequested;
    public event EventHandler<CrudCellEditEventArgs>? CellEditCommitted;
    public event EventHandler<EventArgs>? RowSelectionChanged;

    // ── Columns changed ──────────────────────────────────────────────────────

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var g = (CrudDataGrid)d;

        if (g._columnsNotifier is not null)
            g._columnsNotifier.CollectionChanged -= g.OnColumnsCollectionChanged;

        g._columnsNotifier = e.NewValue as INotifyCollectionChanged;
        if (g._columnsNotifier is not null)
            g._columnsNotifier.CollectionChanged += g.OnColumnsCollectionChanged;

        g.RebuildHeader();
        g.RefreshAllRows();
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildHeader();
        RefreshAllRows();
    }

    // ── Header ───────────────────────────────────────────────────────────────

    private CompactCheckBox? _selectAllCheckBox;
    private bool _syncingSelectAll;
    private readonly Dictionary<string, TextBlock> _sortIndicators = new();

    private void RebuildHeader()
    {
        if (Columns is null) return;

        _sortIndicators.Clear();

        var headerBorder = new Border
        {
            BorderBrush = CrudGridTheme.GridLineBrush(this),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
        };
        _headerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        _headerGrid.SizeChanged += (_, _) => ScheduleLayoutSync();
        CrudColumnLayout.ApplyColumnDefinitions(_headerGrid.ColumnDefinitions, Columns, _widthStore);

        // Col 0: Select All
        _selectAllCheckBox = new CompactCheckBox
        {
            IsThreeState = true,
            IsChecked = IsAllSelected,
            SuppressToggle = true,
        };
        _selectAllCheckBox.Click += (_, _) =>
        {
            if (_syncingSelectAll) return;
            SelectAllClicked?.Invoke(this, EventArgs.Empty);
        };
        Grid.SetColumn(_selectAllCheckBox, 0);
        _headerGrid.Children.Add(_selectAllCheckBox);

        // Col 1: Expand placeholder (граница справа — отделяет служебные колонки от данных)
        var expandHeader = new Border
        {
            BorderBrush = CrudGridTheme.GridLineBrush(this),
            BorderThickness = new Thickness(0, 0, 1, 0),
        };
        Grid.SetColumn(expandHeader, 1);
        _headerGrid.Children.Add(expandHeader);

        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            var col = visible[i];
            int colIdx = CrudColumnLayout.GetDataColIndex(i);

            var cellGrid = new Grid();
            cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });

            // Header text + type label
            var titleContent = CrudColumnHeaderBuilder.BuildGridHeaderContent(col);
            titleContent.Margin = new Thickness(8, 4, 4, 4);
            Grid.SetColumn(titleContent, 0);
            cellGrid.Children.Add(titleContent);

            // Sort indicator
            var sortTb = new TextBlock
            {
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                Visibility = Visibility.Collapsed,
            };
            _sortIndicators[col.Key] = sortTb;
            Grid.SetColumn(sortTb, 1);
            cellGrid.Children.Add(sortTb);

            // Resize handle
            var handle = BuildResizeHandle(col.Key, colIdx);
            Grid.SetColumn(handle, 2);
            cellGrid.Children.Add(handle);

            // Context menu on header cell (click or right-click)
            var headerBorderCell = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = CrudGridTheme.GridLineBrush(this),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Tag = col.Key,
            };
            headerBorderCell.ContextRequested += (s, e) =>
            {
                e.Handled = true;
                HeaderContextRequested?.Invoke(this, new CrudHeaderContextEventArgs
                {
                    ColumnKey = col.Key,
                    HeaderElement = headerBorderCell,
                });
            };
            headerBorderCell.Child = cellGrid;

            Grid.SetColumn(headerBorderCell, colIdx);
            _headerGrid.Children.Add(headerBorderCell);
        }

        headerBorder.Child = _headerGrid;
        if (_headerBorder is not null)
            _headerBorder.SizeChanged -= OnHeaderSizeChanged;

        _headerBorder = headerBorder;
        headerBorder.SizeChanged += OnHeaderSizeChanged;
        HeaderScroll.Content = headerBorder;

        UpdateSortIndicators();
        ScheduleLayoutSync();
        SyncListViewHeight();
    }

    private void OnHeaderSizeChanged(object sender, SizeChangedEventArgs e) =>
        SyncListViewHeight();

    private CrudColumnResizeHandle BuildResizeHandle(string columnKey, int headerColIndex)
    {
        var handle = new CrudColumnResizeHandle
        {
            Width = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        handle.PointerPressed += (s, e) =>
        {
            _resizingKey = columnKey;
            _resizeStartX = e.GetCurrentPoint(this).Position.X;
            _resizeStartWidth = _headerGrid!.ColumnDefinitions[headerColIndex].ActualWidth;
            _resizingHandle = handle;
            handle.CapturePointer(e.Pointer);
        };

        handle.PointerMoved += (s, e) =>
        {
            if (_resizingKey != columnKey) return;
            double delta = e.GetCurrentPoint(this).Position.X - _resizeStartX;
            double newWidth = Math.Max(40, _resizeStartWidth + delta);
            ApplyLiveColumnWidth(columnKey, newWidth);
        };

        handle.PointerReleased += (s, e) => FinishColumnResize(columnKey, handle, e.Pointer);
        handle.PointerCanceled += (s, e) => FinishColumnResize(columnKey, handle, e.Pointer);

        return handle;
    }

    private void FinishColumnResize(string columnKey, CrudColumnResizeHandle handle, Pointer pointer)
    {
        if (_resizingKey != columnKey) return;

        if (_headerGrid is not null && Columns is not null)
        {
            var visible = Columns.Where(c => c.IsVisible).ToList();
            int i = visible.FindIndex(c => c.Key == columnKey);
            if (i >= 0)
            {
                int ci = CrudColumnLayout.GetDataColIndex(i);
                if (ci < _headerGrid.ColumnDefinitions.Count)
                {
                    var effectiveWidth = ResolveHeaderColumnWidth(ci, _headerGrid.ColumnDefinitions[ci].Width.Value);
                    _widthStore.Set(columnKey, effectiveWidth);
                    ApplyColumnWidthToVisibleRows(columnKey, effectiveWidth);
                }
            }
        }

        handle.ReleasePointerCapture(pointer);
        _resizingKey = null;
    }

    /// <summary>
    /// Задаёт ширину колонки заголовка и возвращает фактическую ширину после layout
    /// (заголовок может быть шире запроса из‑за минимума по содержимому).
    /// </summary>
    private double ResolveHeaderColumnWidth(int columnIndex, double requestedWidth)
    {
        if (_headerGrid is null || columnIndex >= _headerGrid.ColumnDefinitions.Count)
            return Math.Max(40, requestedWidth);

        requestedWidth = Math.Max(40, requestedWidth);
        _headerGrid.ColumnDefinitions[columnIndex].Width =
            new GridLength(requestedWidth, GridUnitType.Pixel);
        _headerGrid.UpdateLayout();

        var actual = _headerGrid.ColumnDefinitions[columnIndex].ActualWidth;
        return actual >= 1 ? Math.Max(40, actual) : requestedWidth;
    }

    private void ApplyColumnWidthToVisibleRows(string columnKey, double width)
    {
        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                row.ApplyColumnWidth(columnKey, width);
        }
    }

    /// <summary>Синхронно обновляет ширину колонки в заголовке и видимых строках (во время drag).</summary>
    private void ApplyLiveColumnWidth(string columnKey, double width)
    {
        if (Columns is null || _headerGrid is null) return;

        var visible = Columns.Where(c => c.IsVisible).ToList();
        int i = visible.FindIndex(c => c.Key == columnKey);
        if (i < 0) return;

        int ci = CrudColumnLayout.GetDataColIndex(i);
        if (ci >= _headerGrid.ColumnDefinitions.Count) return;

        var effectiveWidth = ResolveHeaderColumnWidth(ci, width);
        ApplyColumnWidthToVisibleRows(columnKey, effectiveWidth);
    }

    private void UpdateSortIndicators()
    {
        foreach (var (key, tb) in _sortIndicators)
        {
            if (key == SortColumnKey)
            {
                tb.Text = SortDirection == SortDirection.Ascending ? "↑" : "↓";
                tb.Visibility = Visibility.Visible;
            }
            else
            {
                tb.Visibility = Visibility.Collapsed;
            }
        }
    }

    // ── Row virtualisation ───────────────────────────────────────────────────

    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        if (e.ItemContainer.ContentTemplateRoot is not CrudDataGridRow row)
            return;

        // First time: wire events
        if (e.ItemContainer.Tag is null)
        {
            e.ItemContainer.Tag = true;
            row.ExpandRequested += Row_ExpandRequested;
            row.RowDoubleTapped += Row_RowDoubleTapped;
            row.CellContextRequested += Row_CellContextRequested;
            row.CellInlineEditRequested += Row_CellInlineEditRequested;
            row.SelectionChanged += Row_SelectionChanged;
            row.CellEditCommitted += Row_CellEditCommitted;
        }

        // Always set data
        row.Row = e.Item as ICrudGridRow;
        row.SetExpandToolTip(ExpandRowToolTip);
        row.ApplyColumns(Columns, _widthStore, _columnLayoutGeneration);
        ApplyHeaderWidthsToRow(row);
    }

    private void UpdateExpandToolTips()
    {
        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                row.SetExpandToolTip(ExpandRowToolTip);
        }
    }

    private void RefreshAllRows()
    {
        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                row.ApplyColumns(Columns, _widthStore, _columnLayoutGeneration);
        }
    }

    private void AttachItemsSourceNotifier(object? source)
    {
        if (_itemsSourceNotifier is not null)
            _itemsSourceNotifier.CollectionChanged -= OnItemsSourceCollectionChanged;

        _itemsSourceNotifier = source as INotifyCollectionChanged;
        if (_itemsSourceNotifier is not null)
            _itemsSourceNotifier.CollectionChanged += OnItemsSourceCollectionChanged;
    }

    private bool _syncVisibleRowDataQueued;

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ScheduleSyncVisibleRowData();

    /// <summary>
    /// ListView при виртуализации не всегда перепривязывает строку после Replace в коллекции.
    /// </summary>
    public void RefreshVisibleRowData() => SyncVisibleRowData(forceRebuild: false);

    private void ScheduleSyncVisibleRowData()
    {
        if (_syncVisibleRowDataQueued)
            return;

        _syncVisibleRowDataQueued = true;
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            _syncVisibleRowDataQueued = false;
            SyncVisibleRowData(forceRebuild: false);
        });
    }

    private void SyncVisibleRowData(bool forceRebuild = false)
    {
        foreach (var container in GetVisibleContainers())
        {
            var index = RowsList.IndexFromContainer(container);
            if (index < 0) continue;

            var item = index < RowsList.Items.Count ? RowsList.Items[index] as ICrudGridRow : null;
            if (container.ContentTemplateRoot is CrudDataGridRow gridRow)
            {
                if (forceRebuild)
                    gridRow.RebuildCellsPublic();
                else
                    gridRow.Row = item;
            }
        }
    }

    private IEnumerable<ListViewItem> GetVisibleContainers()
    {
        for (int i = 0; i < RowsList.Items.Count; i++)
        {
            if (RowsList.ContainerFromIndex(i) is ListViewItem item)
                yield return item;
        }
    }

    private int _columnLayoutGeneration;

    /// <summary>
    /// Пересобрать заголовок и обновить все строки после изменения IsVisible у колонок.
    /// </summary>
    public void RebuildAfterColumnsChanged()
    {
        _columnLayoutGeneration++;
        RebuildHeader();
        RefreshAllRows();
    }

    /// <summary>Открыть inline-редактор ячейки (dbl-click или контекстное меню).</summary>
    public void BeginInlineEdit(
        ICrudGridRow row,
        string columnKey,
        Func<string?, string?>? validate = null)
    {
        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow gridRow
                && gridRow.Row?.Id == row.Id)
            {
                gridRow.BeginInlineEdit(columnKey, validate);
                return;
            }
        }
    }

    private void OnWidthStoreChanged(object? sender, EventArgs e)
    {
        // Update header column definitions
        if (_headerGrid is not null && Columns is not null)
        {
            var visible = Columns.Where(c => c.IsVisible).ToList();
            for (int i = 0; i < visible.Count; i++)
            {
                int ci = CrudColumnLayout.GetDataColIndex(i);
                if (ci < _headerGrid.ColumnDefinitions.Count)
                {
                    double defaultW = visible[i].GetEffectiveWidth();
                    double w = _widthStore.Get(visible[i].Key, defaultW);
                    _headerGrid.ColumnDefinitions[ci].Width = new GridLength(w, GridUnitType.Pixel);
                }
            }
        }

        // Update visible rows
        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                row.ApplyColumnWidths(_widthStore);
        }
    }

    // ── Row event forwarders ─────────────────────────────────────────────────

    private void Row_ExpandRequested(object? sender, CrudRowEventArgs e) =>
        RowExpandRequested?.Invoke(this, e);

    private void Row_RowDoubleTapped(object? sender, CrudRowEventArgs e) =>
        RowDoubleTapped?.Invoke(this, e);

    private void Row_CellContextRequested(object? sender, CrudCellContextEventArgs e) =>
        CellContextRequested?.Invoke(this, e);

    private void Row_CellInlineEditRequested(object? sender, CrudCellContextEventArgs e) =>
        CellInlineEditRequested?.Invoke(this, e);

    private void Row_SelectionChanged(object? sender, RoutedEventArgs e) =>
        RowSelectionChanged?.Invoke(this, EventArgs.Empty);

    private void Row_CellEditCommitted(object? sender, CrudCellEditEventArgs e) =>
        CellEditCommitted?.Invoke(this, e);

    // ── Header/body alignment ─────────────────────────────────────────────────

    private void ScheduleLayoutSync()
    {
        if (_resizingKey is not null || _layoutSyncQueued) return;
        _layoutSyncQueued = true;
        DispatcherQueue.TryEnqueue(() =>
            DispatcherQueue.TryEnqueue(() =>
            {
                _layoutSyncQueued = false;
                SyncHeaderWithBody();
            }));
    }

    private void SyncListViewHeight()
    {
        if (ActualHeight <= 0) return;

        var headerHeight = _headerBorder?.ActualHeight ?? HeaderScroll.ActualHeight;
        var bodyHeight = Math.Max(0, ActualHeight - headerHeight);
        RowsList.MaxHeight = bodyHeight;
        ScheduleLayoutSync();
    }

    private void SyncHeaderWithBody()
    {
        UpdateScrollBarPadding();
        SyncColumnWidthsFromHeader();
    }

    private void ApplyGridLineTheme()
    {
        var brush = CrudGridTheme.GridLineBrush(this);

        if (_headerBorder is not null)
            _headerBorder.BorderBrush = brush;

        if (_headerGrid is not null)
        {
            foreach (var child in _headerGrid.Children)
            {
                if (child is Border border && border.BorderThickness != new Thickness(0))
                    border.BorderBrush = brush;
            }
        }

        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                row.ApplyGridLineTheme();
        }
    }

    private void EnsureBodyScrollViewer()
    {
        if (_bodyScrollViewer is not null) return;
        _bodyScrollViewer = FindDescendant<ScrollViewer>(RowsList);
        if (_bodyScrollViewer is not null)
        {
            _bodyScrollViewer.ViewChanged += (_, _) =>
            {
                ScheduleLayoutSync();
                SyncHorizontalScroll(_bodyScrollViewer, HeaderScroll);
            };
        }
    }

    private void HeaderScroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        EnsureBodyScrollViewer();
        if (_bodyScrollViewer is not null)
            SyncHorizontalScroll(HeaderScroll, _bodyScrollViewer);
    }

    private void SyncHorizontalScroll(ScrollViewer source, ScrollViewer target)
    {
        if (_syncingHorizontalScroll)
            return;

        _syncingHorizontalScroll = true;
        target.ChangeView(source.HorizontalOffset, null, null, disableAnimation: true);
        _syncingHorizontalScroll = false;
    }

    private void UpdateScrollBarPadding()
    {
        if (_headerBorder is null) return;
        EnsureBodyScrollViewer();
        if (_bodyScrollViewer is null) return;

        double rightPad = 0;
        if (_bodyScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            rightPad = GetVerticalScrollbarWidth(_bodyScrollViewer);

        double bottomPad = 0;
        if (_bodyScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
            bottomPad = GetHorizontalScrollbarHeight(_bodyScrollViewer);

        _headerBorder.Padding = new Thickness(0, 0, rightPad, 0);
        _bodyScrollViewer.Padding = new Thickness(0, 0, rightPad, bottomPad);
    }

    private static double GetHorizontalScrollbarHeight(ScrollViewer scrollViewer)
    {
        int count = VisualTreeHelper.GetChildrenCount(scrollViewer);
        for (int i = 0; i < count; i++)
        {
            if (FindHorizontalScrollBar(VisualTreeHelper.GetChild(scrollViewer, i)) is ScrollBar bar
                && bar.ActualHeight > 0)
                return bar.ActualHeight;
        }

        return 12;
    }

    private static ScrollBar? FindHorizontalScrollBar(DependencyObject node)
    {
        if (node is ScrollBar bar && bar.Orientation == Orientation.Horizontal)
            return bar;

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            var found = FindHorizontalScrollBar(VisualTreeHelper.GetChild(node, i));
            if (found is not null) return found;
        }

        return null;
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
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    /// После layout переносит фактические ширины из заголовка в store и строки.
    /// </summary>
    private void SyncColumnWidthsFromHeader()
    {
        if (_headerGrid is null || Columns is null) return;

        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            int ci = CrudColumnLayout.GetDataColIndex(i);
            if (ci >= _headerGrid.ColumnDefinitions.Count) continue;

            double actual = _headerGrid.ColumnDefinitions[ci].ActualWidth;
            if (actual < 1) continue;

            double fallback = visible[i].GetEffectiveWidth();
            double stored = _widthStore.Get(visible[i].Key, fallback);
            if (Math.Abs(actual - stored) > 0.5)
                _widthStore.Set(visible[i].Key, actual);
        }

        foreach (var container in GetVisibleContainers())
        {
            if (container.ContentTemplateRoot is CrudDataGridRow row)
                ApplyHeaderWidthsToRow(row);
        }
    }

    /// <summary>Строка должна совпадать с фактической шириной колонок заголовка после layout.</summary>
    private void ApplyHeaderWidthsToRow(CrudDataGridRow row)
    {
        if (_headerGrid is null || Columns is null) return;

        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            int ci = CrudColumnLayout.GetDataColIndex(i);
            if (ci >= _headerGrid.ColumnDefinitions.Count) continue;

            double actual = _headerGrid.ColumnDefinitions[ci].ActualWidth;
            if (actual >= 1)
                row.ApplyColumnWidth(visible[i].Key, actual);
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                return match;
            var nested = FindDescendant<T>(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
