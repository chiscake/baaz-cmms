using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

using Helpers.Microsoft;
using Helpers.Settings;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Integrations.ToolTracker;
using BAAZ.CMMS.Core.Services;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;
using Helpers;

namespace BAAZ.CMMS.App.Pages.Settings;

public partial class SettingsViewModel : PageViewModelBase
{
    private static readonly TimeSpan BannerAutoDismiss = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ConnectionTestTimeout = TimeSpan.FromSeconds(8);

    private readonly ISupabaseClientProvider _supabaseClientProvider;
    private readonly IConnectionService _connectionService;

    public override string PageTitle => ResourceStrings.Get("Settings_Title");
    private static int ThemeToIndex(ElementTheme theme) => theme switch { ElementTheme.Light => 0, ElementTheme.Dark => 1, _ => 2 };
    private static string IndexToThemeTag(int index) => index switch { 0 => "Light", 1 => "Dark", _ => "Default" };

    private int _selectedThemeIndex = ThemeToIndex(ThemeHelper.RootTheme);
    private int _selectedNavigationLocationIndex = SettingsHelper.Current.IsLeftMode ? 0 : 1;
    private int _selectedLanguageIndex = LanguageHelper.LanguageToIndex(SettingsHelper.Current.AppLanguage);
    private string _supabaseUrl = SettingsHelper.Current.SupabaseUrl;
    private string _supabaseAnonKey = SettingsHelper.Current.SupabaseAnonKey;

    public SettingsViewModel(
        ISupabaseClientProvider supabaseClientProvider,
        IConnectionService connectionService)
    {
        _supabaseClientProvider = supabaseClientProvider;
        _connectionService = connectionService;
        _selectedTmsModeIndex = string.Equals(SettingsHelper.Current.TmsIntegrationMode, "Live", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    public IReadOnlyList<string> ThemeOptionLabels { get; } =
    [
        ResourceStrings.Get("Settings_Theme_Light"),
        ResourceStrings.Get("Settings_Theme_Dark"),
        ResourceStrings.Get("Settings_Theme_Default"),
    ];

    public string ThemeHeader => ResourceStrings.Get("Settings_Theme_Header");

    public string ThemeDescription => ResourceStrings.Get("Settings_Theme_Description");

    public string NavigationHeader => ResourceStrings.Get("Settings_Navigation_Header");

    public string LanguageHeader => ResourceStrings.Get("Settings_Language_Header");

    public string LanguageDescription => ResourceStrings.Get("Settings_Language_Description");

    public string SectionData => ResourceStrings.Get("Settings_Section_Data");

    public string ResetSettingsHeader => ResourceStrings.Get("Settings_Reset_Header");

    public string ResetSettingsDescription => ResourceStrings.Get("Settings_Reset_Description");

    public string ResetSettingsButtonText => ResourceStrings.Get("Settings_Reset_Button");

    public string SupabaseSettingsHeader => ResourceStrings.Get("Settings_Supabase_Header");

    public string SupabaseSettingsDescription => ResourceStrings.Get("Settings_Supabase_Description");

    public string SupabaseUrlLabel => ResourceStrings.Get("Settings_Supabase_Url");

    public string SupabaseKeyLabel => ResourceStrings.Get("Settings_Supabase_Key");

    public string SupabaseTestButtonText => ResourceStrings.Get("Settings_Supabase_Test");

    public string SupabaseSaveButtonText => ResourceStrings.Get("Settings_Supabase_Save");

    public string SectionTmsIntegration => ResourceStrings.Get("Settings_Tms_Section");

    public string TmsIntegrationHeader => ResourceStrings.Get("Settings_Tms_Header");

    public string TmsIntegrationDescription => ResourceStrings.Get("Settings_Tms_Description");

    public string TmsModeLabel => ResourceStrings.Get("Settings_Tms_Mode");

    public string TmsBaseUrlLabel => ResourceStrings.Get("Settings_Tms_BaseUrl");

    public string TmsSecretLabel => ResourceStrings.Get("Settings_Tms_Secret");

    public string TmsTestButtonText => ResourceStrings.Get("Settings_Tms_Test");

    public string TmsSaveButtonText => ResourceStrings.Get("Settings_Tms_Save");

    public IReadOnlyList<string> TmsModeOptions { get; } = ["Mock", "Live"];

    private int _selectedTmsModeIndex;
    private string _tmsBaseUrl = SettingsHelper.Current.TmsBaseUrl;
    private string _tmsIntegrationSecret = SettingsHelper.Current.TmsIntegrationSecret;

    public int SelectedTmsModeIndex
    {
        get => _selectedTmsModeIndex;
        set => SetProperty(ref _selectedTmsModeIndex, value);
    }

    public string TmsBaseUrl
    {
        get => _tmsBaseUrl;
        set => SetProperty(ref _tmsBaseUrl, value);
    }

    public void SetTmsIntegrationSecret(string value) => _tmsIntegrationSecret = value ?? string.Empty;

    public string SectionAppearance => ResourceStrings.Get("Settings_Section_Appearance");

    public IReadOnlyList<string> NavigationOptionLabels { get; } =
    [
        ResourceStrings.Get("Settings_Navigation_Left"),
        ResourceStrings.Get("Settings_Navigation_Top"),
    ];

    public IReadOnlyList<string> LanguageOptionLabels { get; } =
    [
        ResourceStrings.Get("Settings_Language_System"),
        ResourceStrings.Get("Settings_Language_Russian"),
        ResourceStrings.Get("Settings_Language_English"),
    ];

    /// <summary>Индекс выбранной темы (0 — светлая, 1 — тёмная, 2 — как в системе). Привязка: ComboBox SelectedIndex.</summary>
    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (value < 0 || value > 2 || !SetProperty(ref _selectedThemeIndex, value))
            {
                return;
            }

            ApplyTheme(IndexToThemeTag(value));
        }
    }

