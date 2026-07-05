using System.Collections.Generic;

using BAAZ.CMMS.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Navigation;

internal static class NavMenuRegistry
{
    /// <summary>Показать «Главная» диспетчера внутри группы «Диспетчер» у admin.</summary>
    internal static readonly bool IncludeDispatcherHomeInAdminGroup = false;

    /// <summary>Показать «Главная» заявителя внутри группы «Заявитель» у admin.</summary>
    internal static readonly bool IncludeRequesterHomeInAdminGroup = false;

    private static readonly IReadOnlyList<NavMenuNode> AdminMenu = BuildAdminMenu();

    private static readonly IReadOnlyList<NavMenuNode> DispatcherMenu = BuildDispatcherMenu();

    private static readonly IReadOnlyList<NavMenuNode> RequesterMenu =
    [
        Leaf(NavItemIds.RequesterHome, NavLeafCatalog.Home),
        Leaf(NavItemIds.RequesterNewRequest, NavLeafCatalog.NewRequest),
        Leaf(NavItemIds.RequesterMyRequests, NavLeafCatalog.MyRequests),
        Leaf(NavItemIds.RequesterAssets, NavLeafCatalog.RequesterAssets),
    ];

    private static readonly Dictionary<string, NavMenuLeafNode> LeafIndex = BuildLeafIndex();

    public static NavLeaf Settings => NavLeafCatalog.Settings;

    public static IReadOnlyList<NavMenuNode> GetMenuTree(UserRole role) => role switch
    {
        UserRole.Admin => AdminMenu,
        UserRole.Dispatcher => DispatcherMenu,
        UserRole.Requester => RequesterMenu,
        _ => RequesterMenu,
    };

    public static bool TryResolveLeaf(string menuItemId, out NavMenuLeafNode leaf) =>
        LeafIndex.TryGetValue(menuItemId, out leaf!);

    public static string? GetTitleResourceKey(string sectionTag) =>
        NavLeafCatalog.GetTitleResourceKey(sectionTag);

