using System.Net.Http.Headers;
using BAAZ.CMMS.Core.Data;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

internal static class TmsIssuanceHttpClientHelper
{
    internal static HttpClient CreateClient(string baseUrl, string integrationSecret, string supabaseAnonKey, string? ifNoneMatch)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        if (!string.IsNullOrWhiteSpace(integrationSecret))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", integrationSecret);
        if (!string.IsNullOrWhiteSpace(supabaseAnonKey))
            client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        if (!string.IsNullOrWhiteSpace(ifNoneMatch))
            client.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(ifNoneMatch.Trim('"')));
        return client;
    }

    internal static async Task<DataError?> ReadErrorAsync(
        string apiId,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return TmsIssuanceHttpErrorMapper.FromHttpResponse(apiId, response.StatusCode, body);
    }

    internal static DataError ConnectionError(string apiId, Exception exception)
        => TmsIssuanceHttpErrorMapper.FromConnectionError(apiId, exception);
}
