using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;

namespace BAAZ.CMMS.App.Pages.Admin.RepairDepartments;

public sealed partial class RepairDepartmentRow : ObservableObject, ICrudGridRow
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    public required string Name { get; init; }

    public string? Code { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public string ActiveLabel => CrudBoolCellHelper.Format(IsActive);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "Id" => Id.ToString(),
        "Name" => Name,
        "Code" => Code,
        "Active" => ActiveLabel,
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        _ => null,
    };
}
