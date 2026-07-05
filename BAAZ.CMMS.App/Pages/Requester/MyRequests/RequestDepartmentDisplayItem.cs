using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Requester.MyRequests;

public sealed class RequestDepartmentDisplayItem
{
    public required string DepartmentName { get; init; }

    public required string AssigneeText { get; init; }

    public static RequestDepartmentDisplayItem From(RequestDepartmentItem item) => new()
    {
        DepartmentName = item.DepartmentName,
        AssigneeText = RequestDetailDisplayHelper.FormatAssigneeName(item.AssigneeName),
    };
}
