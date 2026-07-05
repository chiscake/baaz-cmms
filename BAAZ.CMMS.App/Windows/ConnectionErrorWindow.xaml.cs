using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

using Helpers.Microsoft;
using Helpers.Settings;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace BAAZ.CMMS.App.Windows;

public sealed partial class ConnectionErrorWindow : Window
{
    private TaskCompletionSource<bool>? _completionSource;

    public string WindowTitleText => ResourceStrings.Get("Connection_Window_Title");

    public string TitleText => ResourceStrings.Get("Connection_Error_Title");

    public string MessageText => ResourceStrings.Get("Connection_Error_Message");

    public string RetryButtonText => ResourceStrings.Get("Connection_Retry");

    public ConnectionErrorWindow()
    {
        InitializeComponent();
        rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme;
        if (Content is FrameworkElement root)
        {
            root.DataContext = this;
        }

        Title = ResourceStrings.Get("Connection_Window_Title");
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);
        WindowHelper.TrackWindow(this);
        Closed += OnClosed;
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
