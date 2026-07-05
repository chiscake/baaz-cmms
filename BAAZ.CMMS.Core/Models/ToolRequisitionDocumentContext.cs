namespace BAAZ.CMMS.Core.Models;

/// <summary>Обогащённые поля для печатной формы заявки на инструмент.</summary>
public sealed class ToolRequisitionDocumentContext
{
    public required string RequisitionNumber { get; init; }

    public required Guid RequisitionId { get; init; }

    public required string RepairDepartmentName { get; init; }

    public required string TechnicianFullName { get; init; }

    public string? RequestNumber { get; init; }

    public string? AssetName { get; init; }

    public string? AssetNumber { get; init; }

    public Guid? ScheduleId { get; init; }

    public string? MaintenanceType { get; init; }

    public string? MaintenanceTypeLabel { get; init; }

    public DateOnly? PlannedDate { get; init; }

    public bool IsRequestWorkOrder => RequestNumber is not null;
}
