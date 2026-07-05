using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BAAZ.CMMS.App.Windows;

public partial class ConnectionErrorViewModel : ObservableObject
{
    private readonly SupabaseServerSettingsViewModel _serverSettings;
    private bool _isErrorView = true;

    public ConnectionErrorViewModel(
        ISupabaseClientProvider supabaseClientProvider,
        IConnectionService connectionService)
    {
        _serverSettings = new SupabaseServerSettingsViewModel(supabaseClientProvider, connectionService);
    }

    public SupabaseServerSettingsViewModel ServerSettings => _serverSettings;

    public bool IsErrorView
    {
        get => _isErrorView;
        private set
        {
            if (!SetProperty(ref _isErrorView, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsServerSettingsView));
            OnPropertyChanged(nameof(WindowTitleText));
            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(SubtitleText));
        }
    }

    public bool IsServerSettingsView => !IsErrorView;

    public string WindowTitleText => ResourceStrings.Get(
        IsErrorView ? "Connection_Window_Title" : "Settings_Supabase_Header");

    public string TitleText => ResourceStrings.Get(
        IsErrorView ? "Connection_Error_Title" : "Settings_Supabase_Header");

    public string SubtitleText => ResourceStrings.Get(
        IsErrorView ? "Connection_Error_Message" : "Auth_Server_Subtitle");

    public string RetryButtonText => ResourceStrings.Get("Connection_Retry");

    public string ChangeServerText => ResourceStrings.Get("Auth_ChangeServer");

    public string BackToErrorText => ResourceStrings.Get("Connection_Back");

    [RelayCommand]
    private void ShowServerSettings()
    {
        _serverSettings.ClearStatus();
        IsErrorView = false;
    }

    [RelayCommand]
    private void ShowErrorView()
    {
        _serverSettings.ClearStatus();
        IsErrorView = true;
    }
}
