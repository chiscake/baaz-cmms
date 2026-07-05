using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavSymbolGlyph
{
    public static string Get(Symbol symbol) => ((char)symbol).ToString();
}
