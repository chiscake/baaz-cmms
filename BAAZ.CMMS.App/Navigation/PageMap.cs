using System;
using System.Collections.Generic;

using BAAZ.CMMS.App.Pages.Admin.AllRequests;
using BAAZ.CMMS.App.Pages.Admin.AssetRegistry;
using BAAZ.CMMS.App.Pages.Admin.Locations;
using BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;
using BAAZ.CMMS.App.Pages.Admin.RepairDepartments;
using BAAZ.CMMS.App.Pages.Admin.Users;
using BAAZ.CMMS.App.Pages.Dispatcher.IncomingRequests;
using BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;
using BAAZ.CMMS.App.Pages.Dispatcher.MaterialRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.PersonnelManagement;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestHistory;
using BAAZ.CMMS.App.Pages.Dispatcher.RequestDetail;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisition;
using BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;
using BAAZ.CMMS.App.Pages.Dispatcher.WorkReports;
using BAAZ.CMMS.App.Pages.Home.AdminHome;
using BAAZ.CMMS.App.Pages.Home.DispatcherHome;
using BAAZ.CMMS.App.Pages.Home.RequesterHome;
using BAAZ.CMMS.App.Pages.Requester.MyRequests;
using BAAZ.CMMS.App.Pages.Requester.NewRequest;
using BAAZ.CMMS.App.Pages.Requester.RequesterAssets;
using BAAZ.CMMS.App.Pages.Settings;

namespace BAAZ.CMMS.App.Navigation;

internal static class PageMap
{
    public static IReadOnlyDictionary<string, Type> Pages { get; } = new Dictionary<string, Type>
    {
        ["HomeAdmin"] = typeof(AdminHomePage),
        ["HomeDispatcher"] = typeof(DispatcherHomePage),
        ["HomeRequester"] = typeof(RequesterHomePage),
        // Requests
        ["NewRequest"] = typeof(NewRequestPage),
        ["MyRequests"] = typeof(MyRequestsPage),
        ["RequesterAssets"] = typeof(RequesterAssetsPage),
        ["IncomingRequests"] = typeof(IncomingRequestsPage),
        ["RequestDetail"] = typeof(RequestDetailPage),
        ["RequestHistory"] = typeof(RequestHistoryPage),
        // Maintenance
        ["MaintenanceSchedule"] = typeof(MaintenanceSchedulePage),
        ["WorkReports"] = typeof(WorkReportsPage),
        ["MaintenanceNorms"] = typeof(MaintenanceNormsPage),
        // Supply
        ["MaterialRequisition"] = typeof(MaterialRequisitionPage),
        ["ToolRequisition"] = typeof(ToolRequisitionPage),
        ["ToolRequisitionHistory"] = typeof(ToolRequisitionHistoryPage),
        // Assets & Personnel
        ["AssetRegistry"] = typeof(AssetRegistryPage),
        ["PersonnelManagement"] = typeof(PersonnelManagementPage),
        // Admin
        ["Locations"] = typeof(LocationsPage),
        ["RepairDepartments"] = typeof(RepairDepartmentsPage),
        ["Users"] = typeof(UsersPage),
        ["AllRequests"] = typeof(AllRequestsPage),
        // Shell
        ["Settings"] = typeof(SettingsPage),
    };
}
