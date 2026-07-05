namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

/// <summary>Применяет сохранённые настройки TMS (Mock/Live) и пересоздаёт HTTP-клиент.</summary>
public static class TmsIntegrationSettingsSync
{
    public static void Apply(string? mode, string? baseUrl, string? secret, string? anonKey)
    {
        TmsIntegrationSettings.Mode = string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase)
            ? TmsIntegrationMode.Live
            : TmsIntegrationMode.Mock;
        TmsIntegrationSettings.TmsBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? TmsIntegrationSettings.TmsBaseUrl
            : baseUrl.Trim();
        TmsIntegrationSettings.TmsIntegrationSecret = secret?.Trim() ?? string.Empty;
        TmsIntegrationSettings.SupabaseAnonKey = anonKey?.Trim() ?? string.Empty;
        TmsIntegrationSettings.RefreshIssuanceClient();
    }

    public static bool IsLive => TmsIntegrationSettings.Mode == TmsIntegrationMode.Live;
}
