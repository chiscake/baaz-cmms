using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Services.Notifications;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Pages.Home.Dashboards;

public sealed class RequesterHomeDashboardViewModel : HomeDashboardSectionViewModel
{
    private static readonly string[] ActiveStatuses = ["new", "accepted", "in_progress"];

    private readonly IAuthService _authService;
    private readonly IRequestService _requestService;
    private readonly IRequesterAssetCatalog _requesterAssetCatalog;
    private readonly INavBadgeService _navBadgeService;

    public RequesterHomeDashboardViewModel(
        IAuthService authService,
        IRequestService requestService,
        IRequesterAssetCatalog requesterAssetCatalog,
        INavBadgeService navBadgeService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _authService = authService;
        _requestService = requestService;
        _requesterAssetCatalog = requesterAssetCatalog;
        _navBadgeService = navBadgeService;

        this.AddDashboardAction(NavLeafCatalog.NewRequest, isPrimary: true);
        this.AddDashboardAction(NavLeafCatalog.MyRequests);
        this.AddDashboardAction(NavLeafCatalog.RequesterAssets);
    }

    public override string RoleLabel => ResourceStrings.Get("Home_RoleBadge_Requester");

    public override string SectionHeading => ResourceStrings.Get("Home_Heading_Requester");

    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        LoadError = null;
        ClearStats();

        try
        {
            var profile = _authService.CurrentProfile;
            if (profile is null)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            var requests = await _requestService.GetMyRequestsAsync(profile.Id, cancellationToken: cancellationToken);
            var activeCount = requests.Count(r => ActiveStatuses.Contains(r.Status, StringComparer.Ordinal));
            var awaitingCount = requests.Count(r => string.Equals(r.Status, "completed", StringComparison.Ordinal));

            var assetsResult = await _requesterAssetCatalog.GetActiveScopedAssetsAsync(cancellationToken);
            if (!assetsResult.IsSuccess)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            var statusUpdates = _navBadgeService.GetCount(NavItemIds.RequesterMyRequests);

            var row = BeginStatRow(4);
            AddStat(row, "Home_Stat_ActiveRequests", activeCount, "\uE8BD", StatusBadgeColorToken.BlueGrey);
            AddStat(row, "Home_Stat_AwaitingAcceptance", awaitingCount, "\uE787", RequestColor("completed"));
            AddStat(row, "Home_Stat_ScopedAssets", assetsResult.Value!.Count, "\uE115", AssetColor("active"));
            AddStat(row, "Home_Stat_StatusUpdates", statusUpdates, "\uE95E", StatusBadgeColorToken.BlueGrey);
        }
        catch (Exception)
        {
            LoadError = ResourceStrings.Get("Home_LoadError");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
