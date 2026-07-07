namespace BAAZ.CMMS.Core.Models;

public sealed class AuditLogListItem
{
    public required Guid Id { get; init; }

    public required string TableName { get; init; }

    public Guid? RecordId { get; init; }

    public required string RecordKey { get; init; }

    /// <summary>INSERT, UPDATE или DELETE (PostgreSQL-семантика).</summary>
    public required string Operation { get; init; }

    public Guid? ChangedBy { get; init; }

    public required string ActorName { get; init; }

    public required DateTimeOffset ChangedAt { get; init; }

    public string? OldDataJson { get; init; }

    public string? NewDataJson { get; init; }
}
