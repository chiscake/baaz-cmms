using System;
using System.Collections.Generic;
using System.Linq;

namespace BAAZ.CMMS.App.Controls.MaintenanceScheduleChart;

public static class ScheduleMarkerPriority
{
    public static string ResolveDominantStatus(IEnumerable<(string Status, DateOnly PlannedDate)> events, DateOnly today)
    {
        var list = events.ToList();
        if (list.Count == 0)
            return "scheduled";

        if (list.Any(e => e.Status == "overdue"))
            return "overdue";

        if (list.Any(e => e.Status == "in_progress"))
            return "in_progress";

        var isFuture = list.All(e => e.PlannedDate > today);
        if (isFuture)
        {
            if (list.Any(e => e.Status == "scheduled"))
                return "scheduled";
            if (list.Any(e => e.Status == "completed"))
                return "completed";
            return "cancelled";
        }

        if (list.Any(e => e.Status == "completed"))
            return "completed";
        if (list.Any(e => e.Status == "in_progress"))
            return "in_progress";
        if (list.Any(e => e.Status == "scheduled"))
            return "scheduled";
        return "cancelled";
    }

    public static int StatusPriority(string status) => status switch
    {
        "overdue" => 0,
        "in_progress" => 1,
        "scheduled" => 2,
        "completed" => 3,
        "cancelled" => 4,
        _ => 99,
    };
}
