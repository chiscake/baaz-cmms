using BAAZ.CMMS.Core.Data;

namespace BAAZ.CMMS.Core.Repositories;

public interface IProfileLocationScopeRepository
{
    Task<DataResult<IReadOnlyList<Guid>>> GetLocationIdsByProfileIdAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<DataResult> ReplaceForProfileAsync(
        Guid profileId,
        IReadOnlyList<Guid> locationIds,
        CancellationToken ct = default);
}
