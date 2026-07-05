using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.RequestHelpers;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Requester.MyRequests;

public sealed partial class RequestRow : ObservableObject, ICrudGridRow
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; } = true;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Type { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? AssigneeName { get; init; }

    public string StatusLabel => RequestStatusHelper.GetLabel(Status);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "RequestNumber" => RequestNumber,
        "Title" => Title,
        "Status" => StatusLabel,
        "Priority" => RequestEnumLabels.Priority(Priority),
        "Type" => RequestEnumLabels.Type(Type),
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "Assignee" => RequestDetailDisplayHelper.FormatAssigneeName(AssigneeName),
        "Id" => Id.ToString(),
        _ => null,
    };

    public static RequestRow FromListItem(RequestListItem item) => new()
    {
        Id = item.Id,
        IsActive = true,
        RequestNumber = item.RequestNumber,
        Title = item.Title,
        Status = item.Status,
        Priority = item.Priority,
        Type = item.Type,
        CreatedAt = item.CreatedAt,
        AssigneeName = item.AssigneeName,
    };
}
