using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

internal static class SupabaseRestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<IReadOnlyList<T>?> GetListAsync<T>(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return default;
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{clientProvider.SupabaseUrl}{relativePath}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken);
    }

    public static async Task<bool> PostAsync(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        object body,
        CancellationToken cancellationToken)
    {
        var (success, _) = await PostOrErrorAsync(clientProvider, relativePath, body, cancellationToken);
        return success;
    }

    public static async Task<(bool Success, string? ErrorBody)> PostOrErrorAsync(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        object body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return (false, "NO_SESSION");
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{clientProvider.SupabaseUrl}{relativePath}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, errorBody);
    }

    public static async Task<T?> PostReturningAsync<T>(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        object body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            Debug.WriteLine($"[SupabaseRestClient] PostReturningAsync {relativePath}: NO_SESSION");
            return default;
        }

        var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{clientProvider.SupabaseUrl}{relativePath}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Debug.WriteLine(
                $"[SupabaseRestClient] PostReturningAsync {relativePath} failed: " +
                $"{(int)response.StatusCode} {response.ReasonPhrase}\nBody: {jsonBody}\nResponse: {errorBody}");
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var rows = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken);
        if (rows is not { Count: > 0 })
        {
            Debug.WriteLine($"[SupabaseRestClient] PostReturningAsync {relativePath}: empty or unparseable response");
            return default;
        }

        return rows[0];
    }

    public static async Task<bool> PatchAsync(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        object body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return false;
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{clientProvider.SupabaseUrl}{relativePath}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public static async Task<(bool Success, string? ErrorBody)> PatchOrErrorAsync(
        ISupabaseClientProvider clientProvider,
        string relativePath,
        object body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return (false, "NO_SESSION");
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{clientProvider.SupabaseUrl}{relativePath}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, errorBody);
    }

    public static async Task<IReadOnlyList<T>?> CallRpcAsync<T>(
        ISupabaseClientProvider clientProvider,
        string functionName,
        object? body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return default;
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{clientProvider.SupabaseUrl}/rest/v1/rpc/{functionName}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(body ?? new { }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken);
    }

    /// <summary>Результат RPC со скалярным возвратом: значение или JSON-тело ошибки PostgREST.</summary>
    public readonly record struct RpcScalarResult<T>(T? Value, string? ErrorBody)
    {
        public bool IsSuccess => ErrorBody is null;
    }

    /// <summary>
    /// Вызов RPC, возвращающего скаляр (например <c>returns uuid</c>).
    /// PostgREST отдаёт JSON-значение напрямую, не массив.
    /// </summary>
    public static async Task<T?> CallRpcScalarAsync<T>(
        ISupabaseClientProvider clientProvider,
        string functionName,
        object? body,
        CancellationToken cancellationToken)
    {
        var result = await CallRpcScalarOrErrorAsync<T>(
            clientProvider, functionName, body, cancellationToken);
        return result.Value;
    }

    /// <summary>
    /// Как <see cref="CallRpcScalarAsync{T}"/>, но при ошибке HTTP возвращает тело ответа PostgREST
    /// (для сопоставления бизнес-кодов RAISE EXCEPTION).
    /// </summary>
    public static async Task<RpcScalarResult<T>> CallRpcScalarOrErrorAsync<T>(
        ISupabaseClientProvider clientProvider,
        string functionName,
        object? body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return new RpcScalarResult<T>(default, "NO_SESSION");
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{clientProvider.SupabaseUrl}/rest/v1/rpc/{functionName}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(body ?? new { }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new RpcScalarResult<T>(default, errorBody);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        return new RpcScalarResult<T>(value, null);
    }

    /// <summary>
    /// Вызов RPC-функции, возвращающей void (RPC жизненного цикла заявки и т.п.).
    /// Возвращает текст ошибки из тела ответа при неуспехе — обычно это message,
    /// заданный в RAISE EXCEPTION на стороне Postgres (например, REQUEST_NOT_NEW).
    /// </summary>
    public static async Task<(bool Success, string? Error)> CallRpcVoidAsync(
        ISupabaseClientProvider clientProvider,
        string functionName,
        object? body,
        CancellationToken cancellationToken)
    {
        var session = clientProvider.Client.Auth.CurrentSession;
        if (session?.AccessToken is null or { Length: 0 })
        {
            return (false, "NO_SESSION");
        }

        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{clientProvider.SupabaseUrl}/rest/v1/rpc/{functionName}");
        request.Headers.TryAddWithoutValidation("apikey", clientProvider.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(body ?? new { }, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, errorBody);
    }

    internal sealed class LocationEmbed
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
