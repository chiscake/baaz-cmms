using BAAZ.CMMS.App.Localization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Сборка заголовков колонок с бейджами PK/Unique.</summary>
public static class CrudColumnHeaderBuilder
{
    private const string PrimaryKeyGlyph = "\uE192";
    private const string UniqueGlyph = "\uEDDB";
    private const double BadgeFontSize = 12;
    private const double BadgeOpacity = 0.65;

    public static FrameworkElement BuildGridHeaderContent(CrudColumnDefinition col)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };

        AppendBadges(panel, col);

        panel.Children.Add(new TextBlock
        {
            Text = col.Header,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        if (!string.IsNullOrEmpty(col.DataTypeLabel))
        {
            panel.Children.Add(new TextBlock
            {
                Text = col.DataTypeLabel,
                FontSize = 10,
                Opacity = 0.55,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        return panel;
    }

    public static FrameworkElement BuildColumnPickerLabel(CrudColumnDefinition col)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };

        AppendBadges(panel, col);

        panel.Children.Add(new TextBlock
        {
            Text = col.Header,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        return panel;
    }

    public static void AppendBadges(Panel panel, CrudColumnDefinition col)
    {
        if (col.IsPrimaryKey)
            panel.Children.Add(CreateBadge(PrimaryKeyGlyph, ResourceStrings.Get("CrudGrid_ColumnBadge_PrimaryKey")));

        if (col.IsUnique && !col.IsPrimaryKey)
            panel.Children.Add(CreateBadge(UniqueGlyph, ResourceStrings.Get("CrudGrid_ColumnBadge_Unique")));
    }

    private static FontIcon CreateBadge(string glyph, string tooltip)
    {
        var icon = new FontIcon
        {
            Glyph = glyph,
            FontSize = BadgeFontSize,
            Opacity = BadgeOpacity,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTipService.SetToolTip(icon, tooltip);
        return icon;
    }
}
