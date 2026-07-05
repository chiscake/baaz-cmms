using System.Net.Http.Headers;

namespace BAAZ.CMMS.Core.Services;

public sealed class ConnectionService(ISupabaseClientProvider clientProvider) : IConnectionService
{
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(8);

    private readonly ISupabaseClientProvider _clientProvider = clientProvider;

    public event EventHandler<bool>? ConnectionStateChanged;

    public bool IsConnected { get; private set; }

    public async Task<bool> CheckAsync(CancellationToken cancellationToken = default)
    {
        var connected = await PingAsync(cancellationToken);
        if (connected != IsConnected)
        {
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(this, connected);
        }

        return connected;
    }

    private async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = PingTimeout };
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_clientProvider.SupabaseUrl}/auth/v1/health");

            request.Headers.TryAddWithoutValidation("apikey", _clientProvider.PublishableKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
