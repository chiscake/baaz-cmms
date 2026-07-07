using System.Text.Encodings.Web;
using System.Text.Json;

using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Repositories.Dtos;

namespace BAAZ.CMMS.Core.Services.AuditLog;

public sealed class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonFormatOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IReadOnlyList<AuditLogListItem>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogRepository.ListRecentAsync(limit, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return [];

        return result.Value.Select(MapToListItem).ToList();
    }

    internal static AuditLogListItem MapToListItem(AuditLogRowDto row)
        => new()
        {
            Id = row.Id,
            TableName = row.TableName ?? string.Empty,
            RecordId = row.RecordId,
            RecordKey = row.RecordKey ?? string.Empty,
            Operation = row.Operation ?? string.Empty,
            ChangedBy = row.ChangedBy,
            ActorName = row.Profiles?.FullName ?? string.Empty,
            ChangedAt = row.ChangedAt,
            OldDataJson = FormatJson(row.OldData),
            NewDataJson = FormatJson(row.NewData),
        };

    private static string? FormatJson(JsonElement? element)
    {
        if (!element.HasValue)
            return null;

        if (element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        return JsonSerializer.Serialize(element.Value, JsonFormatOptions);
    }
}
