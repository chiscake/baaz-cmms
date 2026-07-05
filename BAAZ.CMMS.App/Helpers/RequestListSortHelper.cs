using System;
using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Client-side sort for request lists (Kanban columns, etc.).
/// Order: type (category) → priority → created date.
/// </summary>
public static class RequestListSortHelper
{
    /// <summary>Domain order: breakdown first, then service, then inspection.</summary>
    private static readonly string[] TypeOrder = ["breakdown", "service", "inspection"];

    /// <summary>Most urgent first: critical → high → normal → low.</summary>
    private static readonly string[] PriorityOrder = ["critical", "high", "normal", "low"];

    public static int GetTypeRank(string? value) =>
        Array.IndexOf(TypeOrder, value) is var index and >= 0 ? index : TypeOrder.Length;

    public static int GetPriorityRank(string? value) =>
        Array.IndexOf(PriorityOrder, value) is var index and >= 0 ? index : PriorityOrder.Length;

    public static IEnumerable<RequestListItem> SortForIncomingKanban(IEnumerable<RequestListItem> items) =>
        items
            .OrderBy(i => GetTypeRank(i.Type))
            .ThenBy(i => GetPriorityRank(i.Priority))
            .ThenByDescending(i => i.CreatedAt);
}
