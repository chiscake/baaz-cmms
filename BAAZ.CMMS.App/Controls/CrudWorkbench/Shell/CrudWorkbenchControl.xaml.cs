using System;
using System.Collections.ObjectModel;
using System.Linq;

using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using VirtualKey = global::Windows.System.VirtualKey;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudWorkbenchControl : UserControl
{
    private double _lastTableHostMaxHeight = -1;

    public CrudWorkbenchControl()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncTableHostHeight();
        SizeChanged += (_, _) => SyncTableHostHeight();
        LayoutUpdated += (_, _) => SyncTableHostHeight();
    }

    private void SyncTableHostHeight()
    {
        if (ActualHeight <= 0) return;

        var chrome = ToolbarBar.ActualHeight
                     + FilterRow.ActualHeight
                     + (ColumnFilterBar.Visibility == Visibility.Visible ? ColumnFilterBar.ActualHeight : 0)
                     + Paginator.ActualHeight
                     + Paginator.Margin.Top
                     + Paginator.Margin.Bottom;

        var tableHeight = Math.Max(120, ActualHeight - chrome);
        if (Math.Abs(tableHeight - _lastTableHostMaxHeight) < 0.5)
            return;

        _lastTableHostMaxHeight = tableHeight;
        TableHost.MaxHeight = tableHeight;
    }

    public static readonly DependencyProperty TableContentProperty =
        DependencyProperty.Register(nameof(TableContent), typeof(object),
            typeof(CrudWorkbenchControl), new PropertyMetadata(null));
    public object? TableContent { get => GetValue(TableContentProperty); set => SetValue(TableContentProperty, value); }

    public static readonly DependencyProperty AddLabelProperty =
        DependencyProperty.Register(nameof(AddLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string AddLabel { get => (string)GetValue(AddLabelProperty); set => SetValue(AddLabelProperty, value); }

    public static readonly DependencyProperty RefreshLabelProperty =
        DependencyProperty.Register(nameof(RefreshLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string RefreshLabel { get => (string)GetValue(RefreshLabelProperty); set => SetValue(RefreshLabelProperty, value); }

    public static readonly DependencyProperty ArchiveLabelProperty =
        DependencyProperty.Register(nameof(ArchiveLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string ArchiveLabel { get => (string)GetValue(ArchiveLabelProperty); set => SetValue(ArchiveLabelProperty, value); }

    public static readonly DependencyProperty ColumnsLabelProperty =
        DependencyProperty.Register(nameof(ColumnsLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string ColumnsLabel { get => (string)GetValue(ColumnsLabelProperty); set => SetValue(ColumnsLabelProperty, value); }

    public static readonly DependencyProperty FilterPlaceholderProperty =
        DependencyProperty.Register(nameof(FilterPlaceholder), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string FilterPlaceholder { get => (string)GetValue(FilterPlaceholderProperty); set => SetValue(FilterPlaceholderProperty, value); }

    public static readonly DependencyProperty SearchLabelProperty =
        DependencyProperty.Register(nameof(SearchLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string SearchLabel { get => (string)GetValue(SearchLabelProperty); set => SetValue(SearchLabelProperty, value); }

    public static readonly DependencyProperty ShowInactiveLabelProperty =
        DependencyProperty.Register(nameof(ShowInactiveLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string ShowInactiveLabel { get => (string)GetValue(ShowInactiveLabelProperty); set => SetValue(ShowInactiveLabelProperty, value); }

    public static readonly DependencyProperty FilterTextProperty =
        DependencyProperty.Register(nameof(FilterText), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string FilterText { get => (string)GetValue(FilterTextProperty); set => SetValue(FilterTextProperty, value); }

    public static readonly DependencyProperty ShowInactiveProperty =
        DependencyProperty.Register(nameof(ShowInactive), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool ShowInactive { get => (bool)GetValue(ShowInactiveProperty); set => SetValue(ShowInactiveProperty, value); }

    public static readonly DependencyProperty HasShowInactiveProperty =
        DependencyProperty.Register(nameof(HasShowInactive), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(true));
    public bool HasShowInactive { get => (bool)GetValue(HasShowInactiveProperty); set => SetValue(HasShowInactiveProperty, value); }

    public static readonly DependencyProperty CanCreateProperty =
        DependencyProperty.Register(nameof(CanCreate), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool CanCreate { get => (bool)GetValue(CanCreateProperty); set => SetValue(CanCreateProperty, value); }

    public static readonly DependencyProperty ShowArchiveButtonProperty =
        DependencyProperty.Register(nameof(ShowArchiveButton), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false, OnSelectionActionVisibilityChanged));
    public bool ShowArchiveButton { get => (bool)GetValue(ShowArchiveButtonProperty); set => SetValue(ShowArchiveButtonProperty, value); }

    public static readonly DependencyProperty CanArchiveProperty =
        DependencyProperty.Register(nameof(CanArchive), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool CanArchive { get => (bool)GetValue(CanArchiveProperty); set => SetValue(CanArchiveProperty, value); }

    public static readonly DependencyProperty HardDeleteLabelProperty =
        DependencyProperty.Register(nameof(HardDeleteLabel), typeof(string),
            typeof(CrudWorkbenchControl), new PropertyMetadata(string.Empty));
    public string HardDeleteLabel { get => (string)GetValue(HardDeleteLabelProperty); set => SetValue(HardDeleteLabelProperty, value); }

    public static readonly DependencyProperty ShowHardDeleteButtonProperty =
        DependencyProperty.Register(nameof(ShowHardDeleteButton), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false, OnSelectionActionVisibilityChanged));
    public bool ShowHardDeleteButton { get => (bool)GetValue(ShowHardDeleteButtonProperty); set => SetValue(ShowHardDeleteButtonProperty, value); }

    public static readonly DependencyProperty CanHardDeleteProperty =
        DependencyProperty.Register(nameof(CanHardDelete), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool CanHardDelete { get => (bool)GetValue(CanHardDeleteProperty); set => SetValue(CanHardDeleteProperty, value); }

    public static readonly DependencyProperty ShowSelectionActionsSeparatorProperty =
        DependencyProperty.Register(nameof(ShowSelectionActionsSeparator), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool ShowSelectionActionsSeparator
    {
        get => (bool)GetValue(ShowSelectionActionsSeparatorProperty);
        private set => SetValue(ShowSelectionActionsSeparatorProperty, value);
    }

    private static void OnSelectionActionVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CrudWorkbenchControl control)
            control.SyncSelectionActionsSeparator();
    }

    private void SyncSelectionActionsSeparator()
        => ShowSelectionActionsSeparator = ShowArchiveButton || ShowHardDeleteButton;

    public static readonly DependencyProperty ColumnsSourceProperty =
        DependencyProperty.Register(nameof(ColumnsSource), typeof(System.Collections.Generic.IList<CrudColumnDefinition>),
            typeof(CrudWorkbenchControl), new PropertyMetadata(null));
    public System.Collections.Generic.IList<CrudColumnDefinition>? ColumnsSource
    {
        get => (System.Collections.Generic.IList<CrudColumnDefinition>?)GetValue(ColumnsSourceProperty);
        set => SetValue(ColumnsSourceProperty, value);
    }

    public static readonly DependencyProperty ColumnFiltersProperty =
        DependencyProperty.Register(nameof(ColumnFilters), typeof(ObservableCollection<CrudColumnFilter>),
            typeof(CrudWorkbenchControl), new PropertyMetadata(null));
    public ObservableCollection<CrudColumnFilter>? ColumnFilters
    {
        get => (ObservableCollection<CrudColumnFilter>?)GetValue(ColumnFiltersProperty);
        set => SetValue(ColumnFiltersProperty, value);
    }

    public static readonly DependencyProperty FilterColumnsProperty =
        DependencyProperty.Register(nameof(FilterColumns), typeof(System.Collections.Generic.IList<CrudColumnDefinition>),
            typeof(CrudWorkbenchControl), new PropertyMetadata(null));
    public System.Collections.Generic.IList<CrudColumnDefinition>? FilterColumns
    {
        get => (System.Collections.Generic.IList<CrudColumnDefinition>?)GetValue(FilterColumnsProperty);
        set => SetValue(FilterColumnsProperty, value);
    }

    public static readonly DependencyProperty HasColumnFiltersProperty =
        DependencyProperty.Register(nameof(HasColumnFilters), typeof(bool),
            typeof(CrudWorkbenchControl), new PropertyMetadata(false));
    public bool HasColumnFilters
    {
        get => (bool)GetValue(HasColumnFiltersProperty);
        set => SetValue(HasColumnFiltersProperty, value);
    }

    public event EventHandler<EventArgs>? AddClicked;
    public event EventHandler<EventArgs>? RefreshClicked;
    public event EventHandler<EventArgs>? ArchiveClicked;
    public event EventHandler<EventArgs>? HardDeleteClicked;
    public event EventHandler<EventArgs>? ColumnsChanged;
    public event EventHandler<EventArgs>? ColumnsResetRequested;
    public event EventHandler<EventArgs>? SearchClicked;
    public event EventHandler<EventArgs>? FiltersChanged;

    private void ColumnFilterBar_FiltersChanged(object sender, EventArgs e)
        => FiltersChanged?.Invoke(this, EventArgs.Empty);

    private void AddButton_Click(object sender, RoutedEventArgs e) => AddClicked?.Invoke(this, EventArgs.Empty);
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshClicked?.Invoke(this, EventArgs.Empty);
    private void ArchiveButton_Click(object sender, RoutedEventArgs e) => ArchiveClicked?.Invoke(this, EventArgs.Empty);
    private void HardDeleteButton_Click(object sender, RoutedEventArgs e) => HardDeleteClicked?.Invoke(this, EventArgs.Empty);

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        FilterText = FilterBox.Text;
        SearchClicked?.Invoke(this, EventArgs.Empty);
    }

    private void FilterBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            FilterText = FilterBox.Text;
            SearchClicked?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void ColumnsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ColumnsSource is null) return;

        var toggleable = ColumnsSource.Where(c => !c.IsHidden).ToList();
        if (toggleable.Count == 0) return;

        var panel = new StackPanel { Spacing = 2, Padding = new Thickness(4, 8, 8, 8) };

        var reset = new Button
        {
            Content = ResourceStrings.Get("CrudGrid_ResetColumns"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        reset.Click += (_, _) =>
        {
            ColumnsResetRequested?.Invoke(this, EventArgs.Empty);
            foreach (var cb in panel.Children.OfType<CheckBox>())
            {
                if (cb.Tag is CrudColumnDefinition col)
                    cb.IsChecked = col.IsVisible;
            }
        };
        panel.Children.Add(reset);
        panel.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(0, 4, 0, 4),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
        });

        foreach (var col in toggleable)
        {
            var item = new CheckBox
            {
                IsChecked = col.IsVisible,
                Content = CrudColumnHeaderBuilder.BuildColumnPickerLabel(col),
                Tag = col,
                Margin = new Thickness(0, 2, 0, 2),
            };
            item.Checked += (_, _) =>
            {
                col.IsVisible = true;
                ColumnsChanged?.Invoke(this, EventArgs.Empty);
            };
            item.Unchecked += (_, _) =>
            {
                col.IsVisible = false;
                ColumnsChanged?.Invoke(this, EventArgs.Empty);
            };
            panel.Children.Add(item);
        }

        var scroll = new ScrollViewer
        {
            MaxHeight = 480,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel,
        };

        var flyout = new Flyout { Content = scroll };
        flyout.ShowAt(ColumnsButton);
    }
}
