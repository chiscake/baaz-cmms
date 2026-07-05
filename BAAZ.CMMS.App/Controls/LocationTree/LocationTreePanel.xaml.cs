using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace BAAZ.CMMS.App.Controls.LocationTree;

public sealed partial class LocationTreePanel : UserControl
{
    private CancellationTokenSource? _searchDebounceCts;

    public LocationTreePanel()
    {
        InitializeComponent();
        EnsureDefaultItemTemplate();
        Loaded += (_, _) => ApplyFilter("Loaded");
    }

    public void EnsureDefaultItemTemplate()
    {
        if (ItemTemplate is not null)
            return;

        if (Resources.TryGetValue("DefaultLocationTreeItemTemplate", out var template)
            && template is DataTemplate dataTemplate)
        {
            ItemTemplate = dataTemplate;
        }
    }

    public TreeView TreeViewControl => Tree;

    public event EventHandler? FilteredTreeChanged;

    public static readonly DependencyProperty TreeItemsProperty =
        DependencyProperty.Register(
            nameof(TreeItems),
            typeof(IReadOnlyList<LocationTreeItem>),
            typeof(LocationTreePanel),
            new PropertyMetadata(null, OnTreeItemsChanged));

    public IReadOnlyList<LocationTreeItem>? TreeItems
    {
        get => (IReadOnlyList<LocationTreeItem>?)GetValue(TreeItemsProperty);
        set => SetValue(TreeItemsProperty, value);
    }

    public static readonly DependencyProperty LocationPathsProperty =
        DependencyProperty.Register(
            nameof(LocationPaths),
            typeof(IReadOnlyDictionary<Guid, string>),
            typeof(LocationTreePanel),
            new PropertyMetadata(null, OnFilterSourceChanged));

