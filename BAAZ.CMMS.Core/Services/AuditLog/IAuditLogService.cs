using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.AuditLog;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogListItem>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
