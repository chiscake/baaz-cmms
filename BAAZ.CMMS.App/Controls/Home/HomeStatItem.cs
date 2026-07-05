using BAAZ.CMMS.App.Helpers;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed class HomeStatItem
{
    public bool IsSpacer { get; init; }

    public required string Label { get; init; }

    public required string Value { get; init; }

    public required string Glyph { get; init; }

    public StatusBadgeColorToken ValueColorToken { get; init; } = StatusBadgeColorToken.BlueGrey;

    public StatusBadgeColorToken IconColorToken { get; init; } = StatusBadgeColorToken.BlueGrey;
}
