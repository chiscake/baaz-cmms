using System;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Requester.MyRequests;

public sealed class RequestHistoryDisplayItem
{
    private const string RepairZoneChangedDbPrefix = "Изменена зона ремонта: ";

    public required string OldStatusLabel { get; init; }

    public required string NewStatusLabel { get; init; }

    public required string ChangedByName { get; init; }

    public string? Comment { get; init; }

    public required string CreatedAtText { get; init; }

    public static RequestHistoryDisplayItem From(RequestStatusHistoryItem item) => new()
    {
        OldStatusLabel = RequestStatusHelper.GetLabel(item.OldStatus),
        NewStatusLabel = RequestStatusHelper.GetLabel(item.NewStatus),
        ChangedByName = item.ChangedByName,
        Comment = FormatComment(item.Comment),
        CreatedAtText = DateTimeDisplayHelper.Format(item.CreatedAt),
    };

    private static string? FormatComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment)
            || !comment.StartsWith(RepairZoneChangedDbPrefix, StringComparison.Ordinal))
        {
            return comment;
        }

        var zoneKey = comment[RepairZoneChangedDbPrefix.Length..].Trim();
        var localizedZone = RequestEnumLabels.RepairZone(zoneKey);
        if (localizedZone == zoneKey)
            return comment;

        return ResourceStrings.Get("RequestHistory_RepairZoneChanged") + localizedZone;
    }
}
