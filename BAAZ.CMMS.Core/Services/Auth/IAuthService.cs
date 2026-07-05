using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

public interface IAuthService
{
    event EventHandler<UserProfile?>? ProfileChanged;

    UserProfile? CurrentProfile { get; }

    bool IsAuthenticated { get; }

    Task<AuthSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
