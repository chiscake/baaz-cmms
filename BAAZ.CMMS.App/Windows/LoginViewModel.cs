using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BAAZ.CMMS.App.Windows;

public partial class LoginViewModel : ObservableObject
{
    private readonly SupabaseServerSettingsViewModel _serverSettings;
    private bool _isSignInView = true;
    private string? _signInErrorMessage;

    public LoginViewModel(
        ISupabaseClientProvider supabaseClientProvider,
        IConnectionService connectionService)
    {
        _serverSettings = new SupabaseServerSettingsViewModel(supabaseClientProvider, connectionService);
    }

    public SupabaseServerSettingsViewModel ServerSettings => _serverSettings;

    public bool IsSignInView
    {
        get => _isSignInView;
        private set
        {
            if (!SetProperty(ref _isSignInView, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsServerSettingsView));
            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(SubtitleText));
        }
    }

    public bool IsServerSettingsView => !IsSignInView;

    public string TitleText => ResourceStrings.Get(IsSignInView ? "Auth_Window_Title" : "Settings_Supabase_Header");

    public string SubtitleText => ResourceStrings.Get(IsSignInView ? "Auth_Subtitle" : "Auth_Server_Subtitle");

    public string EmailLabel => ResourceStrings.Get("Auth_Email");

    public string EmailPlaceholder => ResourceStrings.Get("Auth_Email_Placeholder");

    public string PasswordLabel => ResourceStrings.Get("Auth_Password");

    public string SignInButtonText => ResourceStrings.Get("Auth_SignIn");

    public string ChangeServerText => ResourceStrings.Get("Auth_ChangeServer");

    public string BackToSignInText => ResourceStrings.Get("Auth_BackToSignIn");

    public string? SignInErrorMessage
    {
        get => _signInErrorMessage;
        private set => SetProperty(ref _signInErrorMessage, value);
    }

    public void SetSignInError(string? message) => SignInErrorMessage = message;

    [RelayCommand]
    private void ShowServerSettings()
    {
        _serverSettings.ClearStatus();
        IsSignInView = false;
    }

    [RelayCommand]
    private void ShowSignIn()
    {
        _serverSettings.ClearStatus();
        IsSignInView = true;
    }
}
