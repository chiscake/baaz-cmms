using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Home.Dashboards;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Home.AdminHome;

public sealed class AdminHomeViewModel : PageViewModelBase
{
    public AdminHomeViewModel(
        AdminHomeDashboardViewModel adminSection,
        DispatcherHomeDashboardViewModel dispatcherSection,
        RequesterHomeDashboardViewModel requesterSection)
    {
        AdminSection = adminSection;
        DispatcherSection = dispatcherSection;
        RequesterSection = requesterSection;
    }

    public override string PageTitle => ResourceStrings.Get("Home_Title");

    public AdminHomeDashboardViewModel AdminSection { get; }

    public DispatcherHomeDashboardViewModel DispatcherSection { get; }

    public RequesterHomeDashboardViewModel RequesterSection { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        InfoBanner.Report(string.Empty);

        await Task.WhenAll(
            AdminSection.LoadAsync(cancellationToken),
            DispatcherSection.LoadAsync(cancellationToken),
            RequesterSection.LoadAsync(cancellationToken));

        ReportSectionErrors(AdminSection, DispatcherSection, RequesterSection);
    }

    private void ReportSectionErrors(params HomeDashboardSectionViewModel[] sections)
    {
        if (sections.Any(s => s.LoadError is not null))
        {
            InfoBanner.Report(ResourceStrings.Get("Home_LoadError"), InfoBarSeverity.Error);
        }
    }
}
