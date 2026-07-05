using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

using Helpers.Microsoft;
using Helpers.Settings;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services;

using Microsoft.UI.Xaml;

using WinUI.UtilsLibrary.ViewModels;
using Helpers;

namespace BAAZ.CMMS.App.Pages.Settings;

public partial class SettingsViewModel : PageViewModelBase
{
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
    }

    public string SectionAppearance => ResourceStrings.Get("Settings_Section_Appearance");

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

    public string SupabaseSaveButtonText => ResourceStrings.Get("Settings_Supabase_Save");

    public IReadOnlyList<string> ThemeOptionLabels { get; } =
    [
        ResourceStrings.Get("Settings_Theme_Light"),
        ResourceStrings.Get("Settings_Theme_Dark"),
        ResourceStrings.Get("Settings_Theme_Default"),
    ];

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
    private async Task SaveSupabaseSettingsAsync()
    {
        var normalizedUrl = (SupabaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var normalizedKey = (SupabaseAnonKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            return;
        }

        SettingsHelper.Current.SupabaseUrl = normalizedUrl;
        SettingsHelper.Current.SupabaseAnonKey = normalizedKey;
        _supabaseUrl = normalizedUrl;
        _supabaseAnonKey = normalizedKey;
        OnPropertyChanged(nameof(SupabaseUrl));
        OnPropertyChanged(nameof(SupabaseAnonKey));

        await _supabaseClientProvider.InitializeAsync(normalizedUrl, normalizedKey);
        await _connectionService.CheckAsync();
    }

    private void ApplyTheme(string themeTag)
    {
        var theme = EnumHelper.GetEnum<ElementTheme>(themeTag);
        SettingsHelper.Current.SelectedAppTheme = theme;
        AppThemeHelper.Apply(theme);
    }
}
