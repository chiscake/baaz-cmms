using System;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;

public sealed class TmsWarehousePickerItem
{
    public required Guid WarehouseId { get; init; }

    public required string Name { get; init; }

    public override string ToString() => Name;
}
