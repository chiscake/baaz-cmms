using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;

namespace BAAZ.CMMS.App.Pages.Dispatcher.PersonnelManagement;

public sealed partial class PersonnelRow : ObservableObject, ICrudGridRow
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    public required string FullName { get; init; }

    public required string Specialty { get; init; }

    public Guid? RepairDepartmentId { get; init; }

    public string? RepairDepartmentName { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Метка активности для отображения в таблице.</summary>
    public string ActiveLabel => CrudBoolCellHelper.Format(IsActive);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "Id" => Id.ToString(),
        "FullName" => FullName,
        "Specialty" => Specialty,
        "Department" => RepairDepartmentName,
        "Active" => ActiveLabel,
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        _ => null,
    };

    public string? GetCellEditValue(string columnKey) => columnKey switch
    {
        "Department" => RepairDepartmentId?.ToString(),
        _ => GetCellText(columnKey),
    };
}
