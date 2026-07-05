using System.Windows.Input;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed class HomeNavLink
{
    public required string Title { get; init; }

    public required string PageKey { get; init; }

    public bool ShowSeparator { get; init; }

    public ICommand? NavigateCommand { get; init; }
}
