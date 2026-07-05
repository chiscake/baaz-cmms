using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.LocationScopePicker;
using BAAZ.CMMS.App.Controls.LocationTree;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Модальные окна выбора локации / зон заявок.</summary>
public static class LocationPickerDialogHelper
{
    public static async Task<LocationPickerDialogResult?> ShowSingleAsync(
        LocationPickerDialogRequest request,
        XamlRoot xamlRoot)
    {
        Debug.WriteLine(
            $"[LocationPickerDialogHelper] ShowSingleAsync START treeItems={request.TreeItems?.Count ?? -1}, " +
            $"treeVersion={request.TreeVersion}, paths={request.LocationPaths?.Count ?? -1}, " +
            $"disabled={request.DisabledNodeIds?.Count ?? 0}, initialSelection={request.InitialSelection}");

        Guid? selectedId = request.InitialSelection;
        var isProgrammaticChange = false;
        var skipNextSelectionChanged = false;

        var treePanel = new LocationTreePanel
        {
            Width = 560,
            Height = 460,
            TreeItems = request.TreeItems,
            LocationPaths = request.LocationPaths ?? new Dictionary<Guid, string>(),
            ShowSearchBox = true,
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder"),
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults"),
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch"),
            TreeMinHeight = 360,
            TreeMaxHeight = 420,
        };
        treePanel.EnsureDefaultItemTemplate();

        var tree = treePanel.TreeViewControl;
        tree.SelectionMode = TreeViewSelectionMode.Single;

        void ApplySelection()
        {
            isProgrammaticChange = true;
            skipNextSelectionChanged = true;
            try
            {
                LocationTreeSelectionHelper.ApplySelection(
                    tree,
                    treePanel.FilteredTreeItems,
                    selectedId);
            }
            finally
            {
                isProgrammaticChange = false;
            }
        }

        treePanel.FilteredTreeChanged += (_, _) => ApplySelection();

        bool IsDisabled(Guid id) => request.DisabledNodeIds?.Contains(id) == true;

        tree.ItemInvoked += (_, args) =>
        {
            if (isProgrammaticChange || args.InvokedItem is not LocationTreeItem item)
                return;

            if (IsDisabled(item.Id))
                return;

            args.Handled = true;
            skipNextSelectionChanged = true;
            selectedId = item.Id;
        };

        tree.SelectionChanged += (_, _) =>
        {
            if (isProgrammaticChange)
                return;

            if (skipNextSelectionChanged)
            {
                skipNextSelectionChanged = false;
                return;
            }

            if (tree.SelectedItem is not LocationTreeItem item)
            {
                if (request.AllowClearSelection)
                    selectedId = null;
                else
                    ApplySelection();
                return;
            }

            if (IsDisabled(item.Id))
            {
                ApplySelection();
                return;
            }

            selectedId = item.Id;
        };

        Debug.WriteLine("[LocationPickerDialogHelper] creating ContentDialog (LocationTreePanel host)");
        var dialog = new ContentDialog
        {
            Title = request.Title ?? ResourceStrings.Get("LocationPicker_DialogTitle"),
            Content = treePanel,
            PrimaryButtonText = ResourceStrings.Get("LocationPicker_DialogApply"),
            CloseButtonText = ResourceStrings.Get("LocationPicker_DialogCancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        treePanel.Loaded += (_, _) =>
        {
            Debug.WriteLine("[LocationPickerDialogHelper] treePanel Loaded");
            ApplySelection();
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!request.AllowClearSelection && selectedId is null)
                args.Cancel = true;
        };

        Debug.WriteLine("[LocationPickerDialogHelper] calling dialog.ShowAsync()");
        ContentDialogResult result;
        try
        {
            result = await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LocationPickerDialogHelper] dialog.ShowAsync() EXCEPTION: {ex}");
            throw;
        }
        Debug.WriteLine($"[LocationPickerDialogHelper] dialog.ShowAsync() returned {result}");

        if (result != ContentDialogResult.Primary)
            return null;

        return new LocationPickerDialogResult { LocationId = selectedId };
    }

    public static async Task<IReadOnlySet<Guid>?> ShowMultiAsync(
        LocationScopePickerDialogRequest request,
        XamlRoot xamlRoot)
    {
        var picker = new LocationScopePicker
        {
            IsDialogHost = true,
            SearchPlaceholder = ResourceStrings.Get("LocationPicker_SearchPlaceholder"),
            NoResultsMessage = ResourceStrings.Get("LocationPicker_SearchNoResults"),
            ClearSearchLabel = ResourceStrings.Get("LocationPicker_ClearSearch"),
            DialogButtonLabel = ResourceStrings.Get("LocationPicker_OpenDialog"),
            DialogTitle = request.Title ?? ResourceStrings.Get("LocationScopePicker_DialogTitle"),
        };

        // Строим проекцию синхронно — так же, как для flyout
        if (request.TreeItems.Count > 0)
        {
            var projection = LocationScopeTreeProjection.Build(request.TreeItems, version: 1);
            picker.AttachTreeSync(
                projection,
                request.TreeItems,
                request.LocationPaths ?? new Dictionary<Guid, string>(),
                version: 1);
        }
        else
        {
            picker.TreeItems = request.TreeItems;
        }

        picker.SetSelection(request.InitialSelection);

        IReadOnlySet<Guid>? dialogResult = null;

        var dialog = new ContentDialog
        {
            Title = request.Title ?? ResourceStrings.Get("LocationScopePicker_DialogTitle"),
            Content = new Grid
            {
                Width = 600,
                Height = 520,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Children = { picker },
            },
            PrimaryButtonText = ResourceStrings.Get("LocationPicker_DialogApply"),
            CloseButtonText = ResourceStrings.Get("LocationPicker_DialogCancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        dialog.PrimaryButtonClick += (_, _) =>
        {
            dialogResult = picker.SelectedLocationIds is not null
                ? new HashSet<Guid>(picker.SelectedLocationIds)
                : new HashSet<Guid>();
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        return dialogResult;
    }
}
