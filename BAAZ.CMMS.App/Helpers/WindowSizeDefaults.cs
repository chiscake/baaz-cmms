using System;

using Helpers.Microsoft;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Размеры окон приложения (логические пиксели, до учёта DPI).
/// </summary>
public static class WindowSizeDefaults
{
    public static class Login
    {
        public const double Width = 440;
        public const double Height = 480;
        public const double DebugHeight = 600;
    }

    public static class ConnectionError
    {
        public const double Width = 480;
        public const double Height = 480;
    }

    public static class StartupLoading
    {
        public const double Width = 400;
        public const double Height = 280;
    }

    public static class MainWindow
    {
        public const double MinWidth = 1024;
        public const double MinHeight = 640;
    }

    public static void ApplyLoginWindowSize(Window window) =>
        ApplyFixedSize(window, Login.Width, Login.Height);

    public static void ApplyConnectionErrorWindowSize(Window window) =>
        ApplyFixedSize(window, ConnectionError.Width, ConnectionError.Height);

    public static void ApplyStartupLoadingWindowSize(Window window) =>
        ApplyFixedSize(window, StartupLoading.Width, StartupLoading.Height);

    public static void ApplyMainWindowMinSize(Window window) =>
        ApplyMinSize(window, MainWindow.MinWidth, MainWindow.MinHeight);

    public static void ApplyFixedSize(Window window, double width, double height)
    {
        EnsureAppliedOnReady(window, () =>
        {
            WindowHelper.SetWindowMinSize(window, width, height);
            SetWindowMaxSize(window, width, height);
            ResizeWindow(window, width, height);
        });
    }

    public static void ApplyMinSize(Window window, double width, double height)
    {
        EnsureAppliedOnReady(window, () => WindowHelper.SetWindowMinSize(window, width, height));
    }

    private static void EnsureAppliedOnReady(Window window, Action apply)
    {
        if (window.Content is not FrameworkElement root)
        {
            return;
        }

        if (root.XamlRoot is not null)
        {
            apply();
            return;
        }

        root.Loaded += (_, _) => apply();
    }

    private static void SetWindowMaxSize(Window window, double width, double height)
    {
        if (window.Content is not FrameworkElement windowContent || windowContent.XamlRoot is null)
        {
            return;
        }

        if (window.AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        var scale = windowContent.XamlRoot.RasterizationScale;
        presenter.PreferredMaximumWidth = (int)(width * scale);
        presenter.PreferredMaximumHeight = (int)(height * scale);
    }

    private static void ResizeWindow(Window window, double width, double height)
    {
        if (window.Content is not FrameworkElement windowContent || windowContent.XamlRoot is null)
        {
            return;
        }

        var scale = windowContent.XamlRoot.RasterizationScale;
        window.AppWindow.Resize(new SizeInt32((int)(width * scale), (int)(height * scale)));
    }
}
