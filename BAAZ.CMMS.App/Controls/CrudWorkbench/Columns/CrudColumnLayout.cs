using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Вспомогательные методы для ColumnDefinitions заголовка и строк CrudDataGrid.</summary>
public static class CrudColumnLayout
{
    public const double SelectColumnWidth = 24;
    public const double ExpandColumnWidth = 32;

    /// <summary>
    /// Grid-индекс для i-й видимой data-колонки (0-based): 2, 3, 4 …
    /// Структура: [Select 24] [Expand 32] [Data0] [Data1] … [DataN]
    /// </summary>
    public static int GetDataColIndex(int i) => 2 + i;

    /// <summary>
    /// Строит ColumnDefinitions для заголовка и строк (одинаковая структура).
    /// </summary>
    public static void ApplyColumnDefinitions(
        IList<ColumnDefinition> target,
        IEnumerable<CrudColumnDefinition> columns,
        CrudColumnWidthStore? store)
    {
        target.Clear();

        target.Add(new ColumnDefinition { Width = new GridLength(SelectColumnWidth, GridUnitType.Pixel) });
        target.Add(new ColumnDefinition { Width = new GridLength(ExpandColumnWidth, GridUnitType.Pixel) });

        var visible = columns.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visible.Count; i++)
        {
            var col = visible[i];
            double defaultW = col.GetEffectiveWidth();
            double w = store?.Get(col.Key, defaultW) ?? defaultW;

            target.Add(new ColumnDefinition
            {
                Width = new GridLength(w, GridUnitType.Pixel),
                MinWidth = 40,
            });
        }
    }

    // Legacy overload — kept so existing callers don't break.
    public static void ApplyColumnDefinitions(
        IList<ColumnDefinition> target,
        IEnumerable<CrudColumnDefinition> columns,
        bool includeSelectColumn)
        => ApplyColumnDefinitions(target, columns, null);
}
