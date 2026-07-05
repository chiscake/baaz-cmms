using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using Helpers.Microsoft;
using Helpers.Settings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Windows;

public sealed partial class ConnectionErrorWindow : Window
{
    private readonly ConnectionErrorViewModel _viewModel;
    private TaskCompletionSource<bool>? _completionSource;

    public ConnectionErrorWindow(ConnectionErrorViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme;
        rootGrid.DataContext = _viewModel;

        Title = ResourceStrings.Get("Connection_Window_Title");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        WindowHelper.TrackWindow(this);
        Closed += OnClosed;
    }

    public ConnectionErrorWindow() : this(App.Services.GetRequiredService<ConnectionErrorViewModel>())
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
        WindowSizeDefaults.ApplyConnectionErrorWindowSize(this);
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            _completionSource?.TrySetResult(true));
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_completionSource is { Task.IsCompleted: false })
        {
            _completionSource.TrySetResult(false);
        }
    }
}
