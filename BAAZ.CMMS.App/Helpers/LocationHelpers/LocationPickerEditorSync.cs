using System;
using System.Collections.Generic;
using System.Threading;

using BAAZ.CMMS.App.Controls.LocationPicker;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>
/// Синхронизация <see cref="LocationPicker"/> с полями редактора CrudWorkbench (AttachTree + SetSelection).
/// Один debounced <see cref="QueueSync"/> — дерево и выбор в одном колбэке; SetSelection после первого AttachTree
/// откладывается до окончания перестройки UI.
/// </summary>
public sealed class LocationPickerEditorSync
{
    private const int MaxDeferredSelectionAttempts = 40;

    private readonly Page _page;
    private readonly Func<LocationPicker?> _getPicker;
    private readonly Func<bool> _isEditorOpen;
    private readonly Func<int> _getTreeVersion;
    private readonly Func<IReadOnlyList<LocationTreeItem>> _getTreeRoots;
    private readonly Func<IReadOnlyDictionary<Guid, string>> _getFullPaths;
    private readonly Func<Guid?> _getSelectedId;
    private readonly Action<Guid?> _setSelectedId;
    private readonly string _debugTag;

    private int _syncVersion;
    private bool _isUpdatingFromPicker;
    private LocationPicker? _subscribedPicker;

    public LocationPickerEditorSync(
        Page page,
        Func<LocationPicker?> getPicker,
        Func<bool> isEditorOpen,
        Func<int> getTreeVersion,
        Func<IReadOnlyList<LocationTreeItem>> getTreeRoots,
        Func<IReadOnlyDictionary<Guid, string>> getFullPaths,
        Func<Guid?> getSelectedId,
        Action<Guid?> setSelectedId,
        string debugTag)
    {
        _page = page;
        _getPicker = getPicker;
        _isEditorOpen = isEditorOpen;
        _getTreeVersion = getTreeVersion;
        _getTreeRoots = getTreeRoots;
        _getFullPaths = getFullPaths;
        _getSelectedId = getSelectedId;
        _setSelectedId = setSelectedId;
        _debugTag = debugTag;
    }

    private void OnLocationSelected(object? sender, Guid? locationId)
    {
        _isUpdatingFromPicker = true;
        try
        {
            _setSelectedId(locationId);
        }
        finally
        {
            _isUpdatingFromPicker = false;
        }
    }

    private void EnsureSubscribed(LocationPicker picker)
    {
        if (ReferenceEquals(_subscribedPicker, picker))
            return;

        if (_subscribedPicker is not null)
            _subscribedPicker.LocationSelected -= OnLocationSelected;

        _subscribedPicker = picker;
        picker.LocationSelected += OnLocationSelected;
    }

    /// <summary>Поставить в очередь полную синхронизацию (дерево + выбор).</summary>
    public void QueueSync()
    {
        var version = Interlocked.Increment(ref _syncVersion);
        Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.QueueSync v={version}");
        _ = _page.DispatcherQueue.TryEnqueue(() => SyncFull(version));
    }

    /// <summary>Обновить выбор в picker после внешнего изменения VM (открытие записи, сброс).</summary>
    public void OnVmSelectedIdChanged()
    {
        if (_isUpdatingFromPicker)
            return;

        var version = Interlocked.Increment(ref _syncVersion);
        _ = _page.DispatcherQueue.TryEnqueue(() => SyncSelectionOnly(version));
    }

    private void SyncFull(int version)
    {
        Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.SyncFull START v={version}");
        if (version != _syncVersion)
        {
            Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.SyncFull SKIP stale v={version}");
            return;
        }

        var picker = _getPicker();
        if (picker is null || !_isEditorOpen())
        {
            Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.SyncFull SKIP picker={picker is not null}, editorOpen={_isEditorOpen()}");
            return;
        }

        EnsureSubscribed(picker);

        var treeVersion = _getTreeVersion();
        if (treeVersion == 0)
        {
            Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.SyncFull SKIP treeVersion=0");
            return;
        }

        var treeAlreadyAttached = picker.AttachedVersion == treeVersion;

        picker.AttachTree(
            _getTreeRoots(),
            _getFullPaths(),
            treeVersion,
            $"{_debugTag}.Attach");

        if (treeAlreadyAttached)
            picker.SetSelection(_getSelectedId(), $"{_debugTag}.Selection");
        else
            QueueDeferredSelection(version);

        Debug.WriteLine($"[{_debugTag}] LocationPickerEditorSync.SyncFull END v={version}, treeAlreadyAttached={treeAlreadyAttached}");
    }

    private void SyncSelectionOnly(int version)
    {
        if (version != _syncVersion)
            return;

        var picker = _getPicker();
        if (picker is null || !_isEditorOpen())
            return;

        EnsureSubscribed(picker);

        if (picker.IsTreeLoading)
        {
            QueueDeferredSelection(version);
            return;
        }

        picker.SetSelection(_getSelectedId(), $"{_debugTag}.VmSelection");
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
        if (picker is null || !_isEditorOpen())
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

        picker.SetSelection(_getSelectedId(), $"{_debugTag}.Selection");
    }
}
