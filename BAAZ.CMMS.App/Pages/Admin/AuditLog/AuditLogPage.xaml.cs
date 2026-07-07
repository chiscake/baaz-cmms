using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BAAZ.CMMS.App.Pages.Admin.AuditLog;

public sealed partial class AuditLogPage : Page
{
    public AuditLogViewModel ViewModel { get; }

    public AuditLogPage()
    {
        ViewModel = App.Services.GetRequiredService<AuditLogViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.OnPageLoadedAsync(e.Parameter);
    }
}
