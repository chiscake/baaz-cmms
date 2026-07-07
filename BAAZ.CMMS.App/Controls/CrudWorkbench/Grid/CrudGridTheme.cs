using BAAZ.CMMS.App.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>ThemeResource-ключи линий сетки CrudDataGrid.</summary>
internal static class CrudGridTheme
{
    /// <summary>DividerStroke читается в обеих темах лучше, чем CardStroke.</summary>
    public const string GridLine = "DividerStrokeColorDefaultBrush";

    public static Brush GridLineBrush(ElementTheme theme) =>
        ThemeBrushResolver.Resolve(GridLine, theme);

    public static Brush GridLineBrush(FrameworkElement context) =>
        ThemeBrushResolver.Resolve(GridLine, context);
}
