using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Helpers;

using BAAZ.CMMS.App.Controls.Home;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.App.Navigation;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Services;
using BAAZ.CMMS.Core.Services.TmsIssuance;

using WinUI.UtilsLibrary.Contracts;

namespace BAAZ.CMMS.App.Pages.Home.Dashboards;

public sealed class DispatcherHomeDashboardViewModel : HomeDashboardSectionViewModel
{
    private readonly IRequestService _requestService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService;

    public DispatcherHomeDashboardViewModel(
        IRequestService requestService,
        IMaintenanceService maintenanceService,
        ITmsToolRequisitionService tmsToolRequisitionService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _requestService = requestService;
        _maintenanceService = maintenanceService;
        _tmsToolRequisitionService = tmsToolRequisitionService;

        this.AddDashboardAction(NavLeafCatalog.IncomingRequests);
        this.AddDashboardAction(NavLeafCatalog.MaintenanceSchedule);
        this.AddDashboardAction(NavLeafCatalog.MaterialRequisition);
        this.AddDashboardAction(NavLeafCatalog.ToolRequisition);

        this.AddDashboardNavLink(NavLeafCatalog.RequestHistory);
        this.AddDashboardNavLink(NavLeafCatalog.ToolRequisitionHistory);
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

            await _tmsToolRequisitionService.RefreshAllLocalAsync(cancellationToken: cancellationToken);
            var toolLinksResult = await _tmsToolRequisitionService.ListAllLocalAsync(cancellationToken: cancellationToken);
            var readyForIssueCount = toolLinksResult.IsSuccess && toolLinksResult.Value is not null
                ? toolLinksResult.Value.Count(link =>
                    string.Equals(link.LastKnownStatus, TmsRequisitionStatuses.ReadyForIssue, StringComparison.Ordinal))
                : 0;

            var scheduleRow = BeginStatRow(4, "Home_Row_PlannedMaintenance", "Home_Row_Tools");
            AddStat(scheduleRow, "Home_Stat_ScheduleOverdue", overdueCount, "\uE7BA", ScheduleColor("overdue"));
            AddStat(scheduleRow, "Home_Stat_ScheduleToday", todayCount, "\uE787", ScheduleColor("scheduled"));
            AddStat(scheduleRow, "Home_Stat_ScheduleInProgress", scheduleInProgressCount, "\uE9D9", ScheduleColor("in_progress"));
            AddStat(
                scheduleRow,
                "Home_Stat_ToolRequisitionsReady",
                readyForIssueCount,
                "\uE90F",
                StatusBadgeColorToken.Green,
                "ToolRequisitionHistory",
                new ToolRequisitionHistoryNavigationArgs
                {
                    StatusFilter = TmsRequisitionStatuses.ReadyForIssue,
                });
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
