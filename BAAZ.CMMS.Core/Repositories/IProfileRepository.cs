using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IProfileRepository
{
    Task<DataResult<ProfileModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<ProfileModel>> UpdateAsync(ProfileModel model, CancellationToken ct = default);
}
