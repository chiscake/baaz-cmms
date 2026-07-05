using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.LocationScopePicker;

public sealed partial class LocationScopePicker : UserControl, INotifyPropertyChanged
{
    private const string LogTag = "[LocationScopePicker]";

    // Узлы по Id — единственный источник истины, CheckedState живёт прямо на узле
    private readonly Dictionary<Guid, LocationScopeNode> _nodesById = [];
    private bool _isInternalSelectionUpdate;
    private IReadOnlyList<LocationScopeNode> _scopeRoots = [];
    private IReadOnlyList<LocationScopeNode> _fullScopeRoots = [];

    // Дебаунс перестройки дерева
    private CancellationTokenSource? _rebuildCts;
    private bool _rebuildScheduled;

    // Флаг «диалог открывается»
    private bool _isDialogOpen;
    private int _attachedVersion = -1;
    private IReadOnlyDictionary<Guid, LocationScopeNode> _canonicalNodesById =
        new Dictionary<Guid, LocationScopeNode>();

    public int AttachedVersion => _attachedVersion;

    /// <summary>Блокирует обратную синхронизацию VM→picker (модальный диалог, клик по дереву).</summary>
    public bool SuppressExternalSelectionSync { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocationScopePicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureLocalizedLabels();
        ScopeTree.ItemInvoked += ScopeTree_ItemInvoked;
        ScopeTree.SelectionMode = TreeViewSelectionMode.None;
        TreePanel.SearchDialogButtonClick += TreePanel_SearchDialogButtonClick;
        UpdateFlyoutSearchDialogButton();
    }

    private void TreePanel_SearchDialogButtonClick(object? sender, EventArgs e) =>
        OpenDialogButton_Click(this, new RoutedEventArgs());

    private TreeView ScopeTree => TreePanel.TreeViewControl;

    public event EventHandler? SelectionChanged;

    /// <summary>Выбор меняется пользователем (клик по дереву или модальный диалог) — до записи в VM.</summary>
    public event EventHandler? UserSelectionChanging;

    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty TreeItemsProperty =
        DependencyProperty.Register(nameof(TreeItems),
            typeof(IReadOnlyList<LocationTreeItem>), typeof(LocationScopePicker),
            new PropertyMetadata(null));

    public IReadOnlyList<LocationTreeItem>? TreeItems
    {
        get => (IReadOnlyList<LocationTreeItem>?)GetValue(TreeItemsProperty);
        set => SetValue(TreeItemsProperty, value);
    }

    public static readonly DependencyProperty LocationPathsProperty =
        DependencyProperty.Register(nameof(LocationPaths),
            typeof(IReadOnlyDictionary<Guid, string>), typeof(LocationScopePicker),
            new PropertyMetadata(null, OnLocationPathsChanged));

    public IReadOnlyDictionary<Guid, string>? LocationPaths
    {
        get => (IReadOnlyDictionary<Guid, string>?)GetValue(LocationPathsProperty);
        set => SetValue(LocationPathsProperty, value);
    }

    public static readonly DependencyProperty SelectedLocationIdsProperty =
        DependencyProperty.Register(nameof(SelectedLocationIds),
            typeof(IReadOnlySet<Guid>), typeof(LocationScopePicker),
            new PropertyMetadata(null, OnSelectedLocationIdsChanged));

    public IReadOnlySet<Guid>? SelectedLocationIds
    {
        get => (IReadOnlySet<Guid>?)GetValue(SelectedLocationIdsProperty);
        set => SetValue(SelectedLocationIdsProperty, value);
    }

    public static readonly DependencyProperty ShowDialogButtonProperty =
        DependencyProperty.Register(nameof(ShowDialogButton),
            typeof(bool), typeof(LocationScopePicker),
            new PropertyMetadata(false, OnChromePropertyChanged));

    public bool ShowDialogButton
    {
        get => (bool)GetValue(ShowDialogButtonProperty);
        set => SetValue(ShowDialogButtonProperty, value);
    }

    public static readonly DependencyProperty IsDialogHostProperty =
        DependencyProperty.Register(nameof(IsDialogHost),
            typeof(bool), typeof(LocationScopePicker),
            new PropertyMetadata(false, OnChromePropertyChanged));

    public bool IsDialogHost
    {
        get => (bool)GetValue(IsDialogHostProperty);
        set => SetValue(IsDialogHostProperty, value);
    }

