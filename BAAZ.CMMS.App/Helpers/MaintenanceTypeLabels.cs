using System.Collections.Generic;

using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>3 фиксированных вида ТО (UC-A5) — не динамический список.</summary>
public static class MaintenanceTypeLabels
{
    public static readonly IReadOnlyList<string> All = ["to1", "to2", "kr"];

    public static string Get(string value) => value switch
    {
        "to1" => ResourceStrings.Get("MaintenanceType_To1"),
        "to2" => ResourceStrings.Get("MaintenanceType_To2"),
        "kr" => ResourceStrings.Get("MaintenanceType_Kr"),
        _ => value,
    };

    public static string ScheduleStatus(string? value) => value switch
    {
        "scheduled" => ResourceStrings.Get("ScheduleStatus_Scheduled"),
        "in_progress" => ResourceStrings.Get("ScheduleStatus_InProgress"),
        "overdue" => ResourceStrings.Get("ScheduleStatus_Overdue"),
        "completed" => ResourceStrings.Get("ScheduleStatus_Completed"),
        "cancelled" => ResourceStrings.Get("ScheduleStatus_Cancelled"),
        _ => value ?? string.Empty,
    };
}
