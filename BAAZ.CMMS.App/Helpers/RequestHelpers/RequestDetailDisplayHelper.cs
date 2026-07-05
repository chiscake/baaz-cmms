using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.RequestHelpers;

public static class RequestDetailDisplayHelper
{
    public static string UnassignedAssigneeText => ResourceStrings.Get("Request_Assignee_None");

    public static string FormatAssigneeName(string? assigneeName) =>
        string.IsNullOrWhiteSpace(assigneeName) ? UnassignedAssigneeText : assigneeName;

    public static string FormatAsset(RequestDetailItem detail)
    {
        if (detail.IsInventoryRequest)
        {
            var name = detail.InventoryName ?? detail.InventoryId?.ToString() ?? "—";
            if (!string.IsNullOrWhiteSpace(detail.InventorySerial))
                return $"{name} ({detail.InventorySerial})";
            return name;
        }

        if (detail.AssetId is null)
            return ResourceStrings.Get("MyRequests_Detail_Asset_None");

        if (!string.IsNullOrWhiteSpace(detail.AssetNumber) && !string.IsNullOrWhiteSpace(detail.AssetName))
            return $"{detail.AssetNumber} — {detail.AssetName}";

        return detail.AssetNumber ?? detail.AssetName ?? detail.AssetId.Value.ToString();
    }
}
