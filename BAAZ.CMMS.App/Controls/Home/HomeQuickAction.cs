using System.Windows.Input;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed class HomeQuickAction
{
    public required string Title { get; init; }

    public required string Glyph { get; init; }

    public required string PageKey { get; init; }

    public bool IsPrimary { get; init; }

    public ICommand? NavigateCommand { get; init; }
}
