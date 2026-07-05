using Microsoft.UI.Xaml;
using BAAZ.CMMS.Core.Configuration;

namespace Helpers.Settings;

public sealed partial class SettingsHelper
{
    public ElementTheme SelectedAppTheme
    {
        get => GetOrCreateDefault(ElementTheme.Default);
        set => Set(value);
    }

    public bool IsLeftMode
    {
        get => GetOrCreateDefault(true);
        set => Set(value);
    }

    /// <summary>App language: "system", "ru-RU", or "en-US".</summary>
    public string AppLanguage
    {
        get => GetOrCreateDefault("system") ?? "system";
        set => Set(value);
    }

    public string SupabaseUrl
    {
        get => GetOrCreateDefault(SupabaseDefaults.Url) ?? SupabaseDefaults.Url;
        set => Set(value);
    }

    public string SupabaseAnonKey
    {
        get => GetOrCreateDefault(SupabaseDefaults.PublishableKey) ?? SupabaseDefaults.PublishableKey;
        set => Set(value);
    }

    /// <summary>
    /// JSON: видимость колонок CrudDataGrid по ключам страниц
    /// (например { "Personnel": { "FullName": true } }).
    /// </summary>
    public string CrudGridColumnVisibilityJson
    {
        get => GetOrCreateDefault("{}") ?? "{}";
        set => Set(value);
    }

    /// <summary>JSON: настройки страницы «График ТО» (вид, zoom, split, collapsed nodes).</summary>
    public string MaintenanceSchedulePrefsJson
    {
        get => GetOrCreateDefault("{}") ?? "{}";
        set => Set(value);
    }

    /// <summary>Сохранённое состояние окна MainWindow (позиция/размер/развёрнутость).</summary>
    public int MainWindowX
    {
        get => GetOrCreateDefault(int.MinValue);
        set => Set(value);
    }

    public int MainWindowY
    {
        get => GetOrCreateDefault(int.MinValue);
        set => Set(value);
    }

    public int MainWindowWidth
    {
        get => GetOrCreateDefault(0);
        set => Set(value);
    }

    public int MainWindowHeight
    {
        get => GetOrCreateDefault(0);
        set => Set(value);
    }

    public bool MainWindowIsMaximized
    {
        get => GetOrCreateDefault(false);
        set => Set(value);
    }

    /// <summary>Последний каталог сохранения документов (заявки ТМЦ и др.).</summary>
    public string LastDocumentSaveDirectory
    {
        get => GetOrCreateDefault(string.Empty) ?? string.Empty;
        set => Set(value);
    }

    /// <summary>Mock | Live — интеграция с Tool Tracker.</summary>
    public string TmsIntegrationMode
    {
        get => GetOrCreateDefault("Mock") ?? "Mock";
        set => Set(value);
    }

    public string TmsBaseUrl
    {
        get => GetOrCreateDefault("http://127.0.0.1:8000") ?? "http://127.0.0.1:8000";
        set => Set(value);
    }

    public string TmsIntegrationSecret
    {
        get => GetOrCreateDefault(string.Empty) ?? string.Empty;
        set => Set(value);
    }
}
