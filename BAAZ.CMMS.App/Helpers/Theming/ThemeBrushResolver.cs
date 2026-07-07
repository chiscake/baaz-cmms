using System;
using System.Collections.Generic;
using System.Diagnostics;

using Helpers.Microsoft;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// ThemeResource-кисти для code-behind: обход ThemeDictionaries с ключами Light/Default/Dark.
/// Всегда возвращает новый <see cref="SolidColorBrush"/> — shared brush из Application.Current
/// не обновляется при смене темы (microsoft-ui-xaml#7663, #9464).
/// </summary>
internal static class ThemeBrushResolver
{
    private static readonly HashSet<string> LoggedKeys = [];

    private static readonly string[] LightThemeDictionaryKeys = ["Light", "Default"];
    private static readonly string[] DarkThemeDictionaryKeys = ["Dark"];

    public static Brush Resolve(string resourceKey, ElementTheme theme = ElementTheme.Default)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            return Transparent;

        var effectiveTheme = theme == ElementTheme.Default ? ThemeHelper.ActualTheme : theme;

        if (TryResolveFromThemeDictionaries(resourceKey, effectiveTheme, out var brush))
        {
            LogResolved(resourceKey, effectiveTheme, brush, "ThemeDictionaries");
            return brush;
        }

        // Не используем Application.Current.Resources как основной путь — там баг с темой.
        var fallback = CreateFallbackBrush(resourceKey, effectiveTheme);
        LogResolved(resourceKey, effectiveTheme, fallback, "Fallback");
        return fallback;
    }

    public static Brush Resolve(string resourceKey, FrameworkElement context) =>
        Resolve(resourceKey, context.ActualTheme);

    private static bool TryResolveFromThemeDictionaries(string resourceKey, ElementTheme theme, out Brush brush)
    {
        brush = null!;
        var themeKeys = IsDarkTheme(theme) ? DarkThemeDictionaryKeys : LightThemeDictionaryKeys;

        foreach (var dictionary in EnumerateResourceDictionaries(Application.Current.Resources))
        {
            if (dictionary.ThemeDictionaries.Count == 0)
                continue;

            foreach (var themeKey in themeKeys)
            {
                if (!dictionary.ThemeDictionaries.TryGetValue(themeKey, out var themedResources)
                    || themedResources is not ResourceDictionary themedDictionary)
                {
                    continue;
                }

                if (TryCreateSolidBrush(themedDictionary, resourceKey, out brush))
                    return true;
            }
        }

        return false;
    }

    private static bool TryCreateSolidBrush(ResourceDictionary dictionary, string resourceKey, out Brush brush)
    {
        brush = null!;

        foreach (var key in CandidateKeys(resourceKey))
        {
            if (!dictionary.TryGetValue(key, out var resource) || resource is null)
                continue;

            if (resource is Color color)
            {
                brush = new SolidColorBrush(color);
                return true;
            }

            if (resource is SolidColorBrush solid)
            {
                brush = new SolidColorBrush(solid.Color);
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> CandidateKeys(string resourceKey)
    {
        yield return resourceKey;

        if (resourceKey.EndsWith("Brush", StringComparison.Ordinal))
            yield return resourceKey[..^"Brush".Length];
    }

    private static Brush CreateFallbackBrush(string resourceKey, ElementTheme theme)
    {
        var isDark = IsDarkTheme(theme);

        var color = resourceKey switch
        {
            "TextFillColorPrimaryBrush" or "TextFillColorPrimary"
                or "HeaderText" or "AccentTextFillColorPrimaryBrush" or "AccentTextFillColorPrimary"
                => isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 0, 0, 0),
            "TextFillColorSecondaryBrush" or "TextFillColorSecondary"
                => isDark ? Color.FromArgb(255, 200, 200, 200) : Color.FromArgb(255, 96, 96, 96),
            "DividerStrokeColorDefaultBrush" or "DividerStrokeColorDefault"
            or "SurfaceStrokeColorDefaultBrush" or "SurfaceStrokeColorDefault"
            or "CardStrokeColorDefaultBrush" or "CardStrokeColorDefault"
                => isDark ? Color.FromArgb(255, 60, 60, 60) : Color.FromArgb(255, 210, 210, 210),
            "CardBackgroundFillColorDefaultBrush" or "CardBackgroundFillColorDefault"
                => isDark ? Color.FromArgb(255, 44, 44, 44) : Color.FromArgb(255, 255, 255, 255),
            "ControlFillColorSecondaryBrush" or "ControlFillColorSecondary"
            or "SubtleFillColorSecondaryBrush" or "SubtleFillColorSecondary"
            or "ControlAltFillColorSecondaryBrush" or "ControlAltFillColorSecondary"
                => isDark ? Color.FromArgb(255, 50, 50, 50) : Color.FromArgb(255, 243, 243, 243),
            "SubtleFillColorTertiaryBrush" or "SubtleFillColorTertiary"
                => isDark ? Color.FromArgb(255, 58, 58, 58) : Color.FromArgb(255, 235, 235, 235),
            "AccentFillColorDefaultBrush" or "AccentFillColorDefault"
                => Color.FromArgb(255, 0, 103, 192),
            _ => isDark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 0, 0, 0),
        };

        return new SolidColorBrush(color);
    }

    private static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries(ResourceDictionary dictionary)
    {
        yield return dictionary;

        foreach (var merged in dictionary.MergedDictionaries)
        {
            foreach (var nested in EnumerateResourceDictionaries(merged))
                yield return nested;
        }
    }

    private static bool IsDarkTheme(ElementTheme theme) =>
        (theme == ElementTheme.Default ? ThemeHelper.ActualTheme : theme) == ElementTheme.Dark;

    private static void LogResolved(string key, ElementTheme theme, Brush brush, string source)
    {
        if (!LoggedKeys.Add($"{key}|{theme}|{source}"))
            return;

        var colorText = brush is SolidColorBrush solid
            ? $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}"
            : brush.GetType().Name;

        Debug.WriteLine($"[ThemeBrush] {key} theme={theme} via={source} color={colorText}");
    }

    private static Brush Transparent { get; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
}
