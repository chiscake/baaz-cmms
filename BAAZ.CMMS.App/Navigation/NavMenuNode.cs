using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal abstract record NavMenuNode;

internal sealed record NavMenuLeafNode(
    string Id,
    NavLeaf Leaf,
    string? HomePageKeyOverride = null) : NavMenuNode;

internal sealed record NavMenuGroupNode(
    string Id,
    string TitleResourceKey,
    Symbol Icon,
    bool SelectsOnInvoked,
    IReadOnlyList<NavMenuNode> Children) : NavMenuNode;