    public static readonly DependencyProperty IsFlyoutHostProperty =
        DependencyProperty.Register(nameof(IsFlyoutHost),
            typeof(bool), typeof(LocationScopePicker),
            new PropertyMetadata(false, OnChromePropertyChanged));

    public bool IsFlyoutHost
    {
        get => (bool)GetValue(IsFlyoutHostProperty);
        set => SetValue(IsFlyoutHostProperty, value);
    }

    public static readonly DependencyProperty DialogButtonLabelProperty =
        DependencyProperty.Register(nameof(DialogButtonLabel),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string DialogButtonLabel
    {
        get => (string)GetValue(DialogButtonLabelProperty);
        set => SetValue(DialogButtonLabelProperty, value);
    }

    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.Register(nameof(DialogTitle),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public static readonly DependencyProperty SearchPlaceholderProperty =
        DependencyProperty.Register(nameof(SearchPlaceholder),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public static readonly DependencyProperty NoResultsMessageProperty =
        DependencyProperty.Register(nameof(NoResultsMessage),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string NoResultsMessage
    {
        get => (string)GetValue(NoResultsMessageProperty);
        set => SetValue(NoResultsMessageProperty, value);
    }

    public static readonly DependencyProperty ClearSearchLabelProperty =
        DependencyProperty.Register(nameof(ClearSearchLabel),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string ClearSearchLabel
    {
        get => (string)GetValue(ClearSearchLabelProperty);
        set => SetValue(ClearSearchLabelProperty, value);
    }

    public static readonly DependencyProperty SelectionSummaryProperty =
        DependencyProperty.Register(nameof(SelectionSummary),
            typeof(string), typeof(LocationScopePicker),
            new PropertyMetadata(string.Empty));

    public string SelectionSummary
    {
        get => (string)GetValue(SelectionSummaryProperty);
        private set => SetValue(SelectionSummaryProperty, value);
    }

    // ── Chrome computed props ─────────────────────────────────────────────────

    public static readonly DependencyProperty IsTreeLoadingProperty =
        DependencyProperty.Register(nameof(IsTreeLoading),
            typeof(bool), typeof(LocationScopePicker), new PropertyMetadata(false));

    public bool IsTreeLoading
    {
        get => (bool)GetValue(IsTreeLoadingProperty);
        private set => SetValue(IsTreeLoadingProperty, value);
    }

    public static readonly DependencyProperty IsDialogLoadingProperty =
        DependencyProperty.Register(nameof(IsDialogLoading),
            typeof(bool), typeof(LocationScopePicker),
            new PropertyMetadata(false, OnIsDialogLoadingChanged));

    public bool IsDialogLoading
    {
        get => (bool)GetValue(IsDialogLoadingProperty);
        private set => SetValue(IsDialogLoadingProperty, value);
    }

    public bool IsDialogButtonEnabled => !IsDialogLoading;

    private static void OnIsDialogLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationScopePicker picker)
        {
            picker.OnPropertyChanged(nameof(IsDialogButtonEnabled));
            picker.UpdateFlyoutSearchDialogButton();
        }
    }

    public bool ShowSummaryPanel => ShowDialogButton && !IsDialogHost && !IsFlyoutHost;

    public bool ShowDialogButtonInSearchRow => ShowDialogButton && IsFlyoutHost && !IsDialogHost;

    public bool ShowSelectionDetails => !ShowSummaryPanel;

    /// <summary>
    /// Inline-дерево в боковой панели редактора не нужно при <see cref="ShowDialogButton"/> —
    /// выбор через диалог; скрытие снижает высоту формы (см. <c>LocationPicker.ShowInlineTree</c>).
    /// </summary>
    public bool ShowInlineTree => IsDialogHost || IsFlyoutHost || !ShowDialogButton;

    public double SelectionDetailsMaxHeight => IsDialogHost ? 120 : IsFlyoutHost ? 96 : double.PositiveInfinity;

    public ScrollBarVisibility SelectionDetailsScrollBarVisibility =>
        IsDialogHost || IsFlyoutHost ? ScrollBarVisibility.Visible : ScrollBarVisibility.Auto;

    public double InlineTreeMinHeight => IsDialogHost ? 280 : 160;

    public double InlineTreeMaxHeight => IsDialogHost ? 400 : 240;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Привязывает scope-дерево к снимку; перестройка только при смене version.</summary>
    public void AttachTree(
        LocationScopeTreeProjection projection,
        IReadOnlyList<LocationTreeItem> sourceRoots,
        IReadOnlyDictionary<Guid, string> paths,
        int version,
        string reason = "AttachTree")
    {
        if (_attachedVersion == version)
        {
            LocationPaths = paths;
            ApplyCheckStatesToTree();
            UpdateSelectionSummary();
            return;
        }

        Log($"AttachTree({reason}) version={version}");
        _attachedVersion = version;
        _fullScopeRoots = projection.Roots;
        _canonicalNodesById = projection.NodesById;
        _nodesById.Clear();
        foreach (var (id, node) in projection.NodesById)
            _nodesById[id] = node;

        LocationPaths = paths;
        TreeItems = sourceRoots;
        TreePanel.TreeItems = sourceRoots;
        ScheduleRebuild(reason);
    }

    /// <summary>
    /// Привязывает готовую проекцию синхронно — без <see cref="ScheduleRebuild"/>.
    /// Используется в flyout, где дерево должно быть готово к моменту открытия.
    /// </summary>
    public void AttachTreeSync(
        LocationScopeTreeProjection projection,
        IReadOnlyList<LocationTreeItem> sourceRoots,
        IReadOnlyDictionary<Guid, string> paths,
        int version)
    {
        _rebuildCts?.Cancel();
        _attachedVersion = version;
        _fullScopeRoots = projection.Roots;
        _canonicalNodesById = projection.NodesById;
        _nodesById.Clear();
        foreach (var (id, node) in projection.NodesById)
            _nodesById[id] = node;

        LocationPaths = paths;
        TreeItems = sourceRoots;
        TreePanel.TreeItems = sourceRoots;

        ApplyFilteredScopeTree();
        IsTreeLoading = false;
    }

    public void RefreshTree()
    {
        if (_attachedVersion < 0)
            return;

        ScheduleRebuild("RefreshTree");
    }

    public void SetSelection(IReadOnlySet<Guid> locationIds, string reason = "SetSelection")
    {
        var expanded = _canonicalNodesById.Count > 0
            ? LocationScopeSelectionHelper.ExpandAnchorsToSelection(locationIds, _canonicalNodesById)
            : new HashSet<Guid>(locationIds);

        if (SelectedLocationIds is not null && expanded.SetEquals(SelectedLocationIds))
        {
            Log($"SetSelection({reason}) SKIP unchanged count={expanded.Count}");
            ApplyCheckStatesToTree();
            UpdateSelectionSummary();
            return;
        }

        Log($"SetSelection({reason}) count={expanded.Count}");
        _isInternalSelectionUpdate = true;
        try
        {
            SetValue(SelectedLocationIdsProperty, expanded);
        }
        finally
        {
            _isInternalSelectionUpdate = false;
        }

        ApplyCheckStatesToTree();
        UpdateSelectionSummary();
    }

    public async Task<IReadOnlySet<Guid>?> ShowPickerDialogAsync(XamlRoot xamlRoot)
    {
        SuppressExternalSelectionSync = true;
        IsDialogLoading = true;
        try
        {
            var result = await LocationPickerDialogHelper.ShowMultiAsync(
                new LocationScopePickerDialogRequest
                {
                    TreeItems = TreeItems ?? [],
                    LocationPaths = LocationPaths,
                    InitialSelection = SelectedLocationIds ?? new HashSet<Guid>(),
                    Title = DialogTitle,
                },
                xamlRoot);

            if (result is null)
                return null;

            ApplyUserSelection(result, "dialog-apply");
            return result;
        }
        finally
        {
            SuppressExternalSelectionSync = false;
            IsDialogLoading = false;
        }
    }

    private void ApplyUserSelection(IReadOnlySet<Guid> locationIds, string reason)
    {
        UserSelectionChanging?.Invoke(this, EventArgs.Empty);
        SetSelection(locationIds, reason);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Property change callbacks ─────────────────────────────────────────────

    private static void OnLocationPathsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationScopePicker picker)
            picker.UpdateSelectionSummary();
    }

    private static void OnSelectedLocationIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LocationScopePicker picker || picker._isInternalSelectionUpdate)
            return;

        picker.ApplyCheckStatesToTree();
        picker.UpdateSelectionSummary();
    }

