using System;

using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Единое отображение и разбор bool-ячеек CrudWorkbench (✓ / ✗).</summary>
public static class CrudBoolCellHelper
{
    public const string TrueGlyph = "✓";
    public const string FalseGlyph = "✗";

    public static string Format(bool value) => value ? TrueGlyph : FalseGlyph;

    public static string ToFilterValue(bool value) => value.ToString().ToLowerInvariant();

    public static string ToFilterDisplayValue(bool value) =>
        value ? ResourceStrings.Get("CrudFilter_True") : ResourceStrings.Get("CrudFilter_False");

    /// <summary>Разбор display-текста ячейки или локализованного значения фильтра.</summary>
    public static bool TryParseDisplay(string? display, out bool value)
    {
        if (string.IsNullOrWhiteSpace(display))
        {
            value = false;
            return false;
        }

        var trimmed = display.Trim();

        if (trimmed is TrueGlyph or "true" or "True" or "1")
        {
            value = true;
            return true;
        }

        if (trimmed is FalseGlyph or "—" or "false" or "False" or "0")
        {
            value = false;
            return true;
        }

        if (string.Equals(trimmed, ResourceStrings.Get("CrudFilter_True"), StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(trimmed, ResourceStrings.Get("CrudFilter_False"), StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }
}
