using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace BAAZ.CMMS.Core.Services;

public sealed class SupabaseClientProvider(IGotrueSessionPersistence<Session> sessionPersistence)
    : ISupabaseClientProvider
{
    private readonly IGotrueSessionPersistence<Session> _sessionPersistence = sessionPersistence;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Supabase.Client? _client;
    private string _supabaseUrl = string.Empty;
    private string _publishableKey = string.Empty;

    public Supabase.Client Client => _client ?? throw new InvalidOperationException("Supabase client is not initialized.");

    public string SupabaseUrl => _supabaseUrl;

    public string PublishableKey => _publishableKey;

    public void ConfigureEndpoint(string url, string publishableKey)
    {
        _supabaseUrl = url.TrimEnd('/');
        _publishableKey = publishableKey;
    }

    public async Task InitializeAsync(string url, string publishableKey, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_supabaseUrl)
                && _supabaseUrl == url
                && _publishableKey == publishableKey
                && _client is not null)
            {
                return;
            }

            _supabaseUrl = url.TrimEnd('/');
            _publishableKey = publishableKey;

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false,
                SessionHandler = _sessionPersistence,
            };

            _client = new Supabase.Client(_supabaseUrl, _publishableKey, options);
            await _client.InitializeAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