    public static string? FindFirstLeafId(
        UserRole role,
        string? pageKey,
        object? parameter,
        string roleHomePageKey)
    {
        foreach (var leaf in EnumerateLeaves(GetMenuTree(role)))
        {
            if (NavRouter.MatchesNavigation(leaf, pageKey, parameter, roleHomePageKey))
            {
                return leaf.Id;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> GetGroupPathToLeaf(UserRole role, string leafId)
    {
        var path = new List<string>();
        if (TryFindGroupPath(GetMenuTree(role), leafId, path))
        {
            return path;
        }

        return [];
    }

    private static IReadOnlyList<NavMenuNode> BuildDispatcherMenu()
    {
        var nodes = new List<NavMenuNode>
        {
            Leaf(NavItemIds.DispatcherHome, NavLeafCatalog.Home),
            Leaf(NavItemIds.DispatcherIncomingRequests, NavLeafCatalog.IncomingRequests),
            Leaf(NavItemIds.DispatcherMaintenanceSchedule, NavLeafCatalog.MaintenanceSchedule),
            Leaf(NavItemIds.DispatcherMaterialRequisition, NavLeafCatalog.MaterialRequisition),
            Leaf(NavItemIds.DispatcherToolRequisition, NavLeafCatalog.ToolRequisition),
            Leaf(NavItemIds.DispatcherRequestHistory, NavLeafCatalog.RequestHistory),
            Leaf(NavItemIds.DispatcherWorkReports, NavLeafCatalog.WorkReports),
            Leaf(NavItemIds.DispatcherPersonnel, NavLeafCatalog.Personnel),
            Group(
                NavMenuTags.RequesterGroup,
                "Nav_Group_Requester",
                Symbol.ContactInfo,
                selectsOnInvoked: false,
                BuildRequesterGroupChildren()),
        };

        return nodes;
    }

    private static IReadOnlyList<NavMenuNode> BuildAdminMenu()
    {
        var nodes = new List<NavMenuNode>
        {
            Leaf(NavItemIds.AdminHome, NavLeafCatalog.Home),
            Leaf(NavItemIds.AdminEquipment, NavLeafCatalog.Equipment),
            Leaf(NavItemIds.AdminUsers, NavLeafCatalog.Users),
            Leaf(NavItemIds.AdminLocations, NavLeafCatalog.Locations),
            Leaf(NavItemIds.AdminRepairDepartments, NavLeafCatalog.RepairDepartments),
            Leaf(NavItemIds.AdminMaintenanceNorms, NavLeafCatalog.MaintenanceNorms),
            Leaf(NavItemIds.AdminAllRequests, NavLeafCatalog.AllRequests),
            Group(
                NavMenuTags.DispatcherGroup,
                "Nav_Group_Dispatcher",
                Symbol.Switch,
                selectsOnInvoked: false,
                BuildDispatcherGroupChildren()),
            Group(
                NavMenuTags.RequesterGroup,
                "Nav_Group_Requester",
                Symbol.ContactInfo,
                selectsOnInvoked: false,
                BuildRequesterGroupChildren()),
        };

        return nodes;
    }

    private static IReadOnlyList<NavMenuNode> BuildDispatcherGroupChildren()
    {
        var children = new List<NavMenuNode>();

        if (IncludeDispatcherHomeInAdminGroup)
        {
            children.Add(Leaf(
                NavItemIds.DispatcherGroupHome,
                NavLeafCatalog.Home,
                NavHomePageKeys.Dispatcher));
        }

        children.Add(Leaf(NavItemIds.DispatcherIncomingRequests, NavLeafCatalog.IncomingRequests));
        children.Add(Leaf(NavItemIds.DispatcherMaintenanceSchedule, NavLeafCatalog.MaintenanceSchedule));
        children.Add(Leaf(NavItemIds.DispatcherMaterialRequisition, NavLeafCatalog.MaterialRequisition));
        children.Add(Leaf(NavItemIds.DispatcherToolRequisition, NavLeafCatalog.ToolRequisition));
        children.Add(Leaf(NavItemIds.DispatcherRequestHistory, NavLeafCatalog.RequestHistory));
        children.Add(Leaf(NavItemIds.DispatcherWorkReports, NavLeafCatalog.WorkReports));
        children.Add(Leaf(NavItemIds.DispatcherPersonnel, NavLeafCatalog.Personnel));

        return children;
    }

    private static IReadOnlyList<NavMenuNode> BuildRequesterGroupChildren()
    {
        var children = new List<NavMenuNode>();

        if (IncludeRequesterHomeInAdminGroup)
        {
            children.Add(Leaf(
                NavItemIds.RequesterGroupHome,
                NavLeafCatalog.Home,
                NavHomePageKeys.Requester));
        }

        children.Add(Leaf(NavItemIds.RequesterNewRequest, NavLeafCatalog.NewRequest));
        children.Add(Leaf(NavItemIds.RequesterMyRequests, NavLeafCatalog.MyRequests));
        children.Add(Leaf(NavItemIds.RequesterAssets, NavLeafCatalog.RequesterAssets));

        return children;
    }

    private static NavMenuLeafNode Leaf(string id, NavLeaf leaf, string? homePageKeyOverride = null) =>
        new(id, leaf, homePageKeyOverride);

    private static NavMenuGroupNode Group(
        string id,
        string titleResourceKey,
        Symbol icon,
        bool selectsOnInvoked,
        IReadOnlyList<NavMenuNode> children) =>
        new(id, titleResourceKey, icon, selectsOnInvoked, children);

    private static Dictionary<string, NavMenuLeafNode> BuildLeafIndex()
    {
        var index = new Dictionary<string, NavMenuLeafNode>();

        foreach (var role in new[] { UserRole.Admin, UserRole.Dispatcher, UserRole.Requester })
        {
            foreach (var leaf in EnumerateLeaves(GetMenuTree(role)))
            {
                index.TryAdd(leaf.Id, leaf);
            }
        }

        return index;
    }

    private static IEnumerable<NavMenuLeafNode> EnumerateLeaves(IEnumerable<NavMenuNode> nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NavMenuLeafNode leaf:
                    yield return leaf;
                    break;
                case NavMenuGroupNode group:
                    foreach (var child in EnumerateLeaves(group.Children))
                    {
                        yield return child;
                    }

                    break;
            }
        }
    }

    private static bool TryFindGroupPath(
        IReadOnlyList<NavMenuNode> nodes,
        string leafId,
        List<string> groupPath)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case NavMenuLeafNode leaf when leaf.Id == leafId:
                    return true;
                case NavMenuGroupNode group:
                    groupPath.Add(group.Id);
                    if (TryFindGroupPath(group.Children, leafId, groupPath))
                    {
                        return true;
                    }

                    groupPath.RemoveAt(groupPath.Count - 1);
                    break;
            }
        }

        return false;
    }
}
