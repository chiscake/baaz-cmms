using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Строит стандартные MenuFlyout для заголовка колонки и для ячейки.
/// Страница может добавить свои пункты через события <see cref="CrudDataGrid"/>.
/// </summary>
public static class CrudGridContextMenuBuilder
{
    // ── Header menu ───────────────────────────────────────────────────────────

    public static MenuFlyout BuildHeaderMenu(
        CrudColumnDefinition column,
        ICrudWorkbenchViewModel? vm,
        Action? onRebuildColumns)
    {
        var flyout = new MenuFlyout();

        if (column.IsSortable && vm is not null)
        {
            var sortAsc = new MenuFlyoutItem
            {
                Text = ResourceStrings.Get("CrudGrid_SortAscending"),
                Icon = new FontIcon { Glyph = "\uE74A" },
            };
            sortAsc.Click += (_, _) =>
            {
                if (vm.SortColumnKey == column.Key && vm.SortDirection == SortDirection.Ascending)
                {
                    vm.SetSort(string.Empty); // clear
                }
                else
                {
                    vm.SetSort(column.Key);
                }
            };

            var sortDesc = new MenuFlyoutItem
            {
                Text = ResourceStrings.Get("CrudGrid_SortDescending"),
                Icon = new FontIcon { Glyph = "\uE74B" },
            };
            sortDesc.Click += (_, _) =>
            {
                if (vm.SortColumnKey == column.Key && vm.SortDirection == SortDirection.Descending)
                    vm.SetSort(string.Empty);
                else
                {
                    vm.SetSort("~" + column.Key); // ~ prefix signals descending
                }
            };

            flyout.Items.Add(sortAsc);
            flyout.Items.Add(sortDesc);
            flyout.Items.Add(new MenuFlyoutSeparator());
        }

        var copyName = new MenuFlyoutItem
        {
            Text = ResourceStrings.Get("CrudGrid_CopyColumnName"),
            Icon = new FontIcon { Glyph = "\uE8C8" },
        };
        copyName.Click += (_, _) =>
        {
            var dp = new global::Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(column.Header);
            global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
        };
        flyout.Items.Add(copyName);

        var hide = new MenuFlyoutItem
        {
            Text = ResourceStrings.Get("CrudGrid_HideColumn"),
            Icon = new FontIcon { Glyph = "\uED1A" },
        };
        hide.Click += (_, _) =>
        {
            column.IsVisible = false;
            onRebuildColumns?.Invoke();
        };
        if (!column.IsHidden)
            flyout.Items.Add(hide);

        return flyout;
    }

    // ── Cell menu ─────────────────────────────────────────────────────────────

    public static MenuFlyout BuildCellMenu(
        ICrudGridRow row,
        CrudColumnDefinition? column,
        IEnumerable<CrudColumnDefinition> columns,
        ICrudWorkbenchViewModel? vm,
        CrudPermissions? permissions,
        Action? onEditRow,
        Action? onEditCell,
        Action? onArchiveRow,
        Func<Task>? onDeleteRow = null,
        string? archiveRowLabel = null)
    {
        var flyout = new MenuFlyout();

        // Copy cell
        if (column is not null)
        {
            var cellText = row.GetCellText(column.Key);
            if (!string.IsNullOrEmpty(cellText))
            {
                var copyCell = new MenuFlyoutItem
                {
                    Text = ResourceStrings.Get("CrudGrid_CopyCell"),
                    Icon = new FontIcon { Glyph = "\uE8C8" },
                };
                copyCell.Click += (_, _) => ClipboardSetText(cellText);
                flyout.Items.Add(copyCell);
            }
        }

        // Copy row (TSV)
        var copyRow = new MenuFlyoutItem
        {
            Text = ResourceStrings.Get("CrudGrid_CopyRow"),
            Icon = new FontIcon { Glyph = "\uE8C8" },
        };
        copyRow.Click += (_, _) => ClipboardSetText(BuildRowTsv(row, columns));
        flyout.Items.Add(copyRow);

        // Filter by value
        if (column is not null && vm is not null)
        {
            var cellText = row.GetCellText(column.Key);
            if (!string.IsNullOrEmpty(cellText))
            {
                var filterBy = new MenuFlyoutItem
                {
                    Text = ResourceStrings.Get("CrudGrid_FilterByValue"),
                    Icon = new FontIcon { Glyph = "\uE71C" },
                };
                filterBy.Click += (_, _) => vm.FilterByCellValue(column.Key, cellText);
                flyout.Items.Add(filterBy);
            }
        }

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Edit row
        if (onEditRow is not null)
        {
            var editRow = new MenuFlyoutItem
            {
                Text = ResourceStrings.Get("CrudGrid_EditRow"),
                Icon = new FontIcon { Glyph = "\uE8A0" },
            };
            editRow.Click += (_, _) => onEditRow();
            flyout.Items.Add(editRow);
        }

        // Edit cell
        if (onEditCell is not null && column?.IsInlineEditable == true && column.IsComputed != true && permissions?.CanInlineEdit == true)
        {
            var editCell = new MenuFlyoutItem
            {
                Text = ResourceStrings.Get("CrudGrid_EditCell"),
                Icon = new FontIcon { Glyph = "\uE70F" },
            };
            editCell.Click += (_, _) => onEditCell();
            flyout.Items.Add(editCell);
        }

        // Archive/Restore
        if (onArchiveRow is not null && permissions?.CanArchive == true)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            var archiveLabel = archiveRowLabel ?? (row.IsActive
                ? ResourceStrings.Get("CrudGrid_ArchiveRow")
                : ResourceStrings.Get("CrudGrid_RestoreRow"));
            var archiveItem = new MenuFlyoutItem
            {
                Text = archiveLabel,
                Icon = new FontIcon { Glyph = row.IsActive ? "\uE74D" : "\uE777" },
            };
            archiveItem.Click += (_, _) => onArchiveRow();
            flyout.Items.Add(archiveItem);
        }

        if (onDeleteRow is not null && permissions?.CanHardDelete == true)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            var deleteItem = new MenuFlyoutItem
            {
                Text = ResourceStrings.Get("CrudGrid_DeleteRow"),
                Icon = new FontIcon { Glyph = "\uE74D" },
            };
            if (Application.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out var critical)
                && critical is Microsoft.UI.Xaml.Media.Brush brush)
            {
                deleteItem.Foreground = brush;
            }

            deleteItem.Click += async (_, _) => await onDeleteRow();
            flyout.Items.Add(deleteItem);
        }

        return flyout;
    }

    /// <summary>TSV только по колонкам, видимым в таблице (порядок как в гриде).</summary>
    public static string BuildRowTsv(ICrudGridRow row, IEnumerable<CrudColumnDefinition> columns)
    {
        var values = columns
            .Where(c => c.IsVisible && !c.IsHidden)
            .Select(c => row.GetCellText(c.Key) ?? string.Empty);
        return string.Join('\t', values);
    }

    private static void ClipboardSetText(string text)
    {
        var dp = new global::Windows.ApplicationModel.DataTransfer.DataPackage();
        dp.SetText(text);
        global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
    }
}
