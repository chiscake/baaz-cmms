using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase.Functions.Exceptions;

namespace BAAZ.CMMS.Core.Services;

/// <summary>Вызов Edge Function admin-users (list/create/ban/unban/delete).</summary>
public sealed class AdminUsersFunctionClient
{
    private const string FunctionName = "admin-users";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ISupabaseClientProvider _clientProvider;

    public AdminUsersFunctionClient(ISupabaseClientProvider clientProvider)
    {
        _clientProvider = clientProvider;
    }

    public Task<DataResult<IReadOnlyList<ProfileListItem>>> ListAsync(CancellationToken ct = default)
        => InvokeListAsync(ct);

    public Task<DataResult<ProfileListItem>> CreateAsync(CreateUserInput input, CancellationToken ct = default)
        => InvokeItemAsync(new
        {
            action = "create",
            email = input.Email,
            password = input.Password,
            fullName = input.FullName,
            role = input.Role,
            locationId = input.LocationId.ToString(),
            locationScopeIds = input.LocationScopeIds.Select(id => id.ToString()).ToArray(),
            phone = input.Phone,
            repairDepartmentId = input.RepairDepartmentId?.ToString(),
        }, ct);

    public Task<DataResult> BanAsync(Guid userId, CancellationToken ct = default)
        => InvokeOkAsync(new { action = "ban", userId = userId.ToString() }, ct);

    public Task<DataResult> UnbanAsync(Guid userId, CancellationToken ct = default)
        => InvokeOkAsync(new { action = "unban", userId = userId.ToString() }, ct);

    public Task<DataResult> DeleteAsync(Guid userId, CancellationToken ct = default)
        => InvokeOkAsync(new { action = "delete", userId = userId.ToString() }, ct);

    public Task<DataResult<ProfileListItem>> UpdateEmailAsync(
        Guid userId, string email, CancellationToken ct = default)
        => InvokeItemAsync(new { action = "updateEmail", userId = userId.ToString(), email }, ct);

    private async Task<DataResult<IReadOnlyList<ProfileListItem>>> InvokeListAsync(CancellationToken ct)
    {
        try
        {
            var json = await RawPostAsync(new { action = "list" }, ct);
            var response = System.Text.Json.JsonSerializer.Deserialize<ListResponse>(json, JsonOptions);
            if (response?.Items is null)
                return DataResult<IReadOnlyList<ProfileListItem>>.Fail(DataError.Unknown("Пустой ответ функции"));

            var items = response.Items.Select(MapItem).ToList();
            return DataResult<IReadOnlyList<ProfileListItem>>.Ok(items);
        }
        catch (FunctionsException ex)
        {
            return DataResult<IReadOnlyList<ProfileListItem>>.Fail(MapFunctionError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<ProfileListItem>>.Fail(DataError.Network(ex.Message));
        }
    }

    private async Task<DataResult<ProfileListItem>> InvokeItemAsync(object body, CancellationToken ct)
    {
        try
        {
            var json = await RawPostAsync(body, ct);
            var response = System.Text.Json.JsonSerializer.Deserialize<ItemResponse>(json, JsonOptions);
            if (response?.Item is null)
                return DataResult<ProfileListItem>.Fail(DataError.Unknown("Пустой ответ функции"));

            return DataResult<ProfileListItem>.Ok(MapItem(response.Item));
        }
        catch (FunctionsException ex)
        {
            return DataResult<ProfileListItem>.Fail(MapFunctionError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<ProfileListItem>.Fail(DataError.Network(ex.Message));
        }
    }

    private async Task<DataResult> InvokeOkAsync(object body, CancellationToken ct)
    {
        try
        {
            _ = await RawPostAsync(body, ct);
            return DataResult.Ok();
        }
        catch (FunctionsException ex)
        {
            return DataResult.Fail(MapFunctionError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    private async Task<string> RawPostAsync(object body, CancellationToken ct)
    {
        var client = _clientProvider.Client;
        var session = client.Auth.CurrentSession
            ?? throw new InvalidOperationException("Нет активной сессии");

        var options = new Supabase.Functions.Client.InvokeFunctionOptions
        {
            Body = ToFunctionBody(body),
            Headers = new Dictionary<string, string>
            {
                ["apikey"] = _clientProvider.PublishableKey,
            },
        };

        // Second argument becomes Authorization; do not pass PublishableKey there.
        return await client.Functions.Invoke(FunctionName, session.AccessToken, options);
    }

    private static Dictionary<string, object> ToFunctionBody(object body)
    {
        var token = JObject.FromObject(body);
        return token.Properties()
            .ToDictionary(p => p.Name, p => (object)(p.Value.Type == JTokenType.Null ? null! : p.Value));
    }

    private static ProfileListItem MapItem(ProfileDto dto) => new()
    {
        Id = Guid.Parse(dto.Id),
        Email = dto.Email ?? string.Empty,
        FullName = dto.FullName ?? string.Empty,
        Role = dto.Role ?? "requester",
        Phone = dto.Phone,
        LocationId = string.IsNullOrEmpty(dto.LocationId) ? null : Guid.Parse(dto.LocationId),
        LocationName = dto.LocationName,
        LocationScopeIds = dto.LocationScopeIds?
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(Guid.Parse)
            .ToList() ?? [],
        LocationScopeLabels = dto.LocationScopeLabels ?? [],
        RepairDepartmentId = string.IsNullOrEmpty(dto.RepairDepartmentId)
            ? null
            : Guid.Parse(dto.RepairDepartmentId),
        RepairDepartmentName = dto.RepairDepartmentName,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        IsBanned = dto.IsBanned,
        IsAdminAccount = dto.IsAdminAccount,
    };

    private static DataError MapFunctionError(FunctionsException ex)
    {
        var detail = ex.Message;
        var code = (int)ex.StatusCode;
        if (code is 401 or 403)
            return DataError.Unauthorized(detail);

        if (code is 400 or 404)
            return DataError.Validation("DataError_Unknown", detail);

        if (code is 0 or 502 or 503 or 504)
            return DataError.Network(detail);

        return DataError.Unknown(detail);
    }

    private sealed class ListResponse
    {
        [JsonPropertyName("items")]
        public List<ProfileDto>? Items { get; set; }
    }

    private sealed class ItemResponse
    {
        [JsonPropertyName("item")]
        public ProfileDto? Item { get; set; }
    }

    private sealed class ProfileDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("locationId")]
        public string? LocationId { get; set; }

        [JsonPropertyName("locationName")]
        public string? LocationName { get; set; }

        [JsonPropertyName("locationScopeIds")]
        public List<string>? LocationScopeIds { get; set; }

        [JsonPropertyName("locationScopeLabels")]
        public List<string>? LocationScopeLabels { get; set; }

        [JsonPropertyName("repairDepartmentId")]
        public string? RepairDepartmentId { get; set; }

        [JsonPropertyName("repairDepartmentName")]
        public string? RepairDepartmentName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [JsonPropertyName("isBanned")]
        public bool IsBanned { get; set; }

        [JsonPropertyName("isAdminAccount")]
        public bool IsAdminAccount { get; set; }
    }
}
