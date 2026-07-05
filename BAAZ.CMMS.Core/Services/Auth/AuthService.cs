using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using BAAZ.CMMS.Core.Models;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace BAAZ.CMMS.Core.Services;

public sealed class AuthService(
    ISupabaseClientProvider clientProvider,
    IGotrueSessionPersistence<Session> sessionPersistence) : IAuthService
{
    private static readonly TimeSpan RemoteSignOutTimeout = TimeSpan.FromSeconds(2);

    private readonly ISupabaseClientProvider _clientProvider = clientProvider;
    private readonly IGotrueSessionPersistence<Session> _sessionPersistence = sessionPersistence;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public event EventHandler<UserProfile?>? ProfileChanged;

    public UserProfile? CurrentProfile { get; private set; }

    public bool IsAuthenticated => CurrentProfile is not null;

    public async Task<AuthSignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _clientProvider.Client.Auth.SignInWithPassword(email, password);
            if (session?.User is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                return AuthSignInResult.Fail("Auth_InvalidCredentials");
            }

            if (!Guid.TryParse(session.User.Id, out var userId))
            {
                return AuthSignInResult.Fail("Auth_ProfileNotFound");
            }

            var profile = await FetchProfileAsync(userId, session.AccessToken, cancellationToken);
            if (profile is null)
            {
                return AuthSignInResult.Fail("Auth_ProfileNotFound");
            }

            CurrentProfile = profile;
            ProfileChanged?.Invoke(this, profile);
            return AuthSignInResult.Ok();
        }
        catch
        {
            return AuthSignInResult.Fail("Auth_InvalidCredentials");
        }
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var auth = _clientProvider.Client.Auth;

        if (auth.CurrentSession?.User is null || string.IsNullOrWhiteSpace(auth.CurrentSession.AccessToken))
        {
            auth.LoadSession();
            await auth.RetrieveSessionAsync();
        }

        var session = auth.CurrentSession;
        if (session?.User is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            CurrentProfile = null;
            ProfileChanged?.Invoke(this, null);
            return false;
        }

        if (!Guid.TryParse(session.User.Id, out var userId))
        {
            CurrentProfile = null;
            ProfileChanged?.Invoke(this, null);
            return false;
        }

        var profile = await FetchProfileAsync(userId, session.AccessToken, cancellationToken);
        CurrentProfile = profile;
        ProfileChanged?.Invoke(this, profile);
        return profile is not null;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        CurrentProfile = null;
        ProfileChanged?.Invoke(this, null);
        _sessionPersistence.DestroySession();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(RemoteSignOutTimeout);
            await _clientProvider.Client.Auth.SignOut().WaitAsync(timeoutCts.Token);
        }
        catch
        {
            // Локальная сессия уже очищена; выход на сервере не обязателен.
        }
    }

    private async Task<UserProfile?> FetchProfileAsync(Guid userId, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_clientProvider.SupabaseUrl}/rest/v1/profiles?id=eq.{userId:D}&select=id,role,full_name,phone,location_id,repair_department_id,locations!profiles_location_id_fkey(name),repair_departments(name),profile_location_scopes(location_id)");

            request.Headers.TryAddWithoutValidation("apikey", _clientProvider.PublishableKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var profiles = await JsonSerializer.DeserializeAsync<List<ProfileRow>>(stream, JsonOptions, cancellationToken);
            var row = profiles?.FirstOrDefault();
            if (row is null || row.Id == Guid.Empty)
            {
                return null;
            }

            return new UserProfile
            {
                Id = row.Id,
                Role = ParseRole(row.Role),
                FullName = row.FullName,
                LocationId = row.LocationId,
                LocationName = row.Locations?.Name,
                RepairDepartmentId = row.RepairDepartmentId,
                RepairDepartmentName = row.RepairDepartments?.Name,
                LocationScopeIds = row.LocationScopes?
                    .Select(s => s.LocationId)
                    .Where(id => id != Guid.Empty)
                    .ToList() ?? [],
            };
        }
        catch
        {
            return null;
        }
    }

    private static UserRole ParseRole(string? value) => value?.ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "dispatcher" => UserRole.Dispatcher,
        _ => UserRole.Requester,
    };

    private sealed class ProfileRow
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("role")]
        public string? Role { get; init; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; init; }

        [JsonPropertyName("location_id")]
        public Guid? LocationId { get; init; }

        [JsonPropertyName("repair_department_id")]
        public Guid? RepairDepartmentId { get; init; }

        [JsonPropertyName("locations")]
        public EmbedName? Locations { get; init; }

        [JsonPropertyName("repair_departments")]
        public EmbedName? RepairDepartments { get; init; }

        [JsonPropertyName("profile_location_scopes")]
        public List<ProfileLocationScopeRow>? LocationScopes { get; init; }
    }

    private sealed class ProfileLocationScopeRow
    {
        [JsonPropertyName("location_id")]
        public Guid LocationId { get; init; }
    }

    private sealed class EmbedName
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
