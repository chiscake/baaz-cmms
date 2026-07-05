using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.Catalog;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Pages.Home.Dashboards;

public sealed class AdminHomeDashboardViewModel : HomeDashboardSectionViewModel
{
    private readonly IProfileAdminService _profileAdminService;
    private readonly IAssetCatalogService _assetCatalogService;
    private readonly ILocationCatalogService _locationCatalogService;
    private readonly IRepairDepartmentCatalogService _repairDepartmentCatalogService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IRequestService _requestService;

    public AdminHomeDashboardViewModel(
        IProfileAdminService profileAdminService,
        IAssetCatalogService assetCatalogService,
        ILocationCatalogService locationCatalogService,
        IRepairDepartmentCatalogService repairDepartmentCatalogService,
        IMaintenanceService maintenanceService,
        IRequestService requestService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _profileAdminService = profileAdminService;
        _assetCatalogService = assetCatalogService;
        _locationCatalogService = locationCatalogService;
        _repairDepartmentCatalogService = repairDepartmentCatalogService;
        _maintenanceService = maintenanceService;
        _requestService = requestService;

        this.AddDashboardAction(NavLeafCatalog.Users);
        this.AddDashboardAction(NavLeafCatalog.Locations);
        this.AddDashboardAction(NavLeafCatalog.Equipment);
        this.AddDashboardAction(NavLeafCatalog.MaintenanceNorms);

        this.AddDashboardNavLink(NavLeafCatalog.RepairDepartments);
        this.AddDashboardNavLink(NavLeafCatalog.AllRequests);
    }

    public override string RoleLabel => ResourceStrings.Get("Home_RoleBadge_Admin");

    public override string SectionHeading => ResourceStrings.Get("Home_Heading_Admin");

    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        LoadError = null;
        ClearStats();

        try
        {
            var row = BeginStatRow(4);

            var profilesResult = await _profileAdminService.GetProfilesAsync(cancellationToken);
            if (!profilesResult.IsSuccess)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            var profiles = profilesResult.Value!;
            AddStat(row, "Home_Stat_UsersTotal", profiles.Count, "\uE779", StatusBadgeColorToken.BlueGrey);
            AddStat(row, "Home_Stat_UsersActive", profiles.Count(p => !p.IsBanned), "\uE77B", StatusBadgeColorToken.Green);

            var assetsResult = await _assetCatalogService.GetAssetsAdminAsync(
                includeDecommissioned: false,
                cancellationToken);
            if (!assetsResult.IsSuccess)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            var assets = assetsResult.Value!;
            AddStat(
                row,
                "Home_Stat_AssetsActive",
                assets.Count(a => string.Equals(a.Status, "active", StringComparison.Ordinal)),
                "\uE7F4",
                AssetColor("active"));
            AddStat(
                row,
                "Home_Stat_AssetsMaintenance",
                assets.Count(a => string.Equals(a.Status, "maintenance", StringComparison.Ordinal)),
                "\uE90F",
                AssetColor("maintenance"));

            var locations = await _locationCatalogService.GetLocationsAsync(includeInactive: false, cancellationToken);
            AddStat(row, "Home_Stat_Locations", locations.Count, "\uE707", StatusBadgeColorToken.BlueGrey);

            var departmentsResult = await _repairDepartmentCatalogService.GetRepairDepartmentsAdminAsync(
                includeInactive: false,
                cancellationToken);
            if (!departmentsResult.IsSuccess)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            AddStat(row, "Home_Stat_RepairDepartments", departmentsResult.Value!.Count, "\uE821", StatusBadgeColorToken.BlueGrey);

            var normsResult = await _maintenanceService.GetAllEffectiveNormsAsync(cancellationToken);
            if (!normsResult.IsSuccess)
            {
                LoadError = ResourceStrings.Get("Home_LoadError");
                return;
            }

            AddStat(row, "Home_Stat_MaintenanceNorms", normsResult.Value!.Count, "\uE70F", StatusBadgeColorToken.BlueGrey);

            var allRequests = await _requestService.GetAllRequestsAsync(cancellationToken: cancellationToken);
            AddStat(
                row,
                "Home_Stat_RequestsCompleted",
                allRequests.Count(r => string.Equals(r.Status, "completed", StringComparison.Ordinal)),
                "\uE787",
                RequestColor("completed"));
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
