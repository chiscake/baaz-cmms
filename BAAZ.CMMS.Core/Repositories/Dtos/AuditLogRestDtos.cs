using System.Text.Json;
using System.Text.Json.Serialization;

namespace BAAZ.CMMS.Core.Repositories.Dtos;

public sealed class AuditLogRowDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("table_name")]
    public string? TableName { get; init; }

    [JsonPropertyName("record_id")]
    public Guid? RecordId { get; init; }

    [JsonPropertyName("record_key")]
    public string? RecordKey { get; init; }

    [JsonPropertyName("operation")]
    public string? Operation { get; init; }

    [JsonPropertyName("changed_by")]
    public Guid? ChangedBy { get; init; }

    [JsonPropertyName("changed_at")]
    public DateTimeOffset ChangedAt { get; init; }

    [JsonPropertyName("old_data")]
    public JsonElement? OldData { get; init; }

    [JsonPropertyName("new_data")]
    public JsonElement? NewData { get; init; }

    [JsonPropertyName("profiles")]
    public ProfileEmbedDto? Profiles { get; init; }
}
