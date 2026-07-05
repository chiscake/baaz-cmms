using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;

public sealed class IncomingRequestRow
{
    public required Guid Id { get; init; }

    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Type { get; init; }

    public string? AssigneeName { get; init; }

    public string? RequesterName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string PriorityMarkerColorKey => StatusMarkerFactory.ForRequestPriority(Priority).ColorKey;

    public string PriorityMarkerTooltip => StatusMarkerFactory.ForRequestPriority(Priority).Tooltip;

    public string TypeLabel => RequestEnumLabels.Type(Type);

    public string TypeBadgeBackgroundKey => StatusBadgeFactory.ForRequestType(Type).BackgroundKey;

    public string TypeBadgeForegroundKey => StatusBadgeFactory.ForRequestType(Type).ForegroundKey;

    public string AssigneeText => RequestDetailDisplayHelper.FormatAssigneeName(AssigneeName);

    public string CreatedAtText => DateTimeDisplayHelper.Format(CreatedAt);

    public bool IsNew => Status == "new";

    public bool ShowOtherDepartments => !string.IsNullOrWhiteSpace(OtherDepartmentsText);

    public string DepartmentsSectionLabel => ResourceStrings.Get("RequestDetail_Section_Departments");

    public string? OtherDepartmentsText { get; init; }

    /// <summary>Хост-команды страницы — для кнопок внутри DataTemplate ListView.</summary>

    public required IncomingRequestsViewModel Page { get; init; }

    public static IncomingRequestRow FromListItem(RequestListItem item, IncomingRequestsViewModel page) => new()
    {
        Id = item.Id,
        RequestNumber = item.RequestNumber,
        Title = item.Title,
        Status = item.Status,
        Priority = item.Priority,
        Type = item.Type,
        AssigneeName = item.AssigneeName,
        RequesterName = item.RequesterName,
        CreatedAt = item.CreatedAt,
        OtherDepartmentsText = BuildOtherDepartmentsText(item.Departments, page),
        Page = page,
    };

    private static string? BuildOtherDepartmentsText(
        IReadOnlyList<RequestDepartmentItem> departments,
        IncomingRequestsViewModel page)
    {
        if (departments.Count == 0)
            return null;

        IEnumerable<RequestDepartmentItem> visible = page.IsAdmin
            ? departments
            : page.OwnRepairDepartmentId is Guid ownDepartmentId
                ? departments.Where(d => d.RepairDepartmentId != ownDepartmentId)
                : departments;

        var names = visible
            .Select(d => d.DepartmentName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return names.Length == 0 ? null : string.Join(", ", names);
    }
}
