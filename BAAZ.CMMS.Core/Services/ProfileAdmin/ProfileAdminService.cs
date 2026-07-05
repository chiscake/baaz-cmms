using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services;

public sealed class ProfileAdminService : IProfileAdminService
{
    private readonly AdminUsersFunctionClient _functionClient;
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileLocationScopeRepository _scopeRepo;
    private readonly IAuthService _authService;

    public ProfileAdminService(
        AdminUsersFunctionClient functionClient,
        IProfileRepository profileRepo,
        IProfileLocationScopeRepository scopeRepo,
        IAuthService authService)
    {
        _functionClient = functionClient;
        _profileRepo = profileRepo;
        _scopeRepo = scopeRepo;
        _authService = authService;
    }

    public Task<DataResult<IReadOnlyList<ProfileListItem>>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(out var error))
            return Task.FromResult(DataResult<IReadOnlyList<ProfileListItem>>.Fail(error!));

        return _functionClient.ListAsync(cancellationToken);
    }

    public Task<DataResult<ProfileListItem>> CreateUserAsync(
        CreateUserInput input, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(out var error))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(error!));

        if (string.Equals(input.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_AdminRoleForbidden")));

        if (string.IsNullOrWhiteSpace(input.Email))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_EmailRequired")));

        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 6)
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_PasswordTooShort")));

        if (string.IsNullOrWhiteSpace(input.FullName))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_FullNameRequired")));

        if (string.Equals(input.Role, "dispatcher", StringComparison.OrdinalIgnoreCase)
            && !input.RepairDepartmentId.HasValue)
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_DepartmentRequired")));

        return CreateUserInternalAsync(input, cancellationToken);
    }

    private async Task<DataResult<ProfileListItem>> CreateUserInternalAsync(
        CreateUserInput input, CancellationToken cancellationToken)
    {
        var result = await _functionClient.CreateAsync(input, cancellationToken);
        if (!result.IsSuccess)
            return result;

        if (RoleUsesLocationScopes(input.Role)
            && input.LocationScopeIds.Count > 0)
        {
            var scopeResult = await _scopeRepo.ReplaceForProfileAsync(
                result.Value!.Id,
                input.LocationScopeIds,
                cancellationToken);

            if (!scopeResult.IsSuccess)
                return DataResult<ProfileListItem>.Fail(scopeResult.Error!);
        }

        return await EnrichWithScopesAsync(result.Value!, cancellationToken);
    }

    public async Task<DataResult<ProfileListItem>> UpdateProfileAsync(
        Guid profileId, ProfileEditInput input, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(out var error))
            return DataResult<ProfileListItem>.Fail(error!);

        if (string.Equals(input.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_AdminRoleForbidden"));

        if (string.IsNullOrWhiteSpace(input.FullName))
            return DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_FullNameRequired"));

        if (string.Equals(input.Role, "dispatcher", StringComparison.OrdinalIgnoreCase)
            && !input.RepairDepartmentId.HasValue)
            return DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_DepartmentRequired"));

        var existing = await _profileRepo.GetByIdAsync(profileId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<ProfileListItem>.Fail(existing.Error!);

        if (string.Equals(existing.Value!.Role, "admin", StringComparison.OrdinalIgnoreCase)
            && profileId != _authService.CurrentProfile?.Id)
            return DataResult<ProfileListItem>.Fail(DataError.Unauthorized());

        var model = existing.Value!;
        model.FullName = input.FullName.Trim();
        model.Role = input.Role;
        model.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        model.LocationId = input.LocationId;
        model.RepairDepartmentId = string.Equals(input.Role, "dispatcher", StringComparison.OrdinalIgnoreCase)
            ? input.RepairDepartmentId
            : null;

        var updateResult = await _profileRepo.UpdateAsync(model, cancellationToken);
        if (!updateResult.IsSuccess)
            return DataResult<ProfileListItem>.Fail(updateResult.Error!);

        if (input.LocationScopeIds is not null)
        {
            var scopeIds = RoleUsesLocationScopes(input.Role)
                ? input.LocationScopeIds
                : [];

            var scopeResult = await _scopeRepo.ReplaceForProfileAsync(
                profileId,
                scopeIds,
                cancellationToken);
            if (!scopeResult.IsSuccess)
                return DataResult<ProfileListItem>.Fail(scopeResult.Error!);
        }

        var mapped = MapFromModel(updateResult.Value!);
        return await EnrichWithScopesAsync(mapped, cancellationToken);
    }

    private async Task<DataResult<ProfileListItem>> EnrichWithScopesAsync(
        ProfileListItem item,
        CancellationToken cancellationToken)
    {
        if (!RoleUsesLocationScopes(item.Role))
            return DataResult<ProfileListItem>.Ok(item);

        var scopes = await _scopeRepo.GetLocationIdsByProfileIdAsync(item.Id, cancellationToken);
        if (!scopes.IsSuccess)
            return DataResult<ProfileListItem>.Fail(scopes.Error!);

        return DataResult<ProfileListItem>.Ok(new ProfileListItem
        {
            Id = item.Id,
            Email = item.Email,
            FullName = item.FullName,
            Role = item.Role,
            Phone = item.Phone,
            LocationId = item.LocationId,
            LocationName = item.LocationName,
            LocationScopeIds = scopes.Value!,
            LocationScopeLabels = item.LocationScopeLabels,
            RepairDepartmentId = item.RepairDepartmentId,
            RepairDepartmentName = item.RepairDepartmentName,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            IsBanned = item.IsBanned,
            IsAdminAccount = item.IsAdminAccount,
        });
    }

    public Task<DataResult<ProfileListItem>> UpdateUserEmailAsync(
        Guid userId, string email, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(out var error))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(error!));

        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_EmailRequired")));

        if (_authService.CurrentProfile?.Id == userId)
            return Task.FromResult(DataResult<ProfileListItem>.Fail(
                DataError.Validation("Users_Validation_SelfActionForbidden")));

        return UpdateUserEmailInternalAsync(userId, email.Trim(), cancellationToken);
    }

    private async Task<DataResult<ProfileListItem>> UpdateUserEmailInternalAsync(
        Guid userId, string email, CancellationToken ct)
    {
        var existing = await _profileRepo.GetByIdAsync(userId, ct);
        if (!existing.IsSuccess)
            return DataResult<ProfileListItem>.Fail(existing.Error!);

        if (string.Equals(existing.Value!.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return DataResult<ProfileListItem>.Fail(DataError.Unauthorized());

        var result = await _functionClient.UpdateEmailAsync(userId, email, ct);
        if (!result.IsSuccess)
            return result;

        var model = existing.Value!;
        return DataResult<ProfileListItem>.Ok(new ProfileListItem
        {
            Id = userId,
            Email = string.IsNullOrEmpty(result.Value!.Email) ? email : result.Value.Email,
            FullName = model.FullName ?? string.Empty,
            Role = model.Role,
            Phone = model.Phone,
            LocationId = model.LocationId,
            RepairDepartmentId = model.RepairDepartmentId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsBanned = false,
            IsAdminAccount = false,
        });
    }

    public Task<DataResult> BanUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => MutateUserAsync(userId, (id, ct) => _functionClient.BanAsync(id, ct), cancellationToken);

    public Task<DataResult> UnbanUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => MutateUserAsync(userId, (id, ct) => _functionClient.UnbanAsync(id, ct), cancellationToken);

    public Task<DataResult> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Id == userId)
            return Task.FromResult(DataResult.Fail(DataError.Validation("Users_Validation_SelfActionForbidden")));

        return MutateUserAsync(userId, (id, ct) => _functionClient.DeleteAsync(id, ct), cancellationToken);
    }

    private async Task<DataResult> MutateUserAsync(
        Guid userId,
        Func<Guid, CancellationToken, Task<DataResult>> action,
        CancellationToken ct)
    {
        if (!EnsureAdmin(out var error))
            return DataResult.Fail(error!);

        if (_authService.CurrentProfile?.Id == userId)
            return DataResult.Fail(DataError.Validation("Users_Validation_SelfActionForbidden"));

        var existing = await _profileRepo.GetByIdAsync(userId, ct);
        if (!existing.IsSuccess)
            return DataResult.Fail(existing.Error!);

        if (string.Equals(existing.Value!.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return DataResult.Fail(DataError.Unauthorized());

        return await action(userId, ct);
    }

    private bool EnsureAdmin(out DataError? error)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
        {
            error = DataError.Unauthorized();
            return false;
        }

        error = null;
        return true;
    }

    private static bool RoleUsesLocationScopes(string? role) =>
        string.Equals(role, "requester", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "dispatcher", StringComparison.OrdinalIgnoreCase);

    private static ProfileListItem MapFromModel(ProfileModel model) => new()
    {
        Id = model.Id,
        Email = string.Empty,
        FullName = model.FullName ?? string.Empty,
        Role = model.Role,
        Phone = model.Phone,
        LocationId = model.LocationId,
        RepairDepartmentId = model.RepairDepartmentId,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt,
        IsBanned = false,
        IsAdminAccount = string.Equals(model.Role, "admin", StringComparison.OrdinalIgnoreCase),
    };
}
