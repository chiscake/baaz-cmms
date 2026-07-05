using System;

using CommunityToolkit.Mvvm.ComponentModel;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.App.Pages.Admin.AllRequests;

public sealed partial class AdminRequestRow : ObservableObject, ICrudGridRow
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; } = true;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public required string RequestNumber { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Type { get; init; }

    public string LocationDescription { get; init; } = string.Empty;

    public string? AssetNumber { get; init; }

    public string? AssetName { get; init; }

    public string RepairZone { get; init; } = string.Empty;

    public string? ContractorName { get; init; }

    public string? TargetDepartmentName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public string? AssigneeName { get; init; }

    public string? RequesterName { get; init; }

    public string StatusLabel => RequestStatusHelper.GetLabel(Status);

    public string AssetDisplay => FormatAsset(AssetNumber, AssetName);

    public string? GetCellText(string columnKey) => columnKey switch
    {
        "RequestNumber" => RequestNumber,
        "Title" => Title,
        "Description" => Description,
        "Status" => StatusLabel,
        "Priority" => RequestEnumLabels.Priority(Priority),
        "Type" => RequestEnumLabels.Type(Type),
        "Requester" => RequesterName ?? string.Empty,
        "Assignee" => AssigneeName ?? string.Empty,
        "Location" => LocationDescription,
        "Asset" => AssetDisplay,
        "RepairZone" => RequestEnumLabels.RepairZone(RepairZone),
        "ContractorName" => ContractorName ?? string.Empty,
        "TargetDepartment" => TargetDepartmentName ?? string.Empty,
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        "Id" => Id.ToString(),
        _ => null,
    };

    public string? GetCellEditValue(string columnKey) => columnKey switch
    {
        "Priority" => Priority,
        "Type" => Type,
        _ => GetCellText(columnKey),
    };

    public static AdminRequestRow FromListItem(RequestListItem item) => new()
    {
        Id = item.Id,
        IsActive = true,
        RequestNumber = item.RequestNumber,
        Title = item.Title,
        Description = item.Description,
        Status = item.Status,
        Priority = item.Priority,
        Type = item.Type,
        LocationDescription = item.LocationDescription,
        AssetNumber = item.AssetNumber,
        AssetName = item.AssetName,
        RepairZone = item.RepairZone,
        ContractorName = item.ContractorName,
        TargetDepartmentName = item.TargetDepartmentName,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        AssigneeName = item.AssigneeName,
        RequesterName = item.RequesterName,
    };

    private static string FormatAsset(string? number, string? name)
    {
        if (string.IsNullOrWhiteSpace(number) && string.IsNullOrWhiteSpace(name))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(number) && !string.IsNullOrWhiteSpace(name))
            return $"{number} — {name}";

        return number ?? name ?? string.Empty;
    }
}
