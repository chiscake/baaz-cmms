namespace BAAZ.CMMS.Core.Models.TmsIssuance;

public sealed class ToolRequisitionLineInput
{
    public required Guid LineClientId { get; init; }

    public required ToolRequisitionLineKind Kind { get; init; }

    /// <summary>Обязателен при <see cref="Kind"/> = <see cref="ToolRequisitionLineKind.Catalog"/>.</summary>
    public Guid? ToolId { get; init; }

    /// <summary>Обязателен при <see cref="Kind"/> = <see cref="ToolRequisitionLineKind.FreeText"/>.</summary>
    public string? Description { get; init; }

    public int Quantity { get; init; } = 1;
}
