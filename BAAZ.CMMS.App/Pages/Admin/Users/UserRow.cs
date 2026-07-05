using System;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Helpers.LocationHelpers;

namespace BAAZ.CMMS.App.Pages.Admin.Users;

public sealed partial class UserRow : ObservableObject, ICrudGridRow, ICrudSelectableRow
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }

    public string RoleDisplay { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public Guid? LocationId { get; init; }

    public string? LocationName { get; init; }

    public IReadOnlyList<Guid> LocationScopeIds { get; init; } = [];

    public string? LocationScopeSummary { get; init; }

    public Guid? RepairDepartmentId { get; init; }

    public string? RepairDepartmentName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public bool IsBanned { get; init; }

    public bool IsAdminAccount { get; init; }

    public bool IsCurrentUser { get; init; }

    /// <summary>Для CrudWorkbench: активен = не заблокирован.</summary>
    public bool IsActive => !IsBanned;

    public bool IsSelectable => !IsAdminAccount && !IsCurrentUser;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string BannedLabel => CrudBoolCellHelper.Format(IsBanned);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "Id" => Id.ToString(),
        "Email" => Email,
        "FullName" => FullName,
        "Role" => RoleDisplay,
        "Phone" => Phone,
        "Location" => LocationName,
        "LocationScopes" => LocationScopeSummary,
        "Department" => RepairDepartmentName,
        "Banned" => BannedLabel,
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        _ => null,
    };

    public string? GetCellEditValue(string columnKey) => columnKey switch
    {
        "Location" => LocationId?.ToString(),
        "LocationScopes" => LocationScopeIds.Count > 0
            ? LocationScopeIdsWireFormat.Serialize(LocationScopeIds)
            : null,
        "Department" => RepairDepartmentId?.ToString(),
        _ => GetCellText(columnKey),
    };
}
