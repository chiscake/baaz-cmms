using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

public static class MaintenanceScheduleNavBadgeCount
{
    public static int ComputeFromItems(IEnumerable<MaintenanceScheduleItem> items, DateOnly today)
        => items.Count(i => Matches(i.Status, i.PlannedDate, today));

    public static int ComputeFromModels(IEnumerable<MaintenanceScheduleModel> models, DateOnly today)
        => models.Count(m => Matches(m.Status, m.PlannedDate, today));

    private static bool Matches(string status, DateOnly plannedDate, DateOnly today)
        => status == "overdue"
           || status == "in_progress"
           || (status == "scheduled" && plannedDate == today);
}
