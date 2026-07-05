using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;

public sealed partial class IncomingRequestsColumn : ObservableObject
{
    public required string StatusKey { get; init; }

    public required string BaseLabel { get; init; }

    public required string MarkerColorKey { get; init; }

    public required string MarkerTooltip { get; init; }

    public ObservableCollection<IncomingRequestRow> Rows { get; } = [];

    [ObservableProperty]
    public partial string CountText { get; private set; } = "0";

    [ObservableProperty]
    public partial bool ShowEmpty { get; private set; }

    public void RefreshHeader()
    {
        CountText = Rows.Count.ToString();
        ShowEmpty = Rows.Count == 0;
    }
}
