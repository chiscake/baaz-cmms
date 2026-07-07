using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Helpers;

public static class RequestEnumLabels
{
    public static string Type(string? value) => value switch
    {
        "breakdown" => ResourceStrings.Get("RequestType_Breakdown"),
        "service" => ResourceStrings.Get("RequestType_Service"),
        "inspection" => ResourceStrings.Get("RequestType_Inspection"),
        _ => value ?? string.Empty,
    };

    public static string Priority(string? value) => value switch
    {
        "low" => ResourceStrings.Get("RequestPriority_Low"),
        "normal" => ResourceStrings.Get("RequestPriority_Normal"),
        "high" => ResourceStrings.Get("RequestPriority_High"),
        "critical" => ResourceStrings.Get("RequestPriority_Critical"),
        _ => value ?? string.Empty,
    };

    public static string RepairZone(string? value) => value switch
    {
        "on_site" => ResourceStrings.Get("RepairZone_OnSite"),
        "workshop" => ResourceStrings.Get("RepairZone_Workshop"),
        "external" => ResourceStrings.Get("RepairZone_External"),
        _ => value ?? string.Empty,
    };
}
