namespace BAAZ.CMMS.Core.Models.TmsIssuance;

/// <summary>Снимок наряда CMMS для TMS-API-1 (discriminated union без null-заглушек).</summary>
public sealed class ToolRequisitionWorkOrderSnapshot
{
    public required TmsWorkOrderKind Kind { get; init; }

    public required Guid Id { get; init; }

    public string? Number { get; init; }

    /// <summary>Статус для TMS-API-1 (whitelist: <c>in_progress</c>, <c>scheduled</c>).</summary>
    public required string Status { get; init; }

    public string? Title { get; init; }

    public string? AssetName { get; init; }

    public string? LocationName { get; init; }
}
