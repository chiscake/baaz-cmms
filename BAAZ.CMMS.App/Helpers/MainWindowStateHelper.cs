using System;

using Helpers.Settings;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>
/// Отслеживает изменение размера/позиции/развёрнутости <see cref="Window"/> и сохраняет
/// состояние в <see cref="SettingsHelper"/>, чтобы восстановить его при следующем запуске.
/// </summary>
public static class MainWindowStateHelper
{
    /// <summary>
    /// Восстанавливает сохранённое состояние окна (если есть) и начинает отслеживать
    /// последующие изменения размера/позиции/развёрнутости для сохранения в настройках.
    /// </summary>
    public static void Attach(Window window)
    {
        RestoreState(window);

        var appWindow = window.AppWindow;
        appWindow.Changed += (sender, args) =>
        {
            if (args.DidSizeChange || args.DidPositionChange || args.DidPresenterChange)
            {
                SaveState(sender);
            }
        };
    }

    private static void RestoreState(Window window)
    {
        var settings = SettingsHelper.Current;
        var appWindow = window.AppWindow;

        if (settings.MainWindowWidth > 0 && settings.MainWindowHeight > 0)
        {
            appWindow.Resize(new SizeInt32(settings.MainWindowWidth, settings.MainWindowHeight));
        }

        if (settings.MainWindowX != int.MinValue && settings.MainWindowY != int.MinValue)
        {
            appWindow.Move(new PointInt32(settings.MainWindowX, settings.MainWindowY));
        }

        if (settings.MainWindowIsMaximized && appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private static void SaveState(AppWindow appWindow)
    {
        var settings = SettingsHelper.Current;
        var isMaximized = appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized };

        settings.MainWindowIsMaximized = isMaximized;

        // При развёрнутом/свёрнутом окне AppWindow.Size/Position отражают не «восстановленные»
        // границы, а состояние на экране — не перезаписываем сохранённые размеры в этом случае.
        if (isMaximized || appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
        {
            return;
        }

        settings.MainWindowWidth = appWindow.Size.Width;
        settings.MainWindowHeight = appWindow.Size.Height;
        settings.MainWindowX = appWindow.Position.X;
        settings.MainWindowY = appWindow.Position.Y;
    }
}
