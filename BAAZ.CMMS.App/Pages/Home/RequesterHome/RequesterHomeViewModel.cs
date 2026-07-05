using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Pages.Home.Dashboards;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Pages.Home.RequesterHome;

public sealed class RequesterHomeViewModel : PageViewModelBase
{
    public RequesterHomeViewModel(RequesterHomeDashboardViewModel requesterSection)
    {
        RequesterSection = requesterSection;
    }

    public override string PageTitle => ResourceStrings.Get("Home_Title");

    public RequesterHomeDashboardViewModel RequesterSection { get; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        InfoBanner.Report(string.Empty);
        await RequesterSection.LoadAsync(cancellationToken);

        if (RequesterSection.LoadError is not null)
        {
            InfoBanner.Report(RequesterSection.LoadError, InfoBarSeverity.Error);
        }
    }
}
