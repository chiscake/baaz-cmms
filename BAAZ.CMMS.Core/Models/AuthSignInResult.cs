namespace BAAZ.CMMS.Core.Models;

public sealed record AuthSignInResult(bool Success, string? ErrorMessage)
{
    public static AuthSignInResult Ok() => new(true, null);

    public static AuthSignInResult Fail(string message) => new(false, message);
}
