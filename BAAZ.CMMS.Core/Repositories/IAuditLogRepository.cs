using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;

namespace BAAZ.CMMS.Core.Repositories;

public interface IAuditLogRepository
{
    Task<DataResult<IReadOnlyList<AuditLogRowDto>>> ListRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
}