    private static void OnChromePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationScopePicker picker)
            picker.NotifyChromePropertiesChanged();
    }

    // ── Localization ──────────────────────────────────────────────────────────

    private void EnsureLocalizedLabels()
    {
        if (string.IsNullOrEmpty(DialogButtonLabel))
            DialogButtonLabel = ResourceStrings.Get("LocationPicker_OpenDialog");
        if (string.IsNullOrEmpty(DialogTitle))
            DialogTitle = ResourceStrings.Get("LocationScopePicker_DialogTitle");
        if (string.IsNullOrEmpty(SearchPlaceholder))
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder");
        if (string.IsNullOrEmpty(NoResultsMessage))
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults");
        if (string.IsNullOrEmpty(ClearSearchLabel))
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch");
    }

    private void NotifyChromePropertiesChanged()
    {
        OnPropertyChanged(nameof(ShowSummaryPanel));
        OnPropertyChanged(nameof(ShowDialogButtonInSearchRow));
        OnPropertyChanged(nameof(ShowSelectionDetails));
        OnPropertyChanged(nameof(ShowInlineTree));
        OnPropertyChanged(nameof(SelectionDetailsMaxHeight));
        OnPropertyChanged(nameof(SelectionDetailsScrollBarVisibility));
        OnPropertyChanged(nameof(InlineTreeMinHeight));
        OnPropertyChanged(nameof(InlineTreeMaxHeight));
        UpdateFlyoutSearchDialogButton();
    }

    private void UpdateFlyoutSearchDialogButton()
    {
        var show = ShowDialogButtonInSearchRow;
        TreePanel.ShowSearchDialogButton = show;
        TreePanel.SearchDialogButtonLabel = DialogButtonLabel;
        TreePanel.IsSearchDialogButtonEnabled = IsDialogButtonEnabled;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // ── Tree construction ─────────────────────────────────────────────────────

    private void ScheduleRebuild(string reason)
    {
        _rebuildCts?.Cancel();
        _rebuildCts = new CancellationTokenSource();
        var token = _rebuildCts.Token;
        var captured = reason;

        if (!_rebuildScheduled)
        {
            _rebuildScheduled = true;
            IsTreeLoading = true;
        }

        _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (token.IsCancellationRequested)
                return;

            _rebuildScheduled = false;
            RebuildTree(captured);
        });
    }

    private void RebuildTree(string reason = "RebuildTree")
    {
        Log($"RebuildTree({reason}) BEGIN version={_attachedVersion}");
        try
        {
            if (_attachedVersion < 0 || _fullScopeRoots.Count == 0)
            {
                _scopeRoots = [];
                ScopeTree.ItemsSource = null;
                UpdateSelectionSummary();
                return;
            }

            TreePanel.RefreshFilter(reason);
            ApplyFilteredScopeTree();
            UpdateSelectionSummary();
        }
        catch (Exception ex)
        {
            Log($"RebuildTree CAUGHT: {ex.Message}");
        }
        finally
        {
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
                () => IsTreeLoading = false);
        }

        Log($"RebuildTree({reason}) END");
    }

    private void TreePanel_FilteredTreeChanged(object? sender, EventArgs e) =>
        ApplyFilteredScopeTree();

    private void ApplyFilteredScopeTree()
    {
        var filtered = TreePanel.FilteredTreeItems;
        _scopeRoots = TreePanel.IsFilterActive
            ? BuildFilteredView(filtered)
            : _fullScopeRoots;

        ScopeTree.ItemsSource = _scopeRoots;
        ScopeTree.SelectionMode = TreeViewSelectionMode.None;
        ApplyCheckStatesToTree();
    }

    private IReadOnlyList<LocationScopeNode> BuildFilteredView(IEnumerable<LocationTreeItem> items) =>
        items.Select(BuildFilteredNode).ToList();

    private LocationScopeNode BuildFilteredNode(LocationTreeItem item)
    {
        var canonical = _canonicalNodesById[item.Id];
        return new LocationScopeNode
        {
            Id = canonical.Id,
            Name = canonical.Name,
            IsEnabled = canonical.IsEnabled,
            Children = item.Children.Select(BuildFilteredNode).ToList(),
        };
    }

    // ── User interaction ──────────────────────────────────────────────────────

    /// <summary>
    /// Одиночный клик на элемент дерева (CheckBox при этом IsHitTestVisible=False).
    /// Переключает весь поддерево: если полностью выделен — снять, иначе — выделить всё.
    /// </summary>
    private void ScopeTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is not LocationScopeNode node || !node.IsEnabled)
            return;

        if (!_canonicalNodesById.TryGetValue(node.Id, out var canonical))
            return;

        var current = SelectedLocationIds is not null
            ? new HashSet<Guid>(SelectedLocationIds)
            : new HashSet<Guid>();

        var subtree = LocationScopeSelectionHelper.CollectSubtreeIds(canonical);

        if (LocationScopeSelectionHelper.IsSubtreeFullySelected(canonical, current))
        {
            foreach (var id in subtree)
                current.Remove(id);
        }
        else
        {
            foreach (var id in subtree)
                current.Add(id);
        }

        CommitSelection(current);
    }

    private void RaiseUserSelectionChanging() =>
        UserSelectionChanging?.Invoke(this, EventArgs.Empty);

    private void CommitSelection(HashSet<Guid> selected)
    {
        SuppressExternalSelectionSync = true;
        try
        {
            RaiseUserSelectionChanging();
            _isInternalSelectionUpdate = true;
            try
            {
                SetValue(SelectedLocationIdsProperty, selected);
            }
            finally
            {
                _isInternalSelectionUpdate = false;
            }

            ApplyCheckStatesToTree();
            UpdateSelectionSummary();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            SuppressExternalSelectionSync = false;
        }
    }

    // ── Check state propagation ───────────────────────────────────────────────

    /// <summary>
    /// Обновляет CheckedState у всех узлов дерева на основе SelectedLocationIds.
    /// Работает на объектах напрямую — никаких UI-словарей и событий CheckBox.
    /// </summary>
    private void ApplyCheckStatesToTree()
    {
        var selected = SelectedLocationIds ?? (IReadOnlySet<Guid>)new HashSet<Guid>();

        // Обновляем filteredScopeRoots (то, что видит пользователь)
        PropagateCheckStates(_scopeRoots, selected);

        // Обновляем и fullScopeRoots, чтобы CollectSubtreeIds/IsSubtreeFullySelected
        // работали корректно для свёрнутых узлов
        PropagateCheckStates(_fullScopeRoots, selected);

        Log($"ApplyCheckStates: selected={selected.Count}");
    }

    private static void PropagateCheckStates(
        IEnumerable<LocationScopeNode> nodes,
        IReadOnlySet<Guid> selected)
    {
        foreach (var node in nodes)
        {
            // Сначала рекурсивно обновляем детей — порядок важен,
            // т.к. IsSubtreeFullySelected смотрит на всё поддерево
            PropagateCheckStates(node.Children, selected);

            node.CheckedState = ComputeCheckState(node, selected);
        }
    }

    private static bool? ComputeCheckState(LocationScopeNode node, IReadOnlySet<Guid> selected)
    {
        if (node.Children.Count == 0)
            return selected.Contains(node.Id);

        if (LocationScopeSelectionHelper.IsSubtreeFullySelected(node, selected))
            return true;

        if (LocationScopeSelectionHelper.HasPartialSelection(node, selected))
            return null;

        return false;
    }

    // ── Display labels ────────────────────────────────────────────────────────

    private string ResolveLabel(Guid id)
    {
        if (LocationPaths is not null && LocationPaths.TryGetValue(id, out var path))
            return path;

        if (_nodesById.TryGetValue(id, out var node))
            return node.Name;

        // fallback: поиск в fullScopeRoots
        var found = FindNode(_fullScopeRoots, id);
        return found?.Name ?? id.ToString();
    }

    private static LocationScopeNode? FindNode(IEnumerable<LocationScopeNode> nodes, Guid id)
    {
        foreach (var n in nodes)
        {
            if (n.Id == id) return n;
            var child = FindNode(n.Children, id);
            if (child is not null) return child;
        }
        return null;
    }

    private IReadOnlyList<string> BuildDisplayLabels()
    {
        var selected = SelectedLocationIds;
        if (selected is null || selected.Count == 0)
            return [];

        return LocationScopeSelectionHelper.BuildCollapsedDisplayLabels(
            _fullScopeRoots,
            selected,
            ResolveLabel);
    }

    private void UpdateSelectionSummary()
    {
        var labels = BuildDisplayLabels();
        SelectionSummary = labels.Count == 0
            ? ResourceStrings.Get("LocationPicker_NotSelected")
            : LocationScopeSelectionHelper.FormatMultiline(
                labels,
                ResourceStrings.Get("LocationPicker_NotSelected"));
    }

    // ── Dialog button ─────────────────────────────────────────────────────────

    private async void OpenDialogButton_Click(object sender, RoutedEventArgs e)
    {
        if (XamlRoot is null || _isDialogOpen)
            return;

        _isDialogOpen = true;
        try { await ShowPickerDialogAsync(XamlRoot); }
        finally { _isDialogOpen = false; }
    }

    private static void Log(string message) =>
        Debug.WriteLine($"{LogTag} {message}");
}
