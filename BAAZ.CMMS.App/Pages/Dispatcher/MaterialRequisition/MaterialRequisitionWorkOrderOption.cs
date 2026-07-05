using System;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;

public sealed class MaterialRequisitionWorkOrderOption
{
    public required bool IsRequest { get; init; }

    public required Guid Id { get; init; }

    public required string DisplayText { get; init; }

    public string? AssigneeName { get; init; }

    public Guid? SuggestedTechnicianId { get; init; }
}
