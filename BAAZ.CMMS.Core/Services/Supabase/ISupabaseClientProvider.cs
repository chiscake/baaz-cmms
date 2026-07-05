namespace BAAZ.CMMS.Core.Services;

public interface ISupabaseClientProvider
{
    Supabase.Client Client { get; }

    string SupabaseUrl { get; }

    string PublishableKey { get; }

    void ConfigureEndpoint(string url, string publishableKey);

    Task InitializeAsync(string url, string publishableKey, CancellationToken cancellationToken = default);
}