    /// <summary>Индекс расположения навигации (0 — слева, 1 — сверху). Привязка: ComboBox SelectedIndex.</summary>
    public int SelectedNavigationLocationIndex
    {
        get => _selectedNavigationLocationIndex;
        set
        {
            if (value < 0 || value > 1 || value == _selectedNavigationLocationIndex)
            {
                return;
            }

            SettingsHelper.Current.IsLeftMode = value == 0;
            _selectedNavigationLocationIndex = value;
            OnPropertyChanged(nameof(SelectedNavigationLocationIndex));
        }
    }

    /// <summary>Индекс языка (0 — системный, 1 — ru-RU, 2 — en-US). Смена перезапускает приложение.</summary>
    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set
        {
            if (value < 0 || value > 2)
            {
                return;
            }

            var newTag = LanguageHelper.IndexToLanguageTag(value);
            if (newTag == SettingsHelper.Current.AppLanguage)
            {
                if (value != _selectedLanguageIndex)
                {
                    _selectedLanguageIndex = value;
                    OnPropertyChanged(nameof(SelectedLanguageIndex));
                }

                return;
            }

            SettingsHelper.Current.AppLanguage = newTag;
            _selectedLanguageIndex = value;
            OnPropertyChanged(nameof(SelectedLanguageIndex));
            AppRestartHelper.RestartApp();
        }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        SettingsHelper.Current.ClearAllSettings();
        AppRestartHelper.RestartApp();
    }

    public string SupabaseUrl
    {
        get => _supabaseUrl;
        set => SetProperty(ref _supabaseUrl, value);
    }

    public string SupabaseAnonKey
    {
        get => _supabaseAnonKey;
        set => SetProperty(ref _supabaseAnonKey, value);
    }

    [RelayCommand]
    private async Task TestSupabaseConnectionAsync()
    {
        var normalizedUrl = (SupabaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var normalizedKey = (SupabaseAnonKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            ReportBanner(ResourceStrings.Get("Settings_Supabase_Test_Validation"), InfoBarSeverity.Warning);
            return;
        }

        var connected = await PingSupabaseAsync(normalizedUrl, normalizedKey);
        ReportBanner(
            ResourceStrings.Get(connected ? "Settings_Supabase_Test_Success" : "Settings_Supabase_Test_Failed"),
            connected ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    [RelayCommand]
    private async Task SaveSupabaseSettingsAsync()
    {
        var normalizedUrl = (SupabaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var normalizedKey = (SupabaseAnonKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            ReportBanner(ResourceStrings.Get("Settings_Supabase_Test_Validation"), InfoBarSeverity.Warning);
            return;
        }

        SettingsHelper.Current.SupabaseUrl = normalizedUrl;
        SettingsHelper.Current.SupabaseAnonKey = normalizedKey;
        _supabaseUrl = normalizedUrl;
        _supabaseAnonKey = normalizedKey;
        OnPropertyChanged(nameof(SupabaseUrl));
        OnPropertyChanged(nameof(SupabaseAnonKey));

        await _supabaseClientProvider.InitializeAsync(normalizedUrl, normalizedKey);
        var connected = await _connectionService.CheckAsync();
        ReportBanner(
            ResourceStrings.Get(connected ? "Settings_Supabase_Saved" : "Settings_Supabase_Test_Failed"),
            connected ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    [RelayCommand]
    private async Task TestTmsConnectionAsync()
    {
        if (SelectedTmsModeIndex == 0)
        {
            ReportBanner(ResourceStrings.Get("Settings_Tms_Test_Mock_Success"), InfoBarSeverity.Success);
            return;
        }

        var normalizedUrl = (TmsBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            ReportBanner(ResourceStrings.Get("Settings_Tms_Test_Validation"), InfoBarSeverity.Warning);
            return;
        }

        var connected = await PingTmsAsync(normalizedUrl);
        ReportBanner(
            ResourceStrings.Get(connected ? "Settings_Tms_Test_Success" : "Settings_Tms_Test_Failed"),
            connected ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    [RelayCommand]
    private Task SaveTmsIntegrationSettingsAsync()
    {
        SettingsHelper.Current.TmsIntegrationMode = SelectedTmsModeIndex == 1 ? "Live" : "Mock";
        SettingsHelper.Current.TmsBaseUrl = (TmsBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        SettingsHelper.Current.TmsIntegrationSecret = _tmsIntegrationSecret.Trim();
        TmsIntegrationSettingsSync.Apply(
            SettingsHelper.Current.TmsIntegrationMode,
            SettingsHelper.Current.TmsBaseUrl,
            SettingsHelper.Current.TmsIntegrationSecret,
            SettingsHelper.Current.SupabaseAnonKey);
        ReportBanner(ResourceStrings.Get("Settings_Tms_Saved"), InfoBarSeverity.Success);
        return Task.CompletedTask;
    }

    private void ReportBanner(string message, InfoBarSeverity severity)
        => InfoBanner.Report(message, severity, BannerAutoDismiss);

    private static async Task<bool> PingSupabaseAsync(string url, string publishableKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = ConnectionTestTimeout };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{url}/auth/v1/health");
            request.Headers.TryAddWithoutValidation("apikey", publishableKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> PingTmsAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = ConnectionTestTimeout };
            using var response = await httpClient.GetAsync($"{baseUrl}/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void ApplyTheme(string themeTag)
    {
        var theme = EnumHelper.GetEnum<ElementTheme>(themeTag);
        SettingsHelper.Current.SelectedAppTheme = theme;
        AppThemeHelper.Apply(theme);
    }
}