    public IReadOnlyDictionary<Guid, string>? LocationPaths
    {
        get => (IReadOnlyDictionary<Guid, string>?)GetValue(LocationPathsProperty);
        set => SetValue(LocationPathsProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(LocationTreePanel),
            new PropertyMetadata(null));

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly DependencyProperty ShowSearchBoxProperty =
        DependencyProperty.Register(
            nameof(ShowSearchBox),
            typeof(bool),
            typeof(LocationTreePanel),
            new PropertyMetadata(true));

    public bool ShowSearchBox
    {
        get => (bool)GetValue(ShowSearchBoxProperty);
        set => SetValue(ShowSearchBoxProperty, value);
    }

    public static readonly DependencyProperty ShowSearchDialogButtonProperty =
        DependencyProperty.Register(
            nameof(ShowSearchDialogButton),
            typeof(bool),
            typeof(LocationTreePanel),
            new PropertyMetadata(false));

    public bool ShowSearchDialogButton
    {
        get => (bool)GetValue(ShowSearchDialogButtonProperty);
        set => SetValue(ShowSearchDialogButtonProperty, value);
    }

    public static readonly DependencyProperty SearchDialogButtonLabelProperty =
        DependencyProperty.Register(
            nameof(SearchDialogButtonLabel),
            typeof(string),
            typeof(LocationTreePanel),
            new PropertyMetadata(string.Empty));

    public string SearchDialogButtonLabel
    {
        get => (string)GetValue(SearchDialogButtonLabelProperty);
        set => SetValue(SearchDialogButtonLabelProperty, value);
    }

    public static readonly DependencyProperty IsSearchDialogButtonEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSearchDialogButtonEnabled),
            typeof(bool),
            typeof(LocationTreePanel),
            new PropertyMetadata(true));

    public bool IsSearchDialogButtonEnabled
    {
        get => (bool)GetValue(IsSearchDialogButtonEnabledProperty);
        set => SetValue(IsSearchDialogButtonEnabledProperty, value);
    }

    public event EventHandler? SearchDialogButtonClick;

    public static readonly DependencyProperty SearchPlaceholderProperty =
        DependencyProperty.Register(
            nameof(SearchPlaceholder),
            typeof(string),
            typeof(LocationTreePanel),
            new PropertyMetadata(string.Empty));

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public static readonly DependencyProperty ClearSearchLabelProperty =
        DependencyProperty.Register(
            nameof(ClearSearchLabel),
            typeof(string),
            typeof(LocationTreePanel),
            new PropertyMetadata(string.Empty));

    public string ClearSearchLabel
    {
        get => (string)GetValue(ClearSearchLabelProperty);
        set => SetValue(ClearSearchLabelProperty, value);
    }

    public static readonly DependencyProperty NoResultsMessageProperty =
        DependencyProperty.Register(
            nameof(NoResultsMessage),
            typeof(string),
            typeof(LocationTreePanel),
            new PropertyMetadata(string.Empty));

    public string NoResultsMessage
    {
        get => (string)GetValue(NoResultsMessageProperty);
        set => SetValue(NoResultsMessageProperty, value);
    }

    public static readonly DependencyProperty TreeMinHeightProperty =
        DependencyProperty.Register(
            nameof(TreeMinHeight),
            typeof(double),
            typeof(LocationTreePanel),
            new PropertyMetadata(0.0));

    public double TreeMinHeight
    {
        get => (double)GetValue(TreeMinHeightProperty);
        set => SetValue(TreeMinHeightProperty, value);
    }

    public static readonly DependencyProperty TreeMaxHeightProperty =
        DependencyProperty.Register(
            nameof(TreeMaxHeight),
            typeof(double),
            typeof(LocationTreePanel),
            new PropertyMetadata(double.PositiveInfinity));

    public double TreeMaxHeight
    {
        get => (double)GetValue(TreeMaxHeightProperty);
        set => SetValue(TreeMaxHeightProperty, value);
    }

    public static readonly DependencyProperty IsFilterActiveProperty =
        DependencyProperty.Register(
            nameof(IsFilterActive),
            typeof(bool),
            typeof(LocationTreePanel),
            new PropertyMetadata(false));

    public bool IsFilterActive
    {
        get => (bool)GetValue(IsFilterActiveProperty);
        private set => SetValue(IsFilterActiveProperty, value);
    }

    /// <summary>Если false, ItemsSource TreeView не меняется — только FilteredTreeChanged.</summary>
    public static readonly DependencyProperty AutoApplyItemsSourceProperty =
        DependencyProperty.Register(
            nameof(AutoApplyItemsSource),
            typeof(bool),
            typeof(LocationTreePanel),
            new PropertyMetadata(true));

    public bool AutoApplyItemsSource
    {
        get => (bool)GetValue(AutoApplyItemsSourceProperty);
        set => SetValue(AutoApplyItemsSourceProperty, value);
    }

    public IReadOnlyList<LocationTreeItem> FilteredTreeItems { get; private set; } = [];

    public void RefreshFilter(string reason = "RefreshFilter") => ApplyFilter(reason);

    public void ClearSearch()
    {
        SearchBox.Text = string.Empty;
        ApplyFilter("ClearSearch");
    }

    private static void OnTreeItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationTreePanel panel)
            panel.ApplyFilter("TreeItemsChanged");
    }

    private static void OnFilterSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationTreePanel panel && panel.IsFilterActive)
            panel.ApplyFilter("LocationPathsChanged");
    }

    private void SearchDialogButton_Click(object sender, RoutedEventArgs e) =>
        SearchDialogButtonClick?.Invoke(this, EventArgs.Empty);

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            try
            {
                await Task.Delay(200, token);
                if (!token.IsCancellationRequested)
                    ApplyFilter("SearchTextChanged");
            }
            catch (TaskCanceledException)
            {
                // debounce cancelled
            }
        });
    }

    private void ApplyFilter(string reason)
    {
        using var step = PerfDebug.Step(nameof(LocationTreePanel), $"ApplyFilter({reason})");
        var query = SearchBox?.Text;
        var roots = TreeItems ?? [];
        var hasQuery = !string.IsNullOrWhiteSpace(query);
        IsFilterActive = hasQuery;

        using (PerfDebug.Step(nameof(LocationTreePanel), "FilterTree"))
        {
            FilteredTreeItems = hasQuery
                ? LocationTreeFilterHelper.FilterTree(roots, query, LocationPaths)
                : roots;
        }

        if (AutoApplyItemsSource)
            Tree.ItemsSource = FilteredTreeItems;

        NoResultsBar.IsOpen = hasQuery && FilteredTreeItems.Count == 0;
        FilteredTreeChanged?.Invoke(this, EventArgs.Empty);
        PerfDebug.Mark(nameof(LocationTreePanel), $"nodes={FilteredTreeItems.Count} filterActive={hasQuery}");
    }
}
