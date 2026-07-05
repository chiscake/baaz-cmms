using System;
using System.Diagnostics;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Services;

using Helpers.Microsoft;
using Helpers.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace BAAZ.CMMS.App.Windows;

public sealed partial class LoginWindow : Window
{
#if DEBUG
    private const string DevTestPassword = "123";

    private static readonly DevQuickLoginAccount[] DevQuickLoginAccounts =
    [
        new("Администратор — Смирнов", "smirnov.v@baaz.by"),
        new("Диспетчер РМУ — Громов", "gromov.n@baaz.by"),
        new("Диспетчер РМУ — Кузнецов", "kuznetsov.p@baaz.by"),
        new("Диспетчер СГЭ — Литвиненко", "litvinenko.v@baaz.by"),
        new("Диспетчер КИПиА — Фёдоров", "fedorov.a@baaz.by"),
        new("Заявитель — Соколова", "sokolova.m@baaz.by"),
        new("Заявитель — Жуковская", "zhukovskaya.e@baaz.by"),
        new("Заявитель — Демидов", "demidov.i@baaz.by"),
    ];

    private sealed record DevQuickLoginAccount(string Label, string Email);
#endif

    private readonly IAuthService _authService;
    private readonly LoginViewModel _viewModel;
    private TaskCompletionSource<bool>? _completionSource;

    public LoginWindow(IAuthService authService, LoginViewModel viewModel)
    {
        _authService = authService;
        _viewModel = viewModel;
        InitializeComponent();
        rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme;
        rootGrid.DataContext = _viewModel;

        Title = ResourceStrings.Get("Auth_Window_Title");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        WindowHelper.TrackWindow(this);
        Closed += OnClosed;
    }

    public LoginWindow() : this(
        App.Services.GetRequiredService<IAuthService>(),
        App.Services.GetRequiredService<LoginViewModel>())
    {
    }

    public Task<bool> ShowAsync()
    {
        _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Activate();
        return _completionSource.Task;
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, rootGrid.ActualTheme);
#if DEBUG
        devQuickLoginPanel.Visibility = Visibility.Visible;
        devQuickLoginComboBox.ItemsSource = DevQuickLoginAccounts;
        WindowSizeDefaults.ApplyFixedSize(this, WindowSizeDefaults.Login.Width, WindowSizeDefaults.Login.DebugHeight);
#else
        WindowSizeDefaults.ApplyLoginWindowSize(this);
#endif
        emailTextBox.Focus(FocusState.Programmatic);
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[LoginWindow:{GetHashCode():X8}] SignInButton_Click");
        await TrySignInAsync();
    }

    private async void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != global::Windows.System.VirtualKey.Enter)
        {
            return;
        }

        await TrySignInAsync();
    }

#if DEBUG
    private async void DevQuickLogin_Click(object sender, RoutedEventArgs e)
    {
        if (devQuickLoginComboBox.SelectedItem is not DevQuickLoginAccount account)
        {
            _viewModel.SetSignInError("Выберите учётную запись для быстрого входа.");
            return;
        }

        emailTextBox.Text = account.Email;
        passwordBox.Password = DevTestPassword;
        await TrySignInAsync(account.Email, DevTestPassword);
    }
#else
    private void DevQuickLogin_Click(object sender, RoutedEventArgs e)
    {
    }
#endif

    private async Task TrySignInAsync(string? emailOverride = null, string? passwordOverride = null)
    {
        var email = emailOverride ?? emailTextBox.Text?.Trim() ?? string.Empty;
        var password = passwordOverride ?? passwordBox.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Debug.WriteLine($"[LoginWindow:{GetHashCode():X8}] TrySignInAsync: empty credentials");
            _viewModel.SetSignInError(ResourceStrings.Get("Auth_Error_Required"));
            return;
        }

        _viewModel.SetSignInError(null);

        Debug.WriteLine($"[LoginWindow:{GetHashCode():X8}] SignInAsync at {DateTime.UtcNow:HH:mm:ss.fff}Z");
        var result = await _authService.SignInAsync(email, password);
        Debug.WriteLine(
            $"[LoginWindow:{GetHashCode():X8}] SignInAsync done, success={result.Success}, error={result.ErrorMessage ?? "(none)"}");

        if (result.Success)
        {
            ScheduleSignInCompletion();
            return;
        }

        _viewModel.SetSignInError(ResourceStrings.Get(result.ErrorMessage ?? "Auth_InvalidCredentials"));
    }

    // Отложенный TrySetResult — не возобновлять LaunchAsync внутри обработчика кнопки.
    private void ScheduleSignInCompletion()
    {
        if (!DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                Debug.WriteLine($"[LoginWindow:{GetHashCode():X8}] TrySetResult(true)");
                _completionSource?.TrySetResult(true);
            }))
        {
            Debug.WriteLine($"[LoginWindow:{GetHashCode():X8}] TryEnqueue failed, sync TrySetResult");
            _completionSource?.TrySetResult(true);
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_completionSource is { Task.IsCompleted: false })
        {
            _completionSource.TrySetResult(false);
        }
    }
}
