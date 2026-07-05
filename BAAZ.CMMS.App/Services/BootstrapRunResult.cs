using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Services;

/// <summary>
/// Результат начальной загрузки. Окно входа скрывается после MainWindow; процесс завершается в App.ShutdownAfterMainWindowClosedAsync.
/// </summary>
public sealed record BootstrapRunResult(bool CanOpenMainWindow, Window? WindowToHideAfterMain);
