using System;

using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;

public sealed class RequestDepartmentRow
{
    /// <summary>Ожидает принятия в отдел.</summary>
    private const string PendingGlyph = "\uE823";

    /// <summary>Исполнитель не назначен.</summary>
    private const string UnassignedGlyph = "\uE716";

    /// <summary>Исполнитель назначен.</summary>
    private const string AssignedGlyph = "\uE77B";

    public required Guid RepairDepartmentId { get; init; }

    public required string DepartmentName { get; init; }

    public required string AssigneeText { get; init; }

    public required string StatusIconGlyph { get; init; }

    public bool IsOwnDepartment { get; init; }

    public static RequestDepartmentRow From(RequestDepartmentItem item, Guid? ownDepartmentId)
    {
        var assigneeText = RequestDetailDisplayHelper.FormatAssigneeName(item.AssigneeName);

        return new RequestDepartmentRow
        {
            RepairDepartmentId = item.RepairDepartmentId,
            DepartmentName = item.DepartmentName,
            AssigneeText = assigneeText,
            StatusIconGlyph = ResolveStatusIconGlyph(assigneeText),
            IsOwnDepartment = ownDepartmentId is not null && item.RepairDepartmentId == ownDepartmentId,
        };
    }

    public static string ResolveStatusIconGlyph(string assigneeText)
    {
        if (assigneeText == ResourceStrings.Get("RequestDepartment_TargetPending"))
            return PendingGlyph;

        if (assigneeText == RequestDetailDisplayHelper.UnassignedAssigneeText)
            return UnassignedGlyph;

        return AssignedGlyph;
    }
}
