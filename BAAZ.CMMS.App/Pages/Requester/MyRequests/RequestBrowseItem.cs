using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Requester.MyRequests;

public sealed partial class RequestBrowseItem : ObservableObject
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public bool IsPinned { get; init; }

    public string StatusLabel => RequestStatusHelper.GetLabel(Status);

    public string StatusBadgeBackgroundKey => RequestStatusHelper.GetBadgeBackgroundKey(Status);

    public string StatusBadgeForegroundKey => RequestStatusHelper.GetBadgeForegroundKey(Status);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public static RequestBrowseItem FromListItem(RequestListItem item, bool isPinned = false) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Status = item.Status,
        CreatedAt = item.CreatedAt,
        IsPinned = isPinned,
    };
}
