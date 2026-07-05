using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal readonly record struct NavLeaf(
    string SectionTag,
    string? PageKey,
    string TitleResourceKey,
    Symbol Icon);
