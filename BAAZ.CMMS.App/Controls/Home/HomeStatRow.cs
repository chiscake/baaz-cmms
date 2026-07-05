using System.Collections.ObjectModel;

namespace BAAZ.CMMS.App.Controls.Home;

public sealed class HomeStatRow
{
    public int Columns { get; init; } = 4;

    public string? Heading { get; init; }

    public bool HasHeading => !string.IsNullOrEmpty(Heading);

    public ObservableCollection<HomeStatItem> Items { get; } = [];
}
