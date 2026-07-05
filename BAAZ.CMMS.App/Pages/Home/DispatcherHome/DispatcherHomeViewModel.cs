using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Home.Dashboards;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Home.DispatcherHome;

public sealed class DispatcherHomeViewModel : PageViewModelBase
{
    public DispatcherHomeViewModel(
        DispatcherHomeDashboardViewModel dispatcherSection,
        RequesterHomeDashboardViewModel requesterSection)
    {
        DispatcherSection = dispatcherSection;
        RequesterSection = requesterSection;
    }

    public override string PageTitle => ResourceStrings.Get("Home_Title");

    public DispatcherHomeDashboardViewModel DispatcherSection { get; }

    public RequesterHomeDashboardViewModel RequesterSection { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        InfoBanner.Report(string.Empty);

        await Task.WhenAll(
            DispatcherSection.LoadAsync(cancellationToken),
            RequesterSection.LoadAsync(cancellationToken));

        ReportSectionErrors(DispatcherSection, RequesterSection);
    }

    private void ReportSectionErrors(params HomeDashboardSectionViewModel[] sections)
    {
        if (sections.Any(s => s.LoadError is not null))
        {
            InfoBanner.Report(ResourceStrings.Get("Home_LoadError"), InfoBarSeverity.Error);
        }
    }
}
