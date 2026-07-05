using System.Collections.Generic;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers.RequestHelpers;

/// <summary>
/// Заполнение списка отделов для панели деталей заявки:
/// активные отделы из <c>request_repair_departments</c> или целевой отдел до accept.
/// </summary>
public static class RequestDepartmentDisplayHelper
{
    public static IReadOnlyList<RequestDepartmentDisplayItem> BuildItems(RequestDetailItem detail)
    {
        if (detail.Departments.Count > 0)
        {
            var items = new List<RequestDepartmentDisplayItem>(detail.Departments.Count);
            foreach (var department in detail.Departments)
                items.Add(RequestDepartmentDisplayItem.From(department));

            return items;
        }

        if (!string.IsNullOrWhiteSpace(detail.TargetRepairDepartmentName))
        {
            return
            [
                new RequestDepartmentDisplayItem
                {
                    DepartmentName = detail.TargetRepairDepartmentName,
                    AssigneeText = ResourceStrings.Get("RequestDepartment_TargetPending"),
                },
            ];
        }

        return [];
    }

    public static void ReplaceCollection(
        ICollection<RequestDepartmentDisplayItem> target,
        RequestDetailItem detail)
    {
        target.Clear();
        foreach (var item in BuildItems(detail))
            target.Add(item);
    }
}
