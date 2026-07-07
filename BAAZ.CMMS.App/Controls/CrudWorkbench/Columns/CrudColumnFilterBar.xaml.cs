using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudColumnFilterBar : UserControl
{
    private INotifyCollectionChanged? _filtersNotifier;

    public CrudColumnFilterBar()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLocalizedLabels();
    }

    public static readonly DependencyProperty ActiveFiltersProperty =
        DependencyProperty.Register(
            nameof(ActiveFilters),
            typeof(ObservableCollection<CrudColumnFilter>),
            typeof(CrudColumnFilterBar),
            new PropertyMetadata(null, OnActiveFiltersChanged));

    public ObservableCollection<CrudColumnFilter>? ActiveFilters
    {
        get => (ObservableCollection<CrudColumnFilter>?)GetValue(ActiveFiltersProperty);
        set => SetValue(ActiveFiltersProperty, value);
    }

    public static readonly DependencyProperty FilterColumnsProperty =
        DependencyProperty.Register(
            nameof(FilterColumns),
            typeof(System.Collections.Generic.IList<CrudColumnDefinition>),
            typeof(CrudColumnFilterBar),
            new PropertyMetadata(null));

    public System.Collections.Generic.IList<CrudColumnDefinition>? FilterColumns
    {
        get => (System.Collections.Generic.IList<CrudColumnDefinition>?)GetValue(FilterColumnsProperty);
        set => SetValue(FilterColumnsProperty, value);
    }

    public static readonly DependencyProperty AddFilterLabelProperty =
        DependencyProperty.Register(nameof(AddFilterLabel), typeof(string),
            typeof(CrudColumnFilterBar), new PropertyMetadata(string.Empty));

    public string AddFilterLabel
    {
        get => (string)GetValue(AddFilterLabelProperty);
        set => SetValue(AddFilterLabelProperty, value);
    }

    public static readonly DependencyProperty RemoveFilterLabelProperty =
        DependencyProperty.Register(nameof(RemoveFilterLabel), typeof(string),
            typeof(CrudColumnFilterBar), new PropertyMetadata(string.Empty));

    public string RemoveFilterLabel
    {
        get => (string)GetValue(RemoveFilterLabelProperty);
        set => SetValue(RemoveFilterLabelProperty, value);
    }

    public event EventHandler<EventArgs>? FiltersChanged;

    private void ApplyLocalizedLabels()
    {
        if (string.IsNullOrEmpty(AddFilterLabel))
            AddFilterLabel = ResourceStrings.Get("CrudFilter_AddFilter");
        if (string.IsNullOrEmpty(RemoveFilterLabel))
            RemoveFilterLabel = ResourceStrings.Get("CrudFilter_Remove");
    }

    private static void OnActiveFiltersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CrudColumnFilterBar bar) return;
        bar.DetachFiltersNotifier();
        bar._filtersNotifier = e.NewValue as INotifyCollectionChanged;
        if (bar._filtersNotifier is not null)
            bar._filtersNotifier.CollectionChanged += bar.OnFiltersCollectionChanged;

        if (e.NewValue is ObservableCollection<CrudColumnFilter> filters)
        {
            foreach (var filter in filters)
                filter.PropertyChanged += bar.OnFilterPropertyChanged;
        }

        bar.RebuildBadges();
    }

    private void DetachFiltersNotifier()
    {
        if (_filtersNotifier is not null)
            _filtersNotifier.CollectionChanged -= OnFiltersCollectionChanged;
        _filtersNotifier = null;
    }

    private void OnFiltersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CrudColumnFilter filter in e.NewItems)
                filter.PropertyChanged += OnFilterPropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (CrudColumnFilter filter in e.OldItems)
                filter.PropertyChanged -= OnFilterPropertyChanged;
        }

        RebuildBadges();
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CrudColumnFilter.Value)
            or nameof(CrudColumnFilter.DisplayValue)
            or nameof(CrudColumnFilter.ColumnHeader)
            or nameof(CrudColumnFilter.BadgeText))
        {
            RebuildBadges();
            if (e.PropertyName is nameof(CrudColumnFilter.Value) or nameof(CrudColumnFilter.DisplayValue))
                FiltersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RebuildBadges()
    {
        BadgesPanel.Children.Clear();
        if (ActiveFilters is null) return;

        foreach (var filter in ActiveFilters)
        {
            var badge = CreateBadge(filter);
            BadgesPanel.Children.Add(badge);
        }
    }

    private Border CreateBadge(CrudColumnFilter filter)
    {
        var text = new TextBlock
        {
            Text = filter.BadgeText,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var remove = new Button
        {
            Content = "\uE711",
            FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
            FontSize = 10,
            Padding = new Thickness(0),
            MinWidth = 20,
            MinHeight = 20,
            Width = 20,
            Height = 20,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = filter,
        };
        remove.Click += RemoveFilterButton_Click;

        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(text);
        content.Children.Add(remove);

        var border = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Tag = filter,
            Child = content,
        };
        border.PointerPressed += Badge_PointerPressed;
        return border;
    }

    private void AddFilterButton_Click(object sender, RoutedEventArgs e)
        => ShowFieldPicker(AddFilterButton);

    private void Badge_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not CrudColumnFilter filter) return;
        if (e.OriginalSource is Button) return;

        var col = FindColumn(filter.ColumnKey);
        if (col is not null)
            ShowValuePicker(col, filter, fe);
        e.Handled = true;
    }

    private void RemoveFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not CrudColumnFilter filter) return;
        ActiveFilters?.Remove(filter);
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ShowFieldPicker(FrameworkElement anchor)
    {
        var flyout = new MenuFlyout();
        var columns = FilterColumns?.Where(c => c.IsFilterable).ToList();
        if (columns is null || columns.Count == 0) return;

        foreach (var col in columns)
        {
            var item = new MenuFlyoutItem
            {
                Text = col.Header,
                Tag = col,
            };
            item.Click += (_, _) => ShowValuePicker(col, null, anchor);
            flyout.Items.Add(item);
        }

        flyout.ShowAt(anchor);
    }

    private void ShowValuePicker(CrudColumnDefinition col, CrudColumnFilter? existing, FrameworkElement anchor)
    {
        if (col.FilterKind == CrudColumnFilterKind.Bool)
            ShowBoolPicker(col, existing, anchor);
        else
            ShowTextPicker(col, existing, anchor);
    }

    private void ShowBoolPicker(CrudColumnDefinition col, CrudColumnFilter? existing, FrameworkElement anchor)
    {
        var flyout = new MenuFlyout();
        AddBoolItem(flyout, col, existing, true);
        AddBoolItem(flyout, col, existing, false);
        flyout.ShowAt(anchor);
    }

    private void AddBoolItem(MenuFlyout flyout, CrudColumnDefinition col, CrudColumnFilter? existing, bool value)
    {
        var label = value
            ? ResourceStrings.Get("CrudFilter_True")
            : ResourceStrings.Get("CrudFilter_False");
        var item = new MenuFlyoutItem { Text = label };
        item.Click += (_, _) => ApplyFilter(col, value.ToString().ToLowerInvariant(), label, existing);
        flyout.Items.Add(item);
    }

    private void ShowTextPicker(CrudColumnDefinition col, CrudColumnFilter? existing, FrameworkElement anchor)
    {
        var box = new TextBox
        {
            Width = 240,
            PlaceholderText = ResourceStrings.Get("CrudFilter_EnterValue"),
            Text = existing?.Value ?? string.Empty,
        };

        var apply = new Button
        {
            Content = ResourceStrings.Get("CrudFilter_Apply"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var panel = new StackPanel { Padding = new Thickness(12), Spacing = 0 };
        panel.Children.Add(new TextBlock
        {
            Text = col.Header,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        });
        panel.Children.Add(box);
        panel.Children.Add(apply);

        var flyout = new Flyout { Content = panel, Placement = FlyoutPlacementMode.Bottom };
        apply.Click += (_, _) =>
        {
            var text = box.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            ApplyFilter(col, text, text, existing);
            flyout.Hide();
        };

        box.KeyDown += (_, e) =>
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter) return;
            var text = box.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            ApplyFilter(col, text, text, existing);
            flyout.Hide();
            e.Handled = true;
        };

        flyout.ShowAt(anchor);
    }

    private void ApplyFilter(
        CrudColumnDefinition col,
        string value,
        string displayValue,
        CrudColumnFilter? existing)
    {
        if (ActiveFilters is null) return;

        if (existing is not null)
        {
            existing.Value = value;
            existing.DisplayValue = displayValue;
            existing.ColumnHeader = col.Header;
        }
        else
        {
            var dup = ActiveFilters.FirstOrDefault(f => f.ColumnKey == col.Key);
            if (dup is not null)
            {
                dup.Value = value;
                dup.DisplayValue = displayValue;
                dup.ColumnHeader = col.Header;
            }
            else
            {
                var filter = new CrudColumnFilter
                {
                    ColumnKey = col.Key,
                    ColumnHeader = col.Header,
                    Value = value,
                    DisplayValue = displayValue,
                };
                filter.PropertyChanged += OnFilterPropertyChanged;
                ActiveFilters.Add(filter);
            }
        }

        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private CrudColumnDefinition? FindColumn(string columnKey)
        => FilterColumns?.FirstOrDefault(c => c.Key == columnKey);
}
