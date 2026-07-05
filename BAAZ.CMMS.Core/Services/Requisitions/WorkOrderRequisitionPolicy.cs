namespace BAAZ.CMMS.Core.Services.Requisitions;

/// <summary>
/// Окно оформления расходников/инструментов: подготовка + дозаказ в работе.
/// </summary>
public static class WorkOrderRequisitionPolicy
{
    public static bool AllowsMaterialRequisition(string requestStatus) =>
        requestStatus is "accepted" or "in_progress";

    public static bool AllowsToolRequisition(string requestStatus) =>
        requestStatus is "accepted" or "in_progress";

    public static bool AllowsMaterialRequisitionSchedule(string scheduleStatus) =>
        scheduleStatus is "scheduled" or "overdue" or "in_progress";

    public static bool AllowsToolRequisitionSchedule(string scheduleStatus) =>
        scheduleStatus is "scheduled" or "overdue" or "in_progress";

    /// <summary>
    /// CMMS → TMS whitelist: <c>in_progress</c>, <c>scheduled</c>.
    /// </summary>
    public static string MapToTmsWorkOrderStatus(string cmmsStatus) => cmmsStatus switch
    {
        "in_progress" => "in_progress",
        "accepted" or "scheduled" or "overdue" => "scheduled",
        _ => cmmsStatus,
    };
}
