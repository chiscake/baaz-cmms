namespace BAAZ.CMMS.Core.Models;

public sealed class RequestStatusHistoryItem
{
    public Guid Id { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string ChangedByName { get; init; }
    public string? Comment { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
