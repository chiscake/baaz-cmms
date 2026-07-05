using System;
using System.Collections.Generic;

using BAAZ.CMMS.App.Helpers;

using Helpers.Microsoft;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

/// <summary>ThemeResource-ключи палитры графика ТО.</summary>
internal static class ScheduleChartTheme
{
    public const string ChartBackground = "CardBackgroundFillColorDefaultBrush";
    public const string ChartBorder = "CardStrokeColorDefaultBrush";
    public const string HeaderBackground = "ControlFillColorSecondaryBrush";
    public const string Divider = "DividerStrokeColorDefaultBrush";
    public const string HeaderText = "TextFillColorPrimaryBrush";
    public const string RowGroupBackground = "SubtleFillColorSecondaryBrush";
    public const string RowHover = "SubtleFillColorTertiaryBrush";
    public const string RowSelected = "ControlAltFillColorSecondaryBrush";
    public const string GridDayLine = "DividerStrokeColorDefaultBrush";
    public const string GridMonthLine = "SurfaceStrokeColorDefaultBrush";
    public const string WeekendBackground = "ControlAltFillColorSecondaryBrush";
    public const string TodayLine = "AccentFillColorDefaultBrush";
    public const string TextPrimary = "TextFillColorPrimaryBrush";
    public const string TextSecondary = "TextFillColorSecondaryBrush";
    public const string TextDisabled = "TextFillColorDisabledBrush";
    public const string AccentText = "AccentTextFillColorPrimaryBrush";

    private static ElementTheme _cachedTheme;
    private static readonly Dictionary<string, Brush> CachedBrushes = new(StringComparer.Ordinal);

    public static void ClearBrushCache()
    {
        CachedBrushes.Clear();
        _cachedTheme = ElementTheme.Default;
    }

    public static Brush Brush(string key, ElementTheme theme) =>
        ThemeBrushResolver.Resolve(key, theme);

    public static Brush Brush(string key, FrameworkElement context)
    {
        var theme = context.ActualTheme;
        if (theme != _cachedTheme)
        {
            CachedBrushes.Clear();
            _cachedTheme = theme;
        }

        if (!CachedBrushes.TryGetValue(key, out var brush))
            CachedBrushes[key] = brush = ThemeBrushResolver.Resolve(key, context);

        return brush;
    }

    /// <summary>Фон строки локации и столбцов выходных — один стиль.</summary>
    public static Brush BandBackground(FrameworkElement context) =>
        RowBackground(ChartLaneRowKind.Location, context);

    /// <summary>Фон строки swimlane: локация vs объект.</summary>
    public static Brush RowBackground(ChartLaneRowKind kind, FrameworkElement context)
    {
        var theme = context.ActualTheme;
        var cacheKey = kind == ChartLaneRowKind.Location ? "row|location" : "row|asset";
        if (theme != _cachedTheme)
        {
            CachedBrushes.Clear();
            _cachedTheme = theme;
        }

        if (CachedBrushes.TryGetValue(cacheKey, out var cached))
            return cached;

        var isDark = theme == ElementTheme.Dark
            || (theme == ElementTheme.Default && ThemeHelper.ActualTheme == ElementTheme.Dark);

        var color = kind == ChartLaneRowKind.Location
            ? (isDark ? Color.FromArgb(255, 0x32, 0x32, 0x32) : Color.FromArgb(255, 0xF4, 0xF4, 0xF4))
            : (isDark ? Color.FromArgb(255, 0x28, 0x28, 0x28) : Color.FromArgb(255, 0xFD, 0xFD, 0xFD));

        CachedBrushes[cacheKey] = cached = new SolidColorBrush(color);
        return cached;
    }

    public static Brush Transparent { get; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
}
