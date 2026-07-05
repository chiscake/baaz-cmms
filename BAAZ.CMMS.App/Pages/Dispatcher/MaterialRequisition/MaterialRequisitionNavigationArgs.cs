using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed class MaterialRequisitionNavigationArgs
{
    public Guid? RequestId { get; init; }

    public Guid? ScheduleId { get; init; }
}
