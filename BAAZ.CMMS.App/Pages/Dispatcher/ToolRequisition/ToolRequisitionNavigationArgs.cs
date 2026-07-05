using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed class ToolRequisitionNavigationArgs
{
    public Guid? RequestId { get; init; }

    public Guid? ScheduleId { get; init; }

    public ToolRequisitionChannel? Channel { get; init; }
}
