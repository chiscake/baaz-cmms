using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using BAAZ.CMMS.App.Controls;
using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudDataGridRow : UserControl
{
    private const int MultilineCellMaxLines = 4;

    private INotifyPropertyChanged? _rowNotifier;
    private CompactCheckBox? _rowCheckBox;
    private bool _syncingCheckBox;
    private Button? _expandBtn;
    private Border? _expandColumnHost;
    private readonly List<Border> _dataCells = [];

    public CrudDataGridRow()
    {
        InitializeComponent();
        RowGrid.PointerEntered += (_, _) => SetExpandVisible(true);
        RowGrid.PointerExited += (_, _) => SetExpandVisible(false);
        ActualThemeChanged += (_, _) => ApplyGridLineTheme();
    }

    private void SetExpandVisible(bool visible)
    {
        if (_expandBtn is not null)
            _expandBtn.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Button CreateExpandButton()
    {
        const double size = 22;

        var btn = new Button
        {
            Width = size,
            Height = size,
            MinWidth = 0,
            MinHeight = 0,
            MaxWidth = size,
            MaxHeight = size,
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Colors.Transparent),
            Content = new FontIcon { Glyph = "\uE740", FontSize = 10 },
        };

        // Сброс дефолтного MinWidth (~130px) из шаблона Button
        var compactStyle = new Style(typeof(Button));
        compactStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 0.0));
        compactStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 0.0));
        compactStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        compactStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        btn.Style = compactStyle;

        ToolTipService.SetToolTip(btn, ResourceStrings.Get("CrudGrid_ExpandRow"));

        var hoverBg = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
        btn.PointerEntered += (_, _) => btn.Background = hoverBg;
        btn.PointerExited += (_, _) => btn.Background = new SolidColorBrush(Colors.Transparent);

        return btn;
    }

    public void SetExpandToolTip(string toolTip)
    {
        if (_expandBtn is not null)
            ToolTipService.SetToolTip(_expandBtn, toolTip);
    }

    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty RowProperty =
        DependencyProperty.Register(nameof(Row), typeof(ICrudGridRow),
            typeof(CrudDataGridRow), new PropertyMetadata(null, (d, e) =>
            {
                var r = (CrudDataGridRow)d;
                r.DetachRowNotifier();
                if (e.OldValue is ICrudGridRow oldRow
                    && e.NewValue is ICrudGridRow newRow
                    && oldRow.Id == newRow.Id
                    && r._dataCells.Count > 0)
                {
                    r.RefreshCellDisplay();
                }
                else
                {
                    r.RebuildCells();
                }

                r.AttachRowNotifier();
            }));
    public ICrudGridRow? Row { get => (ICrudGridRow?)GetValue(RowProperty); set => SetValue(RowProperty, value); }

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(IList<CrudColumnDefinition>),
            typeof(CrudDataGridRow), new PropertyMetadata(null, (d, _) =>
                ((CrudDataGridRow)d).RebuildCells()));
    public IList<CrudColumnDefinition>? Columns
    {
        get => (IList<CrudColumnDefinition>?)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty WidthStoreProperty =
        DependencyProperty.Register(nameof(WidthStore), typeof(CrudColumnWidthStore),
            typeof(CrudDataGridRow), new PropertyMetadata(null, (d, _) =>
                ((CrudDataGridRow)d).RebuildCells()));
    public CrudColumnWidthStore? WidthStore
    {
        get => (CrudColumnWidthStore?)GetValue(WidthStoreProperty);
        set => SetValue(WidthStoreProperty, value);
    }

    private int _lastColumnLayoutGeneration = -1;

    /// <summary>Применить колонки и пересобрать ячейки при смене видимости/состава.</summary>
    internal void ApplyColumns(
        IList<CrudColumnDefinition>? columns,
        CrudColumnWidthStore? widthStore,
        int layoutGeneration)
    {
        Columns = columns;
        WidthStore = widthStore;

        if (layoutGeneration != _lastColumnLayoutGeneration)
        {
            _lastColumnLayoutGeneration = layoutGeneration;
            RebuildCells();
        }
    }

    // ── Events ───────────────────────────────────────────────────────────────

    public event EventHandler<CrudRowEventArgs>? ExpandRequested;
    public event EventHandler<RoutedEventArgs>? SelectionChanged;
    public event EventHandler<CrudRowEventArgs>? RowDoubleTapped;
    public event EventHandler<CrudCellContextEventArgs>? CellContextRequested;
    public event EventHandler<CrudCellContextEventArgs>? CellInlineEditRequested;
    public event EventHandler<CrudCellEditEventArgs>? CellEditCommitted;

    // ── Build ────────────────────────────────────────────────────────────────

    internal void RebuildCellsPublic() => RebuildCells();

    private void RebuildCells()
    {
        _dataCells.Clear();
        RowGrid.Children.Clear();
        RowGrid.ColumnDefinitions.Clear();
        _expandBtn = null;
        _rowCheckBox = null;

        if (Row is null || Columns is null)
            return;

        CrudColumnLayout.ApplyColumnDefinitions(RowGrid.ColumnDefinitions, Columns, WidthStore);

        // Col 0: Checkbox
        var canSelect = Row is not ICrudSelectableRow selectable || selectable.IsSelectable;
        _rowCheckBox = new CompactCheckBox { IsChecked = Row.IsSelected, IsEnabled = canSelect };
        if (!canSelect)
            _rowCheckBox.Opacity = 0.35;
        _rowCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (_syncingCheckBox || Row is null || _rowCheckBox is null) return;
            if (Row is ICrudSelectableRow { IsSelectable: false })
                return;
            Row.IsSelected = _rowCheckBox.IsChecked == true;
            SelectionChanged?.Invoke(this, new RoutedEventArgs());
        };
        var selectHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(Colors.Transparent),
            Child = _rowCheckBox,
        };
        Grid.SetColumn(selectHost, 0);
        RowGrid.Children.Add(selectHost);

        // Col 1: Expand — компактная квадратная кнопка по центру
        _expandBtn = CreateExpandButton();
        _expandBtn.Click += (_, _) =>
        {
            if (Row is not null)
                ExpandRequested?.Invoke(this, new CrudRowEventArgs { Row = Row, Source = _expandBtn });
        };

        _expandColumnHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = CrudGridTheme.GridLineBrush(this),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(0),
            Child = _expandBtn,
        };
        Grid.SetColumn(_expandColumnHost, 1);
        RowGrid.Children.Add(_expandColumnHost);

        // Data cells
        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            var col = visible[i];
            int colIdx = CrudColumnLayout.GetDataColIndex(i);

            var cellBorder = BuildCell(col, i);
            Grid.SetColumn(cellBorder, colIdx);
            RowGrid.Children.Add(cellBorder);
            _dataCells.Add(cellBorder);
        }

        RowGrid.BorderBrush = CrudGridTheme.GridLineBrush(this);
        RowGrid.BorderThickness = new Thickness(0, 0, 0, 1);

        ApplySelectionBackground();
    }

    public void ApplyGridLineTheme()
    {
        var brush = CrudGridTheme.GridLineBrush(this);
        RowGrid.BorderBrush = brush;

        if (_expandColumnHost is not null)
            _expandColumnHost.BorderBrush = brush;

        foreach (var cell in _dataCells)
            cell.BorderBrush = brush;
    }

    /// <summary>Перерисовать текст ячеек из текущего <see cref="Row"/> без пересборки разметки.</summary>
    public void RefreshCellDisplay()
    {
        if (Row is null || Columns is null)
        {
            RebuildCells();
            return;
        }

        if (_dataCells.Count == 0)
        {
            RebuildCells();
            return;
        }

        var visible = Columns.Where(c => c.IsVisible).ToList();
        if (visible.Count != _dataCells.Count)
        {
            RebuildCells();
            return;
        }

        for (int i = 0; i < visible.Count; i++)
        {
            var col = visible[i];
            var cellBorder = _dataCells[i];
            if ((string?)cellBorder.Tag != col.Key)
            {
                RebuildCells();
                return;
            }

            UpdateCellText(cellBorder, col);
        }

        if (_rowCheckBox is not null)
            _rowCheckBox.IsChecked = Row.IsSelected;

        ApplySelectionBackground();
    }

    private void UpdateCellText(Border cellBorder, CrudColumnDefinition col)
    {
        var cellText = Row!.GetCellText(col.Key) ?? string.Empty;
        var displayText = col.Key == "LocationScopes"
            ? TruncateMultilinePreview(cellText, MultilineCellMaxLines)
            : cellText;

        if (cellBorder.Child is TextBlock tb)
        {
            tb.Text = displayText;
            tb.Opacity = Row.IsActive ? 1.0 : 0.5;
        }

        if (!string.IsNullOrEmpty(cellText))
            ToolTipService.SetToolTip(cellBorder, cellText);
        else
            ToolTipService.SetToolTip(cellBorder, null);
    }

    private Border BuildCell(CrudColumnDefinition col, int dataIndex)
    {
        var cellText = Row!.GetCellText(col.Key) ?? string.Empty;
        var isMultilineScope = col.Key == "LocationScopes";
        var displayText = isMultilineScope
            ? TruncateMultilinePreview(cellText, MultilineCellMaxLines)
            : cellText;
        var text = new TextBlock
        {
            Text = displayText,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = isMultilineScope ? VerticalAlignment.Top : VerticalAlignment.Center,
            Opacity = Row.IsActive ? 1.0 : 0.5,
        };

        if (col.FilterKind == CrudColumnFilterKind.Bool || col.DataTypeLabel == "bool")
            text.HorizontalAlignment = HorizontalAlignment.Center;

        var cellBorder = new Border
        {
            Padding = new Thickness(8, 6, 8, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = CrudGridTheme.GridLineBrush(this),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Tag = col.Key,
            Child = text,
        };

        if (!string.IsNullOrEmpty(cellText))
            ToolTipService.SetToolTip(cellBorder, cellText);

        // Double-click → inline-flyout для редактируемых ячеек, иначе панель редактора
        cellBorder.DoubleTapped += (_, e) =>
        {
            if (Row is null) return;
            e.Handled = true;
            var colKey = (string?)cellBorder.Tag;
            var col = Columns?.FirstOrDefault(c => c.Key == colKey);
            if (col is { IsInlineEditable: true, IsComputed: false })
            {
                CellInlineEditRequested?.Invoke(this, new CrudCellContextEventArgs
                {
                    Row = Row,
                    ColumnKey = colKey ?? string.Empty,
                    CellElement = cellBorder,
                });
            }
            else
            {
                RowDoubleTapped?.Invoke(this, new CrudRowEventArgs { Row = Row, Source = cellBorder });
            }
        };

        // Context menu
        cellBorder.ContextRequested += (_, e) =>
        {
            if (Row is null) return;
            e.Handled = true;
            CellContextRequested?.Invoke(this, new CrudCellContextEventArgs
            {
                Row = Row,
                ColumnKey = col.Key,
                CellElement = cellBorder,
            });
        };

        return cellBorder;
    }

    /// <summary>
    /// Обрезает многострочный текст: если строк больше <paramref name="maxLines"/>,
    /// показывает (maxLines − 1) строк и «…» на последней.
    /// </summary>
    private static string TruncateMultilinePreview(string text, int maxLines)
    {
        if (string.IsNullOrEmpty(text) || maxLines < 2)
            return text;

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0)
            .ToList();

        if (lines.Count <= maxLines)
            return string.Join(Environment.NewLine, lines);

        var preview = lines.Take(maxLines - 1).Append("...").ToList();
        return string.Join(Environment.NewLine, preview);
    }

    // ── Inline editor (контекстное меню «Редактировать ячейку») ───────────────

    internal void BeginInlineEdit(string columnKey, Func<string?, string?>? validate = null)
    {
        if (Row is null || Columns is null) return;
        var col = Columns.FirstOrDefault(c => c.Key == columnKey);
        if (col is null || !col.IsInlineEditable) return;

        var cell = _dataCells.FirstOrDefault(c => (string?)c.Tag == columnKey);
        if (cell?.Child is not TextBlock) return;

        ShowInlineEditor(cell, col, validate);
    }

    private void ShowInlineEditor(
        Border anchor,
        CrudColumnDefinition col,
        Func<string?, string?>? validate)
    {
        if (Row is null) return;

        string? originalValue = Row.GetCellEditValue(col.Key);
        CrudCellEditFlyout.Show(anchor, col, originalValue, async newValue =>
        {
            if (newValue == originalValue) return;
            CellEditCommitted?.Invoke(this, new CrudCellEditEventArgs
            {
                Row = Row,
                ColumnKey = col.Key,
                OldValue = originalValue,
                NewValue = newValue,
            });
        }, validate);
    }

    // ── Width update (without rebuild) ───────────────────────────────────────

    public void ApplyColumnWidths(CrudColumnWidthStore store)
    {
        if (Columns is null) return;
        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            int ci = CrudColumnLayout.GetDataColIndex(i);
            if (ci < RowGrid.ColumnDefinitions.Count)
            {
                double defaultW = visible[i].GetEffectiveWidth();
                double w = store.Get(visible[i].Key, defaultW);
                RowGrid.ColumnDefinitions[ci].Width = new GridLength(w, GridUnitType.Pixel);
            }
        }
    }

    /// <summary>Обновить ширину одной колонки без пересборки ячеек (live resize).</summary>
    public void ApplyColumnWidth(string columnKey, double width)
    {
        if (Columns is null) return;
        var visible = Columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            if (visible[i].Key != columnKey) continue;
            int ci = CrudColumnLayout.GetDataColIndex(i);
            if (ci < RowGrid.ColumnDefinitions.Count)
                RowGrid.ColumnDefinitions[ci].Width = new GridLength(Math.Max(40, width), GridUnitType.Pixel);
            break;
        }
    }

    // ── Selection state ───────────────────────────────────────────────────────

    private static readonly SolidColorBrush SelectedBrush =
        new(global::Windows.UI.Color.FromArgb(0x22, 0x60, 0xAA, 0xFF));

    private void ApplySelectionBackground()
    {
        var bg = Row?.IsSelected == true
            ? SelectedBrush
            : new SolidColorBrush(Colors.Transparent);

        foreach (var cell in _dataCells)
            cell.Background = bg;
    }

    // ── Row property change listener ─────────────────────────────────────────

    private void AttachRowNotifier()
    {
        if (Row is INotifyPropertyChanged npc)
        {
            _rowNotifier = npc;
            _rowNotifier.PropertyChanged += OnRowPropertyChanged;
        }
    }

    private void DetachRowNotifier()
    {
        if (_rowNotifier is not null)
        {
            _rowNotifier.PropertyChanged -= OnRowPropertyChanged;
            _rowNotifier = null;
        }
        _rowCheckBox = null;
        _expandBtn = null;
        _expandColumnHost = null;
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ICrudRow.IsSelected):
                if (_rowCheckBox is not null && Row is not null)
                {
                    _syncingCheckBox = true;
                    _rowCheckBox.IsChecked = Row.IsSelected;
                    _syncingCheckBox = false;
                }
                ApplySelectionBackground();
                break;
        }
    }
}
