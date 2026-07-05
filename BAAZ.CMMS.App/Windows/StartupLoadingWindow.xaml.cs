using System;
using System.Diagnostics;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using Helpers.Microsoft;
using Helpers.Settings;

using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Windows;

public sealed partial class StartupLoadingWindow : Window
{
    public string WindowTitleText => ResourceStrings.Get("Startup_Window_Title");

    public string StatusText => ResourceStrings.Get("Startup_Status");

    public StartupLoadingWindow()
    {
        Debug.WriteLine($"[StartupLoadingWindow] ctor, thread={Environment.CurrentManagedThreadId}");
        InitializeComponent();
        rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme;
        if (Content is FrameworkElement root)
        {
            root.DataContext = this;
        }

        Title = ResourceStrings.Get("Startup_Window_Title");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        WindowHelper.TrackWindow(this);
    }

    public void ShowAndActivate()
    {
        Debug.WriteLine($"[StartupLoadingWindow:{GetHashCode():X8}] Activate");
        Activate();
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(
            $"[StartupLoadingWindow:{GetHashCode():X8}] Loaded, size={rootGrid.ActualWidth}x{rootGrid.ActualHeight}, theme={rootGrid.ActualTheme}");
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, rootGrid.ActualTheme);
        WindowSizeDefaults.ApplyStartupLoadingWindowSize(this);
    }
}
