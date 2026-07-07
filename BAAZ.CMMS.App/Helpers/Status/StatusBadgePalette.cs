using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Разрешение Material-цветов из <see cref="SemanticColorCatalog"/> для бейджей и меток.</summary>
public static class StatusBadgePalette
{
    public static StatusBadgeColorToken DefaultBackgroundToken => StatusBadgeColorToken.BlueGrey;

    public static StatusBadgeColorToken DefaultForegroundToken => StatusBadgeColorToken.OnBadgeText;

    public static string DefaultBackgroundKey => DefaultBackgroundToken.ToString();

    public static string DefaultForegroundKey => DefaultForegroundToken.ToString();

    public static Brush ResolveBackgroundBrush(string? key, ElementTheme theme = ElementTheme.Default)
    {
        if (TryParseToken(key, out var token))
            return ResolveBackgroundBrush(token, theme);

        if (TryResolveThemeBrush(key, out var brush))
            return brush;

        return ResolveBackgroundBrush(DefaultBackgroundToken, theme);
    }

    public static Brush ResolveForegroundBrush(string? key, ElementTheme theme = ElementTheme.Default)
    {
        if (TryParseToken(key, out var token))
            return ResolveForegroundBrush(token, theme);

        if (TryResolveThemeBrush(key, out var brush))
            return brush;

        return ResolveForegroundBrush(DefaultForegroundToken, theme);
    }

    public static Brush ResolveBackgroundBrush(StatusBadgeColorToken token, ElementTheme theme = ElementTheme.Default) =>
        new SolidColorBrush(ResolveColor(token, theme));

    public static Brush ResolveForegroundBrush(StatusBadgeColorToken token, ElementTheme theme = ElementTheme.Default) =>
        new SolidColorBrush(ResolveColor(token, theme));

    public static Color ResolveColor(StatusBadgeColorToken token, ElementTheme theme = ElementTheme.Default)
    {
        if (!SemanticColorCatalog.Colors.TryGetValue(token, out var pair))
            pair = SemanticColorCatalog.Colors[DefaultBackgroundToken];

        return IsDarkTheme(theme) ? pair.Dark : pair.Light;
    }

    public static bool TryParseToken(string? key, out StatusBadgeColorToken token)
    {
        token = default;
        return !string.IsNullOrWhiteSpace(key) && Enum.TryParse(key, ignoreCase: false, out token);
    }

    private static bool TryResolveThemeBrush(string? key, out Brush brush)
    {
        brush = null!;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Brush themeBrush)
        {
            brush = themeBrush;
            return true;
        }

        return false;
    }

    private static bool IsDarkTheme(ElementTheme theme) =>
        theme switch
        {
            ElementTheme.Dark => true,
            ElementTheme.Light => false,
            _ => Application.Current?.RequestedTheme == ApplicationTheme.Dark,
        };
}
