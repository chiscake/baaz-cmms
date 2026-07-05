using BAAZ.CMMS.App.Navigation;

namespace BAAZ.CMMS.App.Controls.Home;

internal static class HomeDashboardNavExtensions
{
    internal static void AddDashboardAction(
        this HomeDashboardSectionViewModel viewModel,
        NavLeaf leaf,
        bool isPrimary = false)
    {
        if (string.IsNullOrEmpty(leaf.PageKey))
        {
            return;
        }

        viewModel.AddDashboardAction(
            leaf.TitleResourceKey,
            NavSymbolGlyph.Get(leaf.Icon),
            leaf.PageKey,
            isPrimary);
    }

    internal static void AddDashboardAction(
        this HomeDashboardSectionViewModel viewModel,
        string titleResourceKey,
        string glyph,
        string pageKey,
        bool isPrimary = false)
    {
        viewModel.AddAction(titleResourceKey, glyph, pageKey, isPrimary);
    }

    internal static void AddDashboardNavLink(this HomeDashboardSectionViewModel viewModel, NavLeaf leaf)
    {
        if (string.IsNullOrEmpty(leaf.PageKey))
        {
            return;
        }

        viewModel.AddNavLink(leaf.TitleResourceKey, leaf.PageKey);
    }
}
