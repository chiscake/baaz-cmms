using BAAZ.CMMS.Core.Contracts.Integrations;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

/// <summary>Фабрика Mock/Live клиентов TMS (контуры Б и REP-EVT-1).</summary>
public static class TmsIntegrationSettings
{
    public static TmsIntegrationMode Mode { get; set; } = TmsIntegrationMode.Mock;

    public static string TmsBaseUrl { get; set; } = "http://127.0.0.1:8000";

    public static string TmsIntegrationSecret { get; set; } = string.Empty;

    public static string SupabaseAnonKey { get; set; } = string.Empty;

    public static ITmsIssuanceOutboundSender CreateOutboundSender()
        => Mode == TmsIntegrationMode.Live
            ? new HttpTmsIssuanceOutboundSender(TmsBaseUrl, TmsIntegrationSecret, SupabaseAnonKey)
            : new MockTmsIssuanceOutboundSender();

    public static ITmsIssuanceClient CreateIssuanceClient(ITmsIssuanceOutboundSender outboundSender)
        => Mode == TmsIntegrationMode.Live
            ? new HttpTmsIssuanceClient(TmsBaseUrl, TmsIntegrationSecret, SupabaseAnonKey, outboundSender)
            : new MockTmsIssuanceClient(outboundSender);

    public static IToolTrackerIntegration CreateToolTrackerIntegration()
        => Mode == TmsIntegrationMode.Live
            ? new HttpToolTrackerIntegration(TmsBaseUrl, TmsIntegrationSecret, SupabaseAnonKey)
            : new MockToolTrackerIntegration();

    private static TmsIssuanceClientProvider? _issuanceClientProvider;

    public static void RegisterIssuanceClientProvider(TmsIssuanceClientProvider provider)
        => _issuanceClientProvider = provider;

    /// <summary>Пересоздаёт Mock/Live-клиент после сохранения настроек TMS в UI.</summary>
    public static void RefreshIssuanceClient()
        => _issuanceClientProvider?.Refresh();
}
