namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>Контекст REP-EVT-1 для inventory-заявок (контур А).</summary>
public sealed record ToolTrackerRequestStatusContext(
    string RequestNumber,
    string PreviousStatus,
    Guid InventoryId,
    string InventoryKind = "tool");
