using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Pages.Admin.AssetRegistry;

public sealed partial class AssetRow : ObservableObject, ICrudGridRow
{
    public required Guid Id { get; init; }

    public bool IsActive => !string.Equals(Status, "decommissioned", StringComparison.Ordinal);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
    public required string AssetNumber { get; init; }

    public required string Name { get; init; }

    public required Guid LocationId { get; init; }

    public string? LocationName { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public required string Status { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public string? SerialNumber { get; init; }

    public DateOnly? CommissioningDate { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public string StatusLabel => FormatStatus(Status);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "Id" => Id.ToString(),
        "AssetNumber" => AssetNumber,
        "Name" => Name,
        "Location" => LocationName ?? "—",
        "Category" => CategoryName ?? ResourceStrings.Get("Assets_Category_None"),
        "Manufacturer" => Manufacturer ?? "—",
        "Model" => Model ?? "—",
        "SerialNumber" => SerialNumber ?? "—",
        "CommissioningDate" => DateDisplayHelper.Format(CommissioningDate),
        "Status" => StatusLabel,
        "Description" => Description ?? "—",
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        _ => null,
    };

    public string? GetCellEditValue(string columnKey) => columnKey switch
    {
        "Location" => LocationId.ToString(),
        "Category" => CategoryId?.ToString() ?? string.Empty,
        "CommissioningDate" => DateDisplayHelper.ToWireFormat(CommissioningDate),
        _ => GetCellText(columnKey),
    };

    private static string FormatStatus(string status) => status switch
    {
        "active" => ResourceStrings.Get("AssetStatus_Active"),
        "maintenance" => ResourceStrings.Get("AssetStatus_Maintenance"),
        "decommissioned" => ResourceStrings.Get("AssetStatus_Decommissioned"),
        _ => status,
    };
}
