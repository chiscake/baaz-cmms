using System;
using System.Collections.Generic;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Хранилище актуальных ширин колонок (px), разделяемое между заголовком и строками CrudDataGrid.
/// </summary>
public sealed class CrudColumnWidthStore
{
    private readonly Dictionary<string, double> _widths = new();

    public event EventHandler? WidthsChanged;

    public double Get(string columnKey, double fallback) =>
        _widths.TryGetValue(columnKey, out var w) ? w : fallback;

    public void Set(string columnKey, double width)
    {
        width = Math.Max(width, 40);
        if (_widths.TryGetValue(columnKey, out var existing) && Math.Abs(existing - width) < 0.5)
            return;

        _widths[columnKey] = width;
        WidthsChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Has(string columnKey) => _widths.ContainsKey(columnKey);
}
