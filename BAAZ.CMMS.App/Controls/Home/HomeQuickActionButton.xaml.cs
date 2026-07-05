using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed partial class HomeQuickActionButton : UserControl
{
    public HomeQuickActionButton()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ApplyFromContext();
        Loaded += (_, _) => ApplyFromContext();
        ActualThemeChanged += (_, _) => ApplyFromContext();
    }

    private void ApplyFromContext()
    {
        if (DataContext is not HomeQuickAction action)
        {
            return;
        }

        var theme = ActualTheme;

        ActionIcon.Glyph = action.Glyph;
        ActionTitle.Text = action.Title;
        ActionButton.Style = (Style)Application.Current.Resources[
            action.IsPrimary ? "HomePrimaryQuickAccessButtonStyle" : "HomeQuickAccessButtonStyle"];

        ActionIcon.ClearValue(FontIcon.StyleProperty);

        if (action.IsPrimary)
        {
            ActionTitle.ClearValue(TextBlock.ForegroundProperty);
            ActionIcon.ClearValue(FontIcon.ForegroundProperty);
            ActionChevron.ClearValue(FontIcon.ForegroundProperty);

            var foreground = ActionButton.Foreground;
            if (foreground is not null)
            {
                ActionIcon.Foreground = foreground;
                ActionChevron.Foreground = foreground;
            }
        }
        else
        {
            ActionIcon.Foreground = ThemeBrushResolver.Resolve("TextFillColorSecondaryBrush", theme);
            ActionTitle.Foreground = ThemeBrushResolver.Resolve("TextFillColorPrimaryBrush", theme);
            ActionChevron.Foreground = ThemeBrushResolver.Resolve("TextFillColorSecondaryBrush", theme);
        }
    }
}
