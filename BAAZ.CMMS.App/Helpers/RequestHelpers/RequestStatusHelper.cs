using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Helpers;

public static class RequestStatusHelper
{
    public static string GetLabel(string? status) => status switch
    {
        "new" => ResourceStrings.Get("RequestStatus_New"),
        "accepted" => ResourceStrings.Get("RequestStatus_Accepted"),
        "in_progress" => ResourceStrings.Get("RequestStatus_InProgress"),
        "completed" => ResourceStrings.Get("RequestStatus_Completed"),
        "closed" => ResourceStrings.Get("RequestStatus_Closed"),
        "rejected" => ResourceStrings.Get("RequestStatus_Rejected"),
        "cancelled" => ResourceStrings.Get("RequestStatus_Cancelled"),
        _ => status ?? string.Empty,
    };

    public static string GetBadgeBackgroundKey(string? status) =>
        StatusBadgeFactory.ForRequest(status).BackgroundKey;

    public static string GetBadgeForegroundKey(string? status) =>
        StatusBadgeFactory.ForRequest(status).ForegroundKey;

    public static string GetBadgeBrushKey(string? status) => GetBadgeBackgroundKey(status);
}
