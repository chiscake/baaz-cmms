using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed class TmsLinkDisplayItem
{
    public required string WarehouseName { get; init; }

    public required string Status { get; init; }

    public required string StatusLabel { get; init; }

    public required DateTimeOffset? LastSyncedAt { get; init; }
}
