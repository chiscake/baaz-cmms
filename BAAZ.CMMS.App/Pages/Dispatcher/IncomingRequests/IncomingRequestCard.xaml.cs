using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;

public sealed partial class IncomingRequestCard : UserControl
{
    public IncomingRequestCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => WireActions();

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => WireActions();

    private void WireActions()
    {
        if (DataContext is not IncomingRequestRow row)
            return;

        var page = row.Page;

        AcceptButton.Content = page.ActionAccept;
        AcceptButton.Command = page.AcceptCommand;
        AcceptButton.CommandParameter = row;
        AcceptButton.Visibility = row.IsNew ? Visibility.Visible : Visibility.Collapsed;

        RejectButton.Content = page.ActionReject;
        RejectButton.Command = page.RejectCommand;
        RejectButton.CommandParameter = row;
        RejectButton.Visibility = row.IsNew ? Visibility.Visible : Visibility.Collapsed;

        OpenButton.Content = page.ActionOpen;
        OpenButton.Command = page.OpenRequestCommand;
        OpenButton.CommandParameter = row;
        OpenButton.Visibility = Visibility.Visible;
    }
}
