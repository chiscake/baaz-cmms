using System.Collections.Generic;

using Windows.UI;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Единый каталог Material-цветов для бейджей и меток (light / dark).
/// </summary>
internal static class SemanticColorCatalog
{
    internal static IReadOnlyDictionary<StatusBadgeColorToken, (Color Light, Color Dark)> Colors { get; } =
        new Dictionary<StatusBadgeColorToken, (Color Light, Color Dark)>
        {
            [StatusBadgeColorToken.OnBadgeText] = (Parse("#FFFFFF"), Parse("#FFFFFF")),
            [StatusBadgeColorToken.Blue] = (Parse("#1E88E5"), Parse("#42A5F5")),
            [StatusBadgeColorToken.BlueGrey] = (Parse("#546E7A"), Parse("#78909C")),
            [StatusBadgeColorToken.Orange700] = (Parse("#F57C00"), Parse("#FB8C00")),
            [StatusBadgeColorToken.Amber] = (Parse("#F9A825"), Parse("#FBC02D")),
            [StatusBadgeColorToken.Green] = (Parse("#43A047"), Parse("#66BB6A")),
            [StatusBadgeColorToken.Teal] = (Parse("#00897B"), Parse("#26A69A")),
            [StatusBadgeColorToken.Red] = (Parse("#D32F2F"), Parse("#EF5350")),
            [StatusBadgeColorToken.Brown] = (Parse("#8D6E63"), Parse("#A1887F")),
        };

    private static Color Parse(string hex)
    {
        var value = hex.TrimStart('#');
        return Color.FromArgb(
            255,
            byte.Parse(value[..2], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value[2..4], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value[4..6], System.Globalization.NumberStyles.HexNumber));
    }
}
