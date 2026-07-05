using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Helpers.Settings;

namespace BAAZ.CMMS.App.Windows;

public partial class SupabaseServerSettingsViewModel : ObservableObject
{
    private static readonly TimeSpan ConnectionTestTimeout = TimeSpan.FromSeconds(8);

    private readonly ISupabaseClientProvider _supabaseClientProvider;
    private readonly IConnectionService _connectionService;

    private string _supabaseUrl = SettingsHelper.Current.SupabaseUrl;
    private string _supabaseAnonKey = SettingsHelper.Current.SupabaseAnonKey;
    private string? _statusMessage;

    public SupabaseServerSettingsViewModel(
        ISupabaseClientProvider supabaseClientProvider,
        IConnectionService connectionService)
    {
        _supabaseClientProvider = supabaseClientProvider;
        _connectionService = connectionService;
    }

    public string SupabaseUrlLabel => ResourceStrings.Get("Settings_Supabase_Url");

    public string SupabaseKeyLabel => ResourceStrings.Get("Settings_Supabase_Key");

    public string SupabaseTestButtonText => ResourceStrings.Get("Settings_Supabase_Test");

    public string SupabaseSaveButtonText => ResourceStrings.Get("Settings_Supabase_Save");

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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

    public void ClearStatus() => StatusMessage = null;

    [RelayCommand]
    private async Task TestSupabaseConnectionAsync()
    {
        var normalizedUrl = (SupabaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var normalizedKey = (SupabaseAnonKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            StatusMessage = ResourceStrings.Get("Settings_Supabase_Test_Validation");
            return;
        }

        var connected = await PingSupabaseAsync(normalizedUrl, normalizedKey);
        StatusMessage = ResourceStrings.Get(
            connected ? "Settings_Supabase_Test_Success" : "Settings_Supabase_Test_Failed");
    }

    [RelayCommand]
    private async Task SaveSupabaseSettingsAsync()
    {
        var normalizedUrl = (SupabaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var normalizedKey = (SupabaseAnonKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            StatusMessage = ResourceStrings.Get("Settings_Supabase_Test_Validation");
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
        StatusMessage = ResourceStrings.Get(
            connected ? "Settings_Supabase_Saved" : "Settings_Supabase_Test_Failed");
    }

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
}
