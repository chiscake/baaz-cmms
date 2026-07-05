using Helpers.Microsoft;
using Helpers.Settings;

using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Синхронизация темы на корневых элементах окон.
/// <see cref="Application.RequestedTheme"/> намеренно не трогаем: присвоение в
/// <c>OnLaunched</c> вызывает COMException (WinUI). Тема code-behind — через
/// <see cref="ThemeBrushResolver"/> и XAML-стили с {ThemeResource}.
/// </summary>
public static class AppThemeHelper
{
    public static void ApplySavedTheme() =>
        Apply(SettingsHelper.Current.SelectedAppTheme);

    public static void Apply(ElementTheme theme)
    {
        ThemeHelper.RootTheme = theme;

        var captionTheme = theme == ElementTheme.Default ? ThemeHelper.ActualTheme : theme;
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is FrameworkElement root)
                root.RequestedTheme = theme;

            TitleBarHelper.ApplySystemThemeToCaptionButtons(window, captionTheme);
        }
    }
}
