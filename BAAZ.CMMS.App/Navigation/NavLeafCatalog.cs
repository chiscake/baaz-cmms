using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavLeafCatalog
{
    public static NavLeaf Home { get; } = new(
        NavMenuTags.Home,
        PageKey: null,
        TitleResourceKey: "Nav_Home",
        Icon: Symbol.Home);

    public static NavLeaf Equipment { get; } = new(
        NavMenuTags.Equipment,
        PageKey: "AssetRegistry",
        TitleResourceKey: "Nav_Equipment",
        Icon: Symbol.List);

    public static NavLeaf Personnel { get; } = new(
        NavMenuTags.Personnel,
        PageKey: "PersonnelManagement",
        TitleResourceKey: "Nav_Personnel",
        Icon: Symbol.People);

    public static NavLeaf MaintenanceSchedule { get; } = new(
        NavMenuTags.MaintenanceSchedule,
        PageKey: "MaintenanceSchedule",
        TitleResourceKey: "Nav_MaintenanceSchedule",
        Icon: Symbol.Calendar);

    public static NavLeaf WorkReports { get; } = new(
        NavMenuTags.WorkReports,
        PageKey: "WorkReports",
        TitleResourceKey: "Nav_WorkReports",
        Icon: Symbol.Document);

    public static NavLeaf IncomingRequests { get; } = new(
        NavMenuTags.IncomingRequests,
        PageKey: "IncomingRequests",
        TitleResourceKey: "Nav_IncomingRequests",
        Icon: Symbol.Message);

    public static NavLeaf NewRequest { get; } = new(
        NavMenuTags.NewRequest,
        PageKey: "NewRequest",
        TitleResourceKey: "Nav_NewRequest",
        Icon: Symbol.Add);

    public static NavLeaf MyRequests { get; } = new(
        NavMenuTags.MyRequests,
        PageKey: "MyRequests",
        TitleResourceKey: "Nav_MyRequests",
        Icon: Symbol.Message);

    public static NavLeaf AllRequests { get; } = new(
        NavMenuTags.AllRequests,
        PageKey: "AllRequests",
        TitleResourceKey: "Nav_AllRequests",
        Icon: Symbol.Document);

    public static NavLeaf AuditLog { get; } = new(
        NavMenuTags.AuditLog,
        PageKey: "AuditLog",
        TitleResourceKey: "Nav_AuditLog",
        Icon: Symbol.Clock);

    public static NavLeaf RequestHistory { get; } = new(
        NavMenuTags.RequestHistory,
        PageKey: "RequestHistory",
        TitleResourceKey: "Nav_RequestHistory",
        Icon: Symbol.Clock);

    public static NavLeaf RequesterAssets { get; } = new(
        NavMenuTags.RequesterAssets,
        PageKey: "RequesterAssets",
        TitleResourceKey: "Nav_RequesterAssets",
        Icon: Symbol.List);

    public static NavLeaf Locations { get; } = new(
        NavMenuTags.Locations,
        PageKey: "Locations",
        TitleResourceKey: "Nav_Locations",
        Icon: Symbol.MapPin);

    public static NavLeaf RepairDepartments { get; } = new(
        NavMenuTags.RepairDepartments,
        PageKey: "RepairDepartments",
        TitleResourceKey: "Nav_RepairDepartments",
        Icon: Symbol.Repair);

    public static NavLeaf Users { get; } = new(
        NavMenuTags.Users,
        PageKey: "Users",
        TitleResourceKey: "Nav_Users",
        Icon: Symbol.Contact);

    public static NavLeaf MaintenanceNorms { get; } = new(
        NavMenuTags.MaintenanceNorms,
        PageKey: "MaintenanceNorms",
        TitleResourceKey: "Nav_MaintenanceNorms",
        Icon: Symbol.Edit);

    public static NavLeaf MaterialRequisition { get; } = new(
        NavMenuTags.MaterialRequisition,
        PageKey: "MaterialRequisition",
        TitleResourceKey: "Nav_MaterialRequisition",
        Icon: Symbol.Shop);

    public static NavLeaf ToolRequisition { get; } = new(
        NavMenuTags.ToolRequisition,
        PageKey: "ToolRequisition",
        TitleResourceKey: "Nav_ToolRequisition",
        Icon: Symbol.Repair);

    public static NavLeaf ToolRequisitionHistory { get; } = new(
        NavMenuTags.ToolRequisitionHistory,
        PageKey: "ToolRequisitionHistory",
        TitleResourceKey: "Nav_ToolRequisitionHistory",
        Icon: Symbol.List);

    public static NavLeaf Settings { get; } = new(
        NavMenuTags.Settings,
        PageKey: "Settings",
        TitleResourceKey: "Settings_Title",
        Icon: Symbol.Setting);

    public static string? GetTitleResourceKey(string sectionTag) => sectionTag switch
    {
        NavMenuTags.Home => Home.TitleResourceKey,
        NavMenuTags.Equipment => Equipment.TitleResourceKey,
        NavMenuTags.Personnel => Personnel.TitleResourceKey,
        NavMenuTags.MaintenanceSchedule => MaintenanceSchedule.TitleResourceKey,
        NavMenuTags.WorkReports => WorkReports.TitleResourceKey,
        NavMenuTags.IncomingRequests => IncomingRequests.TitleResourceKey,
        NavMenuTags.NewRequest => NewRequest.TitleResourceKey,
        NavMenuTags.MyRequests => MyRequests.TitleResourceKey,
        NavMenuTags.AllRequests => AllRequests.TitleResourceKey,
        NavMenuTags.AuditLog => AuditLog.TitleResourceKey,
        NavMenuTags.RequestHistory => RequestHistory.TitleResourceKey,
        NavMenuTags.RequesterAssets => RequesterAssets.TitleResourceKey,
        NavMenuTags.Locations => Locations.TitleResourceKey,
        NavMenuTags.RepairDepartments => RepairDepartments.TitleResourceKey,
        NavMenuTags.Users => Users.TitleResourceKey,
        NavMenuTags.MaintenanceNorms => MaintenanceNorms.TitleResourceKey,
        NavMenuTags.MaterialRequisition => MaterialRequisition.TitleResourceKey,
        NavMenuTags.ToolRequisition => ToolRequisition.TitleResourceKey,
        NavMenuTags.ToolRequisitionHistory => ToolRequisitionHistory.TitleResourceKey,
        NavMenuTags.Settings => Settings.TitleResourceKey,
        _ => null,
    };
}
