using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers.LocationHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.LocationPicker;

public sealed partial class LocationPicker : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const string LogTag = "[LocationPicker]";

    // Дебаунс перестройки дерева — отмена при быстрых повторных изменениях TreeItems.
    private CancellationTokenSource? _rebuildCts;
    private bool _rebuildScheduled;

    // Флаг «пикер сам меняет SelectedParentId» — подавляет обратный callback.
    private bool _isProgrammaticChange;

    // Флаг «диалог открывается» — показывает кольцо прогресса на кнопке.
    private bool _isDialogOpen;
    private int _attachedVersion = -1;

    public int AttachedVersion => _attachedVersion;

    public LocationPicker()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Публичные события и API ───────────────────────────────────────────────

    public event EventHandler<Guid?>? LocationSelected;

    /// <summary>
    /// Привязывает дерево к снимку каталога. Перестройка UI только при смене <paramref name="version"/>.
    /// </summary>
    public void AttachTree(
        IReadOnlyList<LocationTreeItem> roots,
        IReadOnlyDictionary<Guid, string> paths,
        int version,
        string reason = "AttachTree")
    {
        if (_attachedVersion == version)
        {
            LocationPaths = paths;
            ApplySelectionToTree();
            return;
        }

        Log($"AttachTree({reason}) version={version}");
        _attachedVersion = version;
        LocationPaths = paths;
        TreeItems = roots;
        TreePanel.TreeItems = roots;
        ScheduleRebuild(reason);
    }

    /// <summary>
    /// Программно выставляет выбранную локацию (без перестройки дерева).
    /// </summary>
    public void SetSelection(Guid? locationId, string reason = "SetSelection")
    {
        Log($"SetSelection({reason}) id={locationId}");
        SetSelectedIdSilent(locationId);
        UpdateDisplay(locationId);
        ApplySelectionToTree();
    }

    /// <summary>Перестроить фильтр/отображение при том же снимке.</summary>
    public void RefreshTree()
    {
        if (_attachedVersion < 0)
            return;

        ScheduleRebuild("RefreshTree");
    }

    /// <summary>Показ модального диалога выбора локации.</summary>
    public async Task<Guid?> ShowPickerDialogAsync(XamlRoot xamlRoot)
    {
        Log($"ShowPickerDialogAsync START treeItems={TreeItems?.Count ?? -1}, attachedVersion={_attachedVersion}");
        IsDialogLoading = true;
        try
        {
            var request = new LocationPickerDialogRequest
            {
                TreeItems = TreeItems ?? [],
                TreeVersion = _attachedVersion,
                LocationPaths = LocationPaths,
                DisabledNodeIds = DisabledNodeIds,
                InitialSelection = SelectedParentId,
                AllowClearSelection = AllowClearSelection,
                ClearParentLabel = ClearParentLabel,
                Title = DialogTitle,
            };
            Log("ShowPickerDialogAsync: calling LocationPickerDialogHelper.ShowSingleAsync");
            var result = await LocationPickerDialogHelper.ShowSingleAsync(request, xamlRoot);
            Log($"ShowPickerDialogAsync: ShowSingleAsync returned, result={(result is null ? "null" : result.LocationId.ToString())}");

            if (result is null)
                return null;

            CommitUserSelection(result.LocationId, "Dialog");
            return result.LocationId;
        }
        catch (Exception ex)
        {
            Log($"ShowPickerDialogAsync EXCEPTION: {ex}");
            throw;
        }
        finally
        {
            IsDialogLoading = false;
            Log("ShowPickerDialogAsync END");
        }
    }

    // ── Dependency Properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty TreeItemsProperty =
        DependencyProperty.Register(nameof(TreeItems),
            typeof(IReadOnlyList<LocationTreeItem>), typeof(LocationPicker),
            new PropertyMetadata(null));

    public IReadOnlyList<LocationTreeItem>? TreeItems
    {
        get => (IReadOnlyList<LocationTreeItem>?)GetValue(TreeItemsProperty);
        set => SetValue(TreeItemsProperty, value);
    }

    public static readonly DependencyProperty SelectedParentIdProperty =
        DependencyProperty.Register(nameof(SelectedParentId),
            typeof(Guid?), typeof(LocationPicker),
            new PropertyMetadata(null, (d, e) => ((LocationPicker)d).OnSelectedParentIdChanged(e)));

    public Guid? SelectedParentId
    {
        get => (Guid?)GetValue(SelectedParentIdProperty);
        set => SetValue(SelectedParentIdProperty, value);
    }

    public static readonly DependencyProperty LocationPathsProperty =
        DependencyProperty.Register(nameof(LocationPaths),
            typeof(IReadOnlyDictionary<Guid, string>), typeof(LocationPicker),
            new PropertyMetadata(null, (d, _) => ((LocationPicker)d).UpdateDisplay(((LocationPicker)d).SelectedParentId)));

    public IReadOnlyDictionary<Guid, string>? LocationPaths
    {
        get => (IReadOnlyDictionary<Guid, string>?)GetValue(LocationPathsProperty);
        set => SetValue(LocationPathsProperty, value);
    }

    public static readonly DependencyProperty DisabledNodeIdsProperty =
        DependencyProperty.Register(nameof(DisabledNodeIds),
            typeof(IReadOnlySet<Guid>), typeof(LocationPicker),
            new PropertyMetadata(null));

    public IReadOnlySet<Guid>? DisabledNodeIds
    {
        get => (IReadOnlySet<Guid>?)GetValue(DisabledNodeIdsProperty);
        set => SetValue(DisabledNodeIdsProperty, value);
    }

    public static readonly DependencyProperty AllowClearSelectionProperty =
        DependencyProperty.Register(nameof(AllowClearSelection),
            typeof(bool), typeof(LocationPicker),
            new PropertyMetadata(true, (d, _) => ((LocationPicker)d).NotifyChromeChanged()));

    public bool AllowClearSelection
    {
        get => (bool)GetValue(AllowClearSelectionProperty);
        set => SetValue(AllowClearSelectionProperty, value);
    }

    public static readonly DependencyProperty ShowDialogButtonProperty =
        DependencyProperty.Register(nameof(ShowDialogButton),
            typeof(bool), typeof(LocationPicker),
            new PropertyMetadata(false, (d, _) => ((LocationPicker)d).NotifyChromeChanged()));

    public bool ShowDialogButton
    {
        get => (bool)GetValue(ShowDialogButtonProperty);
        set => SetValue(ShowDialogButtonProperty, value);
    }

    public static readonly DependencyProperty IsDialogHostProperty =
        DependencyProperty.Register(nameof(IsDialogHost),
            typeof(bool), typeof(LocationPicker),
            new PropertyMetadata(false, (d, _) => ((LocationPicker)d).NotifyChromeChanged()));

    public bool IsDialogHost
    {
        get => (bool)GetValue(IsDialogHostProperty);
        set => SetValue(IsDialogHostProperty, value);
    }

    public static readonly DependencyProperty IsFlyoutHostProperty =
        DependencyProperty.Register(nameof(IsFlyoutHost),
            typeof(bool), typeof(LocationPicker),
            new PropertyMetadata(false, (d, _) => ((LocationPicker)d).NotifyChromeChanged()));

    public bool IsFlyoutHost
    {
        get => (bool)GetValue(IsFlyoutHostProperty);
        set => SetValue(IsFlyoutHostProperty, value);
    }

    public static readonly DependencyProperty ClearParentLabelProperty =
        DependencyProperty.Register(nameof(ClearParentLabel),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string ClearParentLabel
    {
        get => (string)GetValue(ClearParentLabelProperty);
        set => SetValue(ClearParentLabelProperty, value);
    }

    public static readonly DependencyProperty DialogButtonLabelProperty =
        DependencyProperty.Register(nameof(DialogButtonLabel),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string DialogButtonLabel
    {
        get => (string)GetValue(DialogButtonLabelProperty);
        set => SetValue(DialogButtonLabelProperty, value);
    }

    public static readonly DependencyProperty DialogTitleProperty =
        DependencyProperty.Register(nameof(DialogTitle),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string DialogTitle
    {
        get => (string)GetValue(DialogTitleProperty);
        set => SetValue(DialogTitleProperty, value);
    }

    public static readonly DependencyProperty SearchPlaceholderProperty =
        DependencyProperty.Register(nameof(SearchPlaceholder),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public static readonly DependencyProperty NoResultsMessageProperty =
        DependencyProperty.Register(nameof(NoResultsMessage),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string NoResultsMessage
    {
        get => (string)GetValue(NoResultsMessageProperty);
        set => SetValue(NoResultsMessageProperty, value);
    }

    public static readonly DependencyProperty ClearSearchLabelProperty =
        DependencyProperty.Register(nameof(ClearSearchLabel),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string ClearSearchLabel
    {
        get => (string)GetValue(ClearSearchLabelProperty);
        set => SetValue(ClearSearchLabelProperty, value);
    }

    public static readonly DependencyProperty NotSelectedLabelProperty =
        DependencyProperty.Register(nameof(NotSelectedLabel),
            typeof(string), typeof(LocationPicker),
            new PropertyMetadata(string.Empty, (d, _) => ((LocationPicker)d).UpdateDisplay(((LocationPicker)d).SelectedParentId)));

    public string NotSelectedLabel
    {
        get => (string)GetValue(NotSelectedLabelProperty);
        set => SetValue(NotSelectedLabelProperty, value);
    }

    public static readonly DependencyProperty SelectedLocationDisplayProperty =
        DependencyProperty.Register(nameof(SelectedLocationDisplay),
            typeof(string), typeof(LocationPicker), new PropertyMetadata(string.Empty));

    public string SelectedLocationDisplay
    {
        get => (string)GetValue(SelectedLocationDisplayProperty);
        private set => SetValue(SelectedLocationDisplayProperty, value);
    }

    public static readonly DependencyProperty IsTreeLoadingProperty =
        DependencyProperty.Register(nameof(IsTreeLoading),
            typeof(bool), typeof(LocationPicker), new PropertyMetadata(false));

    public bool IsTreeLoading
    {
        get => (bool)GetValue(IsTreeLoadingProperty);
        private set => SetValue(IsTreeLoadingProperty, value);
    }

    public static readonly DependencyProperty IsDialogLoadingProperty =
        DependencyProperty.Register(nameof(IsDialogLoading),
            typeof(bool), typeof(LocationPicker),
            new PropertyMetadata(false, OnIsDialogLoadingChanged));

    public bool IsDialogLoading
    {
        get => (bool)GetValue(IsDialogLoadingProperty);
        private set => SetValue(IsDialogLoadingProperty, value);
    }

    public bool IsDialogButtonEnabled => !IsDialogLoading;

    private static void OnIsDialogLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LocationPicker picker)
        {
            picker.OnPropertyChanged(nameof(IsDialogButtonEnabled));
            picker.UpdateFlyoutSearchDialogButton();
        }
    }

    // ── Computed chrome props ─────────────────────────────────────────────────

    public bool ShowSummaryPanel => ShowDialogButton && !IsDialogHost && !IsFlyoutHost;

    public bool ShowDialogButtonInSearchRow => ShowDialogButton && IsFlyoutHost && !IsDialogHost;

    public bool ShowClearButton => AllowClearSelection && !IsDialogHost && !IsFlyoutHost;

    /// <summary>
    /// Inline-дерево в боковой панели редактора не нужно при <see cref="ShowDialogButton"/> —
    /// выбор через диалог; скрытие снижает высоту формы и убирает вложенный ScrollViewer.
    /// </summary>
    public bool ShowInlineTree => IsDialogHost || IsFlyoutHost || !ShowDialogButton;

    public double InlineTreeMinHeight => IsDialogHost ? 280 : 160;

    public double InlineTreeMaxHeight => IsDialogHost ? 400 : 240;

    // ── Initialization ────────────────────────────────────────────────────────

    private bool _eventsHooked;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureLabels();
        TreePanel.EnsureDefaultItemTemplate();
        InlineTreeGrid.Visibility = ShowInlineTree ? Visibility.Visible : Visibility.Collapsed;

        if (!_eventsHooked)
        {
            _eventsHooked = true;
            ParentTree.SelectionMode = TreeViewSelectionMode.Single;
            ParentTree.ItemInvoked += Tree_ItemInvoked;
            ParentTree.SelectionChanged += Tree_SelectionChanged;
            TreePanel.SearchDialogButtonClick += TreePanel_SearchDialogButtonClick;
        }

        UpdateFlyoutSearchDialogButton();
        UpdateDisplay(SelectedParentId);
    }

    private void TreePanel_SearchDialogButtonClick(object? sender, EventArgs e) =>
        OpenDialogButton_Click(this, new RoutedEventArgs());

    private TreeView ParentTree => TreePanel.TreeViewControl;

    private void EnsureLabels()
    {
        if (string.IsNullOrEmpty(DialogButtonLabel))
            DialogButtonLabel = ResourceStrings.Get("LocationPicker_OpenDialog");
        if (string.IsNullOrEmpty(DialogTitle))
            DialogTitle = ResourceStrings.Get("LocationPicker_DialogTitle");
        if (string.IsNullOrEmpty(SearchPlaceholder))
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder");
        if (string.IsNullOrEmpty(NoResultsMessage))
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults");
        if (string.IsNullOrEmpty(ClearSearchLabel))
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch");
    }

    // ── Debounced tree rebuild ────────────────────────────────────────────────

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

    private void RebuildTree(string reason)
    {
        Log($"RebuildTree({reason}) START, IsDialogHost={IsDialogHost}, treeItems={TreeItems?.Count ?? -1}");
        using var step = PerfDebug.Step(nameof(LocationPicker), $"RebuildTree({reason})");
        try
        {
            Log("RebuildTree: calling TreePanel.RefreshFilter");
            using (PerfDebug.Step(nameof(LocationPicker), "TreePanel.RefreshFilter"))
                TreePanel.RefreshFilter(reason);
            Log($"RebuildTree: RefreshFilter done, filteredCount={TreePanel.FilteredTreeItems?.Count ?? -1}");
            Log("RebuildTree: calling ApplySelectionToTree");
            using (PerfDebug.Step(nameof(LocationPicker), "ApplySelectionToTree"))
                ApplySelectionToTree();
            Log("RebuildTree: ApplySelectionToTree done");
        }
        catch (Exception ex)
        {
            PerfDebug.Mark(nameof(LocationPicker), $"RebuildTree ERROR {ex.Message}");
            Log($"RebuildTree EXCEPTION: {ex}");
        }
        finally
        {
            Log("RebuildTree END");
            // Отключаем кольцо после следующего кадра (после рендера дерева)
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
                () => IsTreeLoading = false);
        }
    }

    private void TreePanel_FilteredTreeChanged(object? sender, EventArgs e) =>
        ApplySelectionToTree();

    // ── Selection change callbacks ────────────────────────────────────────────

    private void OnSelectedParentIdChanged(DependencyPropertyChangedEventArgs e)
    {
        if (_isProgrammaticChange)
            return;

        UpdateDisplay(SelectedParentId);
        ApplySelectionToTree();
    }

    private void NotifyChromeChanged()
    {
        OnPropertyChanged(nameof(ShowSummaryPanel));
        OnPropertyChanged(nameof(ShowDialogButtonInSearchRow));
        OnPropertyChanged(nameof(ShowClearButton));
        OnPropertyChanged(nameof(ShowInlineTree));
        InlineTreeGrid.Visibility = ShowInlineTree ? Visibility.Visible : Visibility.Collapsed;
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── Tree interaction ──────────────────────────────────────────────────────

    private bool _skipNextSelectionChanged;

    private void Tree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (_isProgrammaticChange || args.InvokedItem is not LocationTreeItem item)
            return;

        if (DisabledNodeIds?.Contains(item.Id) == true)
            return;

        args.Handled = true;
        _skipNextSelectionChanged = true;
        HandleUserPick(item.Id);
    }

    private void Tree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (_isProgrammaticChange)
            return;

        if (_skipNextSelectionChanged)
        {
            _skipNextSelectionChanged = false;
            return;
        }

        if (sender.SelectedItem is not LocationTreeItem item)
        {
            if (AllowClearSelection)
                HandleUserPick(null);
            else
                ApplySelectionToTree();
            return;
        }

        if (DisabledNodeIds?.Contains(item.Id) == true)
        {
            ApplySelectionToTree();
            return;
        }

        HandleUserPick(item.Id);
    }

    private void HandleUserPick(Guid? id)
    {
        if (IsDialogHost)
        {
            // Диалог — выбор ещё не подтверждён (нажатие «Выбрать»)
            SetSelectedIdSilent(id);
            UpdateDisplay(id);
            return;
        }

        // Flyout и обычный режим — немедленно коммитим и уведомляем снаружи
        CommitUserSelection(id, "UserPick");
    }

    private void CommitUserSelection(Guid? id, string reason)
    {
        Log($"Commit({reason}) id={id}");
        SetSelectedIdSilent(id);
        UpdateDisplay(id);
        ApplySelectionToTree();
        LocationSelected?.Invoke(this, id);
    }

    private void ApplySelectionToTree()
    {
        Log($"ApplySelectionToTree START, selectedId={SelectedParentId}");
        _isProgrammaticChange = true;
        _skipNextSelectionChanged = true;
        try
        {
            var items = TreePanel.FilteredTreeItems;
            Log($"ApplySelectionToTree: filteredItems={items?.Count ?? -1}, rootNodes={ParentTree.RootNodes.Count}");
            if (SelectedParentId is Guid id)
            {
                Log("ApplySelectionToTree: calling FindItem");
                var found = FindItem(items, id, 0);
                Log($"ApplySelectionToTree: FindItem done, found={found is not null}");
                if (found is not null)
                {
                    Log("ApplySelectionToTree: calling ExpandAncestorsOf");
                    ExpandAncestorsOf(items, id);
                    Log("ApplySelectionToTree: ExpandAncestorsOf done, setting SelectedItem");
                    ParentTree.SelectedItem = found;
                }
                else
                {
                    ParentTree.SelectedItem = null;
                }
            }
            else
            {
                ParentTree.SelectedItem = null;
            }
        }
        finally
        {
            _isProgrammaticChange = false;
            Log("ApplySelectionToTree END");
        }
    }

    private void ExpandAncestorsOf(IReadOnlyList<LocationTreeItem>? items, Guid targetId)
    {
        if (items is null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.Id == targetId) return;

            if (ContainsDescendant(item.Children, targetId, 0) && i < ParentTree.RootNodes.Count)
            {
                ParentTree.RootNodes[i].IsExpanded = true;
                return;
            }
        }
    }

    private static bool ContainsDescendant(IList<LocationTreeItem> items, Guid id, int depth)
    {
        if (depth > 64)
        {
            Log($"ContainsDescendant: DEPTH LIMIT EXCEEDED at depth={depth}, id={id} — вероятен цикл в дереве локаций");
            return false;
        }

        foreach (var item in items)
        {
            if (item.Id == id || ContainsDescendant(item.Children, id, depth + 1))
                return true;
        }
        return false;
    }

    private void SetSelectedIdSilent(Guid? id)
    {
        if (SelectedParentId == id)
            return;

        _isProgrammaticChange = true;
        try { SelectedParentId = id; }
        finally { _isProgrammaticChange = false; }
    }

    private void UpdateDisplay(Guid? id)
    {
        if (id is Guid gid)
        {
            if (LocationPaths?.TryGetValue(gid, out var path) == true)
            {
                SelectedLocationDisplay = path;
                return;
            }

            var item = FindItem(TreeItems, gid, 0);
            SelectedLocationDisplay = item?.Name ?? gid.ToString();
            return;
        }

        SelectedLocationDisplay = string.IsNullOrEmpty(NotSelectedLabel)
            ? ResourceStrings.Get("LocationPicker_NotSelected")
            : NotSelectedLabel;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void ClearParent_Click(object sender, RoutedEventArgs e)
        => CommitUserSelection(null, "ClearParent");

    private async void OpenDialogButton_Click(object sender, RoutedEventArgs e)
    {
        Log($"OpenDialogButton_Click: XamlRoot={(XamlRoot is null ? "null" : "set")}, isDialogOpen={_isDialogOpen}");
        if (XamlRoot is null || _isDialogOpen)
            return;

        _isDialogOpen = true;
        try
        {
            PopupDismissHelper.CloseAncestorPopups(this);
            await Task.Yield();
            await ShowPickerDialogAsync(XamlRoot);
        }
        finally { _isDialogOpen = false; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LocationTreeItem? FindItem(IEnumerable<LocationTreeItem>? items, Guid id, int depth)
    {
        if (items is null)
            return null;

        if (depth > 64)
        {
            Log($"FindItem: DEPTH LIMIT EXCEEDED at depth={depth}, id={id} — вероятен цикл в дереве локаций");
            return null;
        }

        foreach (var item in items)
        {
            if (item.Id == id)
                return item;

            var found = FindItem(item.Children, id, depth + 1);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static void Log(string msg) =>
        PerfDebug.Mark(nameof(LocationPicker), msg);
}
