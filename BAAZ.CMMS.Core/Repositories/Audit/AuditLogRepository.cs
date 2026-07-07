using System.Diagnostics;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;
using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private const string ListSelect =
        "id,table_name,record_id,record_key,operation,changed_by,changed_at,old_data,new_data," +
        "profiles(full_name)";

    private readonly ISupabaseClientProvider _clientProvider;

    public AuditLogRepository(ISupabaseClientProvider clientProvider)
    {
        _clientProvider = clientProvider;
    }

    public async Task<DataResult<IReadOnlyList<AuditLogRowDto>>> ListRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var path =
            $"/rest/v1/audit_log?select={ListSelect}" +
            $"&order=changed_at.desc&limit={safeLimit}";

        try
        {
            var rows = await SupabaseRestClient.GetListAsync<AuditLogRowDto>(
                _clientProvider,
                path,
                cancellationToken);

            if (rows is null)
            {
                Debug.WriteLine("[AuditLogRepository] ListRecentAsync: null response");
                return DataResult<IReadOnlyList<AuditLogRowDto>>.Fail(
                    DataError.Unknown("Не удалось загрузить журнал изменений"));
            }

            return DataResult<IReadOnlyList<AuditLogRowDto>>.Ok(rows);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Debug.WriteLine($"[AuditLogRepository] ListRecentAsync network error: {ex}");
            return DataResult<IReadOnlyList<AuditLogRowDto>>.Fail(DataError.Network(ex.Message));
        }
    }
}
