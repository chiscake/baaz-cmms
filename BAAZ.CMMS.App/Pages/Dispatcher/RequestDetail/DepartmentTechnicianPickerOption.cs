using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;

/// <summary>Элемент ComboBox: отдел заявки + техник этого отдела (для admin).</summary>
public sealed class DepartmentTechnicianPickerOption
{
    public required Guid RepairDepartmentId { get; init; }

    public required Guid TechnicianId { get; init; }

    public required string DisplayName { get; init; }

    public override string ToString() => DisplayName;
}
