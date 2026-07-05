using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

/// <summary>
/// Управление профилями пользователей (UC-A2).
/// Создание и операции auth — через Edge Function admin-users; UPDATE profiles — PostgREST.
/// </summary>
public interface IProfileAdminService
{
    Task<DataResult<IReadOnlyList<ProfileListItem>>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<DataResult<ProfileListItem>> CreateUserAsync(CreateUserInput input, CancellationToken cancellationToken = default);

    Task<DataResult<ProfileListItem>> UpdateProfileAsync(
        Guid profileId, ProfileEditInput input, CancellationToken cancellationToken = default);

    Task<DataResult<ProfileListItem>> UpdateUserEmailAsync(
        Guid userId, string email, CancellationToken cancellationToken = default);

    Task<DataResult> BanUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DataResult> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DataResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
