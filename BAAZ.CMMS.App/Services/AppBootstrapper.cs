using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Helpers.Settings;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Windows;
using BAAZ.CMMS.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Services;

public sealed class AppBootstrapper(
    ISupabaseClientProvider supabaseClientProvider,
    IConnectionService connectionService,
    IAuthService authService,
    IServiceProvider serviceProvider)
{
    private readonly ISupabaseClientProvider _supabaseClientProvider = supabaseClientProvider;
    private readonly IConnectionService _connectionService = connectionService;
    private readonly IAuthService _authService = authService;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private StartupLoadingWindow? _loadingWindow;

    public void CloseLoadingWindow()
    {
        _loadingWindow?.Close();
        _loadingWindow = null;
    }

    public async Task<BootstrapRunResult> RunAsync(DispatcherQueue dispatcherQueue)
    {
        void ShowLoading()
        {
            if (_loadingWindow is not null)
            {
                var previous = _loadingWindow;
                _loadingWindow = null;
                dispatcherQueue.TryEnqueue(() => previous.Close());
            }

            _loadingWindow = ActivatorUtilities.CreateInstance<StartupLoadingWindow>(_serviceProvider);
            _loadingWindow.ShowAndActivate();
        }

        void HandOffTo(Window nextWindow)
        {
            nextWindow.Activate();
            if (_loadingWindow is not null)
            {
                var loading = _loadingWindow;
                _loadingWindow = null;
                dispatcherQueue.TryEnqueue(() => loading.Close());
            }
        }

        ShowLoading();

        var supabaseUrl = SettingsHelper.Current.SupabaseUrl;
        var supabaseAnonKey = SettingsHelper.Current.SupabaseAnonKey;

        Debug.WriteLine($"[AppBootstrapper] ConfigureEndpoint url={supabaseUrl}");
        _supabaseClientProvider.ConfigureEndpoint(supabaseUrl, supabaseAnonKey);

        Debug.WriteLine("[AppBootstrapper] CheckAsync...");
        while (!await _connectionService.CheckAsync())
        {
            Debug.WriteLine("[AppBootstrapper] CheckAsync: not connected");
            var window = ActivatorUtilities.CreateInstance<ConnectionErrorWindow>(_serviceProvider);
            HandOffTo(window);
            var retry = await window.ShowAsync();
            if (!retry)
            {
                await UiDispatchHelper.RunAsync(dispatcherQueue, () => window.Close());
                return new BootstrapRunResult(false, null);
            }

            ShowLoading();
            await UiDispatchHelper.RunAsync(dispatcherQueue, () => window.Close());

            supabaseUrl = SettingsHelper.Current.SupabaseUrl;
            supabaseAnonKey = SettingsHelper.Current.SupabaseAnonKey;
            _supabaseClientProvider.ConfigureEndpoint(supabaseUrl, supabaseAnonKey);
        }
        Debug.WriteLine("[AppBootstrapper] CheckAsync: connected");

        Debug.WriteLine("[AppBootstrapper] InitializeAsync...");
        await _supabaseClientProvider.InitializeAsync(supabaseUrl, supabaseAnonKey);
        Debug.WriteLine("[AppBootstrapper] InitializeAsync done");

        Debug.WriteLine("[AppBootstrapper] TryRestoreSessionAsync...");
        var sessionRestored = await _authService.TryRestoreSessionAsync();
        Debug.WriteLine($"[AppBootstrapper] TryRestoreSessionAsync: {sessionRestored}");
        if (sessionRestored)
        {
            return new BootstrapRunResult(true, null);
        }

        var loginWindow = ActivatorUtilities.CreateInstance<LoginWindow>(_serviceProvider);
        HandOffTo(loginWindow);
        var signedIn = await loginWindow.ShowAsync();
        if (!signedIn)
        {
            return new BootstrapRunResult(false, null);
        }

        return new BootstrapRunResult(true, loginWindow);
    }
}
