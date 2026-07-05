using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.Core.Services;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Pages.Home.Dashboards;

public sealed class DispatcherHomeDashboardViewModel : HomeDashboardSectionViewModel
{
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;

    public DispatcherHomeDashboardViewModel(
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _requestService = requestService;
        _maintenanceService = maintenanceService;

        this.AddDashboardAction(NavLeafCatalog.IncomingRequests);
        this.AddDashboardAction(NavLeafCatalog.MaintenanceSchedule);
        this.AddDashboardAction(NavLeafCatalog.MaterialRequisition);
        this.AddDashboardAction(NavLeafCatalog.ToolRequisition);

        this.AddDashboardNavLink(NavLeafCatalog.RequestHistory);
        this.AddDashboardNavLink(NavLeafCatalog.WorkReports);
        this.AddDashboardNavLink(NavLeafCatalog.Personnel);
    }

    public override string RoleLabel => ResourceStrings.Get("Home_RoleBadge_Dispatcher");

    public override string SectionHeading => ResourceStrings.Get("Home_Heading_Dispatcher");

    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        LoadError = null;
        ClearStats();

        try
        {
            var incoming = await _requestService.GetIncomingRequestsAsync(cancellationToken);
            var newCount = incoming.Count(r => string.Equals(r.Status, "new", StringComparison.Ordinal));

            var departmentRequests = await _requestService.GetRequestsByStatusesAsync(
                ["accepted", "in_progress", "completed"],
                cancellationToken);
            var acceptedCount = departmentRequests.Count(r => string.Equals(r.Status, "accepted", StringComparison.Ordinal));
            var inProgressCount = departmentRequests.Count(r => string.Equals(r.Status, "in_progress", StringComparison.Ordinal));
            var completedCount = departmentRequests.Count(r => string.Equals(r.Status, "completed", StringComparison.Ordinal));

            var requestRow = BeginStatRow(4, "Home_Row_Requests");
            AddStat(requestRow, "Home_Stat_New", newCount, "\uE8BD", RequestColor("new"));
            AddStat(requestRow, "Home_Stat_AcceptedRequests", acceptedCount, "\uE787", RequestColor("accepted"));
            AddStat(requestRow, "Home_Stat_InProgressRequests", inProgressCount, "\uE9D9", RequestColor("in_progress"));
            AddStat(requestRow, "Home_Stat_RequestsCompleted", completedCount, "\uE73E", RequestColor("completed"));

            var schedule = await _maintenanceService.GetScheduleAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var overdueCount = schedule.Count(s => string.Equals(s.Status, "overdue", StringComparison.Ordinal));
            var todayCount = schedule.Count(s =>
                string.Equals(s.Status, "scheduled", StringComparison.Ordinal)
                && s.PlannedDate == today);
            var scheduleInProgressCount = schedule.Count(s => string.Equals(s.Status, "in_progress", StringComparison.Ordinal));

            var scheduleRow = BeginStatRow(4, "Home_Row_PlannedMaintenance");
            AddStat(scheduleRow, "Home_Stat_ScheduleOverdue", overdueCount, "\uE7BA", ScheduleColor("overdue"));
            AddStat(scheduleRow, "Home_Stat_ScheduleToday", todayCount, "\uE787", ScheduleColor("scheduled"));
            AddStat(scheduleRow, "Home_Stat_ScheduleInProgress", scheduleInProgressCount, "\uE9D9", ScheduleColor("in_progress"));
            AddStatSpacer(scheduleRow);
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
