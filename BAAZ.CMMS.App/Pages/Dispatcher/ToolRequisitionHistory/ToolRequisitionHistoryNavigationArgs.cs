using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed class ToolRequisitionHistoryNavigationArgs
{
    public string? StatusFilter { get; init; }

    public Guid? LinkId { get; init; }
}
