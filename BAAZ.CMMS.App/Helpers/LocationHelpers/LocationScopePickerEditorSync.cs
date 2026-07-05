using System;
using System.Collections.Generic;
using System.Threading;

using BAAZ.CMMS.App.Controls.LocationScopePicker;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>
/// Синхронизация <see cref="LocationScopePicker"/> с полями редактора CrudWorkbench (AttachTree + SetSelection).
/// Подавляет обратную запись в picker при изменении выбора пользователем.
/// </summary>
public sealed class LocationScopePickerEditorSync
{
    private const int MaxDeferredSelectionAttempts = 40;

    private readonly Page _page;
    private readonly Func<LocationScopePicker?> _getPicker;
    private readonly Func<bool> _isEditorActive;
    private readonly Func<int> _getTreeVersion;
    private readonly Func<LocationScopeTreeProjection> _getProjection;
    private readonly Func<IReadOnlyList<LocationTreeItem>> _getTreeRoots;
    private readonly Func<IReadOnlyDictionary<Guid, string>> _getFullPaths;
    private readonly Func<IReadOnlySet<Guid>> _getScopeIds;
    private readonly Action<IEnumerable<Guid>> _setScopeIds;
    private readonly string _debugTag;

    private int _syncVersion;
    private bool _isUpdatingFromPicker;
    private LocationScopePicker? _subscribedPicker;

    public LocationScopePickerEditorSync(
        Page page,
        Func<LocationScopePicker?> getPicker,
        Func<bool> isEditorActive,
        Func<int> getTreeVersion,
        Func<LocationScopeTreeProjection> getProjection,
        Func<IReadOnlyList<LocationTreeItem>> getTreeRoots,
        Func<IReadOnlyDictionary<Guid, string>> getFullPaths,
        Func<IReadOnlySet<Guid>> getScopeIds,
        Action<IEnumerable<Guid>> setScopeIds,
        string debugTag)
    {
        _page = page;
        _getPicker = getPicker;
        _isEditorActive = isEditorActive;
        _getTreeVersion = getTreeVersion;
        _getProjection = getProjection;
        _getTreeRoots = getTreeRoots;
        _getFullPaths = getFullPaths;
        _getScopeIds = getScopeIds;
        _setScopeIds = setScopeIds;
        _debugTag = debugTag;
    }

    /// <summary>Подписать picker на события выбора (до первой полной синхронизации).</summary>
    public void EnsurePickerSubscribed()
    {
        var picker = _getPicker();
        if (picker is not null)
            EnsureSubscribed(picker);
    }

    /// <summary>Поставить в очередь полную синхронизацию (дерево + выбор).</summary>
    public void QueueSync()
    {
        var version = Interlocked.Increment(ref _syncVersion);
        _ = _page.DispatcherQueue.TryEnqueue(() => SyncFull(version));
    }

    /// <summary>Обновить выбор в picker после внешнего изменения VM (загрузка записи, сброс роли).</summary>
    public void OnVmScopeIdsChanged()
    {
        if (_isUpdatingFromPicker)
            return;

        var version = Interlocked.Increment(ref _syncVersion);
        _ = _page.DispatcherQueue.TryEnqueue(() => SyncSelectionOnly(version));
    }

    private void EnsureSubscribed(LocationScopePicker picker)
    {
        if (ReferenceEquals(_subscribedPicker, picker))
            return;

        if (_subscribedPicker is not null)
        {
            _subscribedPicker.SelectionChanged -= OnPickerSelectionChanged;
            _subscribedPicker.UserSelectionChanging -= OnPickerUserSelectionChanging;
        }

        _subscribedPicker = picker;
        picker.SelectionChanged += OnPickerSelectionChanged;
        picker.UserSelectionChanging += OnPickerUserSelectionChanging;
    }

    private void OnPickerUserSelectionChanging(object? sender, EventArgs e) =>
        Interlocked.Increment(ref _syncVersion);

    private void OnPickerSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is not LocationScopePicker picker || picker.SelectedLocationIds is null)
            return;

        Interlocked.Increment(ref _syncVersion);
        _isUpdatingFromPicker = true;
        try
        {
            _setScopeIds(picker.SelectedLocationIds);
        }
        finally
        {
            _isUpdatingFromPicker = false;
        }
    }

    private void SyncFull(int version)
    {
        if (version != _syncVersion)
            return;

        var picker = _getPicker();
        if (picker is null || !_isEditorActive())
            return;

        EnsureSubscribed(picker);

        var treeVersion = _getTreeVersion();
        if (treeVersion == 0)
            return;

        var treeAlreadyAttached = picker.AttachedVersion == treeVersion;

        picker.AttachTree(
            _getProjection(),
            _getTreeRoots(),
            _getFullPaths(),
            treeVersion,
            $"{_debugTag}.Attach");

        if (treeAlreadyAttached)
            ApplySelection(picker, $"{_debugTag}.Selection");
        else
            QueueDeferredSelection(version);
    }

    private void SyncSelectionOnly(int version)
    {
        if (version != _syncVersion)
            return;

        var picker = _getPicker();
        if (picker is null || !_isEditorActive())
            return;

        EnsureSubscribed(picker);

        if (picker.IsTreeLoading)
        {
            QueueDeferredSelection(version);
            return;
        }

        ApplySelection(picker, $"{_debugTag}.VmSelection");
    }

    private void QueueDeferredSelection(int syncVersion)
    {
        _ = _page.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => TryApplyDeferredSelection(syncVersion, attempt: 0));
    }

    private void TryApplyDeferredSelection(int syncVersion, int attempt)
    {
        if (syncVersion != _syncVersion)
            return;

        var picker = _getPicker();
        if (picker is null || !_isEditorActive())
            return;

        if (picker.IsTreeLoading)
        {
            if (attempt < MaxDeferredSelectionAttempts)
            {
                _ = _page.DispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    () => TryApplyDeferredSelection(syncVersion, attempt + 1));
            }

            return;
        }

        ApplySelection(picker, $"{_debugTag}.DeferredSelection");
    }

    private void ApplySelection(LocationScopePicker picker, string reason)
    {
        if (picker.SuppressExternalSelectionSync)
            return;

        if (IsPickerInSyncWithVm(picker))
            return;

        picker.SetSelection(_getScopeIds(), reason);
    }

    private bool IsPickerInSyncWithVm(LocationScopePicker picker)
    {
        var vmIds = _getScopeIds();
        var projection = _getProjection();
        var expanded = projection.NodesById.Count > 0 && vmIds.Count > 0
            ? LocationScopeSelectionHelper.ExpandAnchorsToSelection(vmIds, projection.NodesById)
            : new HashSet<Guid>(vmIds);

        var current = picker.SelectedLocationIds ?? new HashSet<Guid>();
        return expanded.SetEquals(current);
    }
}

