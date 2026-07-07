namespace BAAZ.CMMS.Core.Models;

public sealed class ToolRequisitionLine
{
    public string? InventoryNumber { get; init; }

    public required string Name { get; init; }

    public int Quantity { get; init; } = 1;

    public string? LineNote { get; init; }

    /// <summary>Заполняется при отправке в TMS (каталог).</summary>
    public Guid? ToolId { get; init; }
}

public sealed class ToolRequisitionFormInput
{
    public Guid? RequestId { get; init; }

    public Guid? ScheduleId { get; init; }

    public required Guid TechnicianId { get; init; }

    /// <summary>Текстовое имя склада (docx и отображение).</summary>
    public required string WarehouseName { get; init; }

    /// <summary>Идентификатор склада TMS (только канал TMS).</summary>
    public Guid? WarehouseId { get; init; }

    public required IReadOnlyList<ToolRequisitionLine> Lines { get; init; }

    public string? Notes { get; init; }
}
