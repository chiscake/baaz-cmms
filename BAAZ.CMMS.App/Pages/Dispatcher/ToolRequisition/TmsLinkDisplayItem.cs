using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed class TmsLinkDisplayItem
{
    public required string WarehouseName { get; init; }

    public Guid TmsRequisitionId { get; init; }

    public string TmsRequisitionIdShort =>
        TmsRequisitionId == Guid.Empty ? "—" : TmsRequisitionId.ToString()[..8] + "…";

    public required string Status { get; init; }

    public required string StatusLabel { get; init; }

    public string? CancelledBy { get; init; }

    public bool IsCancelledByStorekeeper =>
        string.Equals(CancelledBy, "storekeeper", StringComparison.OrdinalIgnoreCase);

    public string? StorekeeperCancelBanner { get; init; }

    public required DateTimeOffset? LastSyncedAt { get; init; }
}
