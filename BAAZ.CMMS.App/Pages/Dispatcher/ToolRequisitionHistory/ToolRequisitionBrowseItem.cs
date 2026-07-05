using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed partial class ToolRequisitionBrowseItem : ObservableObject
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset? UpdatedAt { get; init; }

    public bool IsPinned { get; init; }

    public string StatusLabel => TmsRequisitionStatusHelper.GetLabel(Status);

    public string StatusBadgeBackgroundKey => TmsRequisitionStatusHelper.GetBadgeBackgroundKey(Status);

    public string StatusBadgeForegroundKey => TmsRequisitionStatusHelper.GetBadgeForegroundKey(Status);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public static ToolRequisitionBrowseItem FromLink(TmsToolRequisitionLinkModel link, bool isPinned = false)
    {
        var number = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId);
        var warehouse = string.IsNullOrWhiteSpace(link.WarehouseName)
            ? ResourceStrings.Get("ToolRequisitionHistory_UnknownWarehouse")
            : link.WarehouseName;

        return new ToolRequisitionBrowseItem
        {
            Id = link.Id,
            Title = $"{number} — {warehouse}",
            Status = link.LastKnownStatus,
            UpdatedAt = link.UpdatedAt ?? link.CreatedAt,
            IsPinned = isPinned,
        };
    }
}
