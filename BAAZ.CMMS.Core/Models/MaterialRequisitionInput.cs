namespace BAAZ.CMMS.Core.Models;

public sealed class MaterialRequisitionLine
{
    public string? Sku { get; init; }

    public required string Name { get; init; }

    public decimal Quantity { get; init; }

    public required string Unit { get; init; }

    public string? LineNote { get; init; }
}

public sealed class MaterialRequisitionInput
{
    public Guid? RequestId { get; init; }

    public Guid? ScheduleId { get; init; }

    public required Guid TechnicianId { get; init; }

    public required string WarehouseName { get; init; }

    public required IReadOnlyList<MaterialRequisitionLine> Lines { get; init; }

    public string? Notes { get; init; }
}
