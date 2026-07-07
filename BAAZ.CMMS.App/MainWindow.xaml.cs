using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Helpers.Microsoft;
using Helpers.Settings;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Services.Notifications;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Realtime;
using BAAZ.CMMS.Core.Services;
using Microsoft.UI.Dispatching;
using WinUI.UtilsLibrary.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private INavigationService? _navigationService;
    private IAuthService? _authService;
    private IConnectionService? _connectionService;
    private ISupabaseClientProvider? _supabaseClientProvider;
    private IRealtimeNotificationService? _realtimeService;
    private IShellNotificationPresenter? _shellPresenter;
    private INavBadgeService? _navBadgeService;
    private DispatcherQueueTimer? _connectionTimer;
    private DispatcherQueue? _uiDispatcherQueue;
    private UserRole _currentRole = UserRole.Requester;
    private string _homePageKey = NavHomePageKeys.Requester;
    private string? _lastInvokedMenuItemId;
    private bool _shellInitialized;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NavigationView NavigationView => navView;

    public Frame NavigationFrame => navFrame;

    public bool CanNavigateBack => _navigationService?.CanGoBack ?? false;

    public string WindowTitle => ResourceStrings.Get("App_Title");

    public string TitleBarTitle => ResourceStrings.Get("App_TitleBar");

    public string SignOutText => ResourceStrings.Get("Auth_SignOut");

    public string ThemeSwitcherTooltip => ResourceStrings.Get("Settings_Theme_Header");

    public string ThemeSwitcherGlyph => GetThemeGlyph(SettingsHelper.Current.SelectedAppTheme);

    public string ProfileName
    {
        get
        {
            var profile = _authService?.CurrentProfile;
            if (profile is null)
                return ResourceStrings.Get("TitleBar_Profile_Empty");

            var name = profile.FullName ?? string.Empty;
            var suffixFormat = ResourceStrings.Get("TitleBar_Profile_Dispatcher_Format");

            if (profile.Role == UserRole.Dispatcher
                && !string.IsNullOrEmpty(profile.RepairDepartmentName))
            {
                return string.Format(suffixFormat, name, profile.RepairDepartmentName);
            }

            if (profile.Role == UserRole.Admin)
            {
                return string.Format(suffixFormat, name, ResourceStrings.Get("Users_Role_Admin"));
            }

            if (profile.Role == UserRole.Requester)
            {
                return string.Format(suffixFormat, name, ResourceStrings.Get("Users_Role_Requester"));
            }

            return string.IsNullOrEmpty(name)
                ? ResourceStrings.Get("TitleBar_Profile_Empty")
                : name;
        }
    }

    public bool IsServerConnected => _connectionService?.IsConnected == true;

    public string ConnectionMarkerColorKey => IsServerConnected
        ? nameof(StatusBadgeColorToken.Green)
        : nameof(StatusBadgeColorToken.Red);

    public string ConnectionMarkerTooltip => IsServerConnected
        ? ResourceStrings.Get("Status_Connected_Tooltip")
        : ResourceStrings.Get("Status_Disconnected_Tooltip");

    public string ConnectionOfflineLabel => ResourceStrings.Get("Status_Offline");

    public bool ShowConnectionOfflineLabel => !IsServerConnected;

    public string ServerAddressText =>
        !string.IsNullOrWhiteSpace(_supabaseClientProvider?.SupabaseUrl)
            ? _supabaseClientProvider.SupabaseUrl
            : SettingsHelper.Current.SupabaseUrl;

    public MainWindow()
    {
        InitializeComponent();
        // ThemeHelper.RootTheme применяет тему только к уже отслеживаемым окнам
        // (WindowHelper.ActiveWindows) — на момент App.OnLaunched окон ещё нет,
        // поэтому применяем сохранённую тему напрямую к корневому элементу окна.
        rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme;
        _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        rootGrid.DataContext = this;
        Title = ResourceStrings.Get("App_Title");

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        Closed += OnWindowClosed;
        BAAZ.CMMS.App.Helpers.MainWindowStateHelper.Attach(this);
    }

    public void Initialize(IServiceProvider services)
    {
        _navigationService = services.GetRequiredService<INavigationService>();
        _authService = services.GetRequiredService<IAuthService>();
        _connectionService = services.GetRequiredService<IConnectionService>();
        _supabaseClientProvider = services.GetRequiredService<ISupabaseClientProvider>();
        _realtimeService = services.GetRequiredService<IRealtimeNotificationService>();
        _shellPresenter = services.GetRequiredService<IShellNotificationPresenter>();
        _navBadgeService = services.GetRequiredService<INavBadgeService>();

        _ = _realtimeService.StartAsync();
        _shellPresenter.Start();
        _navBadgeService.Attach(navView);
        _navBadgeService.BadgesChanged += (_, _) => _navBadgeService.ApplyToNavigationView();

        if (AppNotificationActivation.TryConsume(out var pageKey, out var requestId))
        {
            MainWindowActivationHelper.BringToForeground(this);
            _shellPresenter.NavigateFromToast(pageKey, requestId);
        }
        _navigationService.CanGoBackChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanNavigateBack)));
        _navigationService.Navigated += OnNavigationServiceNavigated;
        SettingsHelper.Current.PropertyChanged += OnSettingsPropertyChanged;
        _authService.ProfileChanged += OnProfileChanged;
        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;

        _currentRole = _authService.CurrentProfile?.Role ?? UserRole.Requester;
        _homePageKey = ResolveHomePageKey(_currentRole);
        NavMenuBuilder.ApplyRoleMenu(navView, _currentRole);
        NotifyConnectionStatusChanged();
        NotifyServerAddressChanged();
    }

    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
        Debug.WriteLine("[MainWindow] Closed");
        _connectionTimer?.Stop();
        _connectionTimer = null;
    }

    private void EnsureShellInitialized()
    {
        if (_shellInitialized || _navigationService is null)
        {
            return;
        }

        _shellInitialized = true;
        _navigationService.NavigateTo(_homePageKey);

        _connectionTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _connectionTimer.Interval = TimeSpan.FromSeconds(5);
        _connectionTimer.Tick += async (_, _) => await UpdateConnectionStatusAsync();
        _connectionTimer.Start();
        _ = UpdateConnectionStatusAsync();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsHelper.IsLeftMode))
        {
            var isLeftMode = SettingsHelper.Current.IsLeftMode;
            NavigationOrientationHelper.ApplyToNavigationView(navView, isLeftMode);
            titleBar.IsPaneToggleButtonVisible = isLeftMode;
        }
        else if (e.PropertyName is nameof(SettingsHelper.SupabaseUrl) or nameof(SettingsHelper.SupabaseAnonKey))
        {
            NotifyServerAddressChanged();
        }
        else if (e.PropertyName == nameof(SettingsHelper.SelectedAppTheme))
        {
            SyncThemeMenuSelection();
        }
    }

    private void OnNavigationServiceNavigated(object? sender, NavigationChangedEventArgs e)
    {
        var pageKey = e.PageKey;
        var parameter = e.Parameter;
        navView.DispatcherQueue.TryEnqueue(() => UpdateNavigationSelection(pageKey, parameter));
    }

    private void UpdateNavigationSelection(string? pageKey, object? parameter = null)
    {
        try
        {
            if (pageKey == "Settings")
            {
                navView.SelectedItem = navView.SettingsItem;
                return;
            }

            var selectionId = ResolveSelectionId(pageKey, parameter);
            if (selectionId is null || !TrySelectMenuItem(selectionId))
            {
                navView.SelectedItem = null;
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // WinUI может падать при смене SelectedItem во время layout.
        }
    }

    private string? ResolveSelectionId(string? pageKey, object? parameter)
    {
        if (_lastInvokedMenuItemId is not null
            && NavMenuRegistry.TryResolveLeaf(_lastInvokedMenuItemId, out var lastLeaf)
            && NavRouter.MatchesNavigation(lastLeaf, pageKey, parameter, _homePageKey))
        {
            return _lastInvokedMenuItemId;
        }

        return NavMenuRegistry.FindFirstLeafId(_currentRole, pageKey, parameter, _homePageKey);
    }

    private bool TrySelectMenuItem(string selectionId)
    {
        var groupPath = NavMenuRegistry.GetGroupPathToLeaf(_currentRole, selectionId);
        if (navView.IsPaneOpen)
        {
            NavMenuSelectionHelper.ExpandGroups(navView.MenuItems, groupPath);
        }

        var item = NavMenuSelectionHelper.FindItemById(navView.MenuItems, selectionId);
        if (item is null)
        {
            return false;
        }

        navView.SelectedItem = item;
        return true;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (_navigationService?.CanGoBack == true)
        {
            _navigationService.GoBack();
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (_navigationService is null)
        {
            return;
        }

        var navigated = false;
        if (args.IsSettingsInvoked)
        {
            _lastInvokedMenuItemId = null;
            _navigationService.NavigateTo("Settings");
            navigated = true;
        }
        else if (args.InvokedItemContainer is NavigationViewItem { Tag: string menuItemId }
                 && NavMenuRegistry.TryResolveLeaf(menuItemId, out var leafNode))
        {
            _lastInvokedMenuItemId = menuItemId;
            NavRouter.Navigate(_navigationService, leafNode, _homePageKey);
            navigated = true;
        }

        // Группы (SelectsOnInvoked=false) тоже вызывают ItemInvoked — не сворачиваем панель.
        // Свернуть группы до закрытия панели: иначе NavigationView в compact-режиме
        // показывает flyout с дочерними пунктами (IsExpanded=true + IsPaneOpen=false).
        if (navigated && navView.IsPaneOpen)
        {
            NavMenuSelectionHelper.CollapseAllGroups(navView.MenuItems);
            navView.IsPaneOpen = false;
        }
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(
            $"[MainWindow:{GetHashCode():X8}] Loaded, role={_currentRole}, home={_homePageKey}, shell={_shellInitialized}");
        var isLeftMode = SettingsHelper.Current.IsLeftMode;
        NavigationOrientationHelper.ApplyToNavigationView(navView, isLeftMode);
        titleBar.IsPaneToggleButtonVisible = isLeftMode;
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, rootGrid.ActualTheme);
        BAAZ.CMMS.App.Helpers.WindowSizeDefaults.ApplyMainWindowMinSize(this);
        InitializeThemeSwitcher();
#if DEBUG
        debugStatusPanel.Visibility = Visibility.Visible;
#endif
        EnsureShellInitialized();
        Debug.WriteLine($"[MainWindow:{GetHashCode():X8}] Shell ready");
    }

    private void InitializeThemeSwitcher()
    {
        themeLightItem.Text = ResourceStrings.Get("Settings_Theme_Light");
        themeDarkItem.Text = ResourceStrings.Get("Settings_Theme_Dark");
        themeDefaultItem.Text = ResourceStrings.Get("Settings_Theme_Default");
        SyncThemeMenuSelection();
    }

    private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioMenuFlyoutItem item || item.Tag is not string tag)
        {
            return;
        }

        var theme = EnumHelper.GetEnum<ElementTheme>(tag);
        if (SettingsHelper.Current.SelectedAppTheme == theme)
        {
            return;
        }

        SettingsHelper.Current.SelectedAppTheme = theme;
        AppThemeHelper.Apply(theme);
        SyncThemeMenuSelection();
    }

    private void SyncThemeMenuSelection()
    {
        var theme = SettingsHelper.Current.SelectedAppTheme;
        themeLightItem.IsChecked = theme == ElementTheme.Light;
        themeDarkItem.IsChecked = theme == ElementTheme.Dark;
        themeDefaultItem.IsChecked = theme == ElementTheme.Default;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThemeSwitcherGlyph)));
    }

    private static string GetThemeGlyph(ElementTheme theme) => theme switch
    {
        ElementTheme.Light => "\uE706",
        ElementTheme.Dark => "\uE708",
        _ => "\uE7F8",
    };

    private async void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authService is null)
        {
            return;
        }

        try
        {
            if (_realtimeService is not null)
                await _realtimeService.StopAsync();

            _shellPresenter?.Stop();

            await _authService.SignOutAsync();
        }
        finally
        {
            global::Helpers.AppRestartHelper.RestartApp();
        }
    }

    private async Task UpdateConnectionStatusAsync()
    {
        if (_connectionService is null)
        {
            return;
        }

        await _connectionService.CheckAsync().ConfigureAwait(false);
        NotifyConnectionStatusChanged();
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        NotifyConnectionStatusChanged();
    }

    private void NotifyConnectionStatusChanged()
    {
        _uiDispatcherQueue?.TryEnqueue(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsServerConnected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionMarkerColorKey)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionMarkerTooltip)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowConnectionOfflineLabel)));
        });
    }

    private void NotifyServerAddressChanged()
    {
        _uiDispatcherQueue?.TryEnqueue(() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerAddressText))));
    }

    private void OnProfileChanged(object? sender, UserProfile? profile)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileName)));
    }

    private static string ResolveHomePageKey(UserRole role) => role switch
    {
        UserRole.Admin => NavHomePageKeys.Admin,
        UserRole.Dispatcher => NavHomePageKeys.Dispatcher,
        _ => NavHomePageKeys.Requester,
    };
}
