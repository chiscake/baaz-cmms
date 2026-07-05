using System;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.App.Pages.Dispatcher.ToolRequisitionHistory;

public sealed class ToolRequisitionHistoryRow : ICrudGridRow
{
    public required Guid Id { get; init; }

    public required string RequisitionNumber { get; init; }

    public required string WarehouseName { get; init; }

    public required string Status { get; init; }

    public required string WorkOrderKind { get; init; }

    public required string WorkOrderRef { get; init; }

    public required DateTimeOffset? CreatedAt { get; init; }

    public required DateTimeOffset? UpdatedAt { get; init; }

    public bool IsActive => true;

    public bool IsSelected { get; set; }

    public string GetCellText(string columnKey) => columnKey switch
    {
        "RequisitionNumber" => RequisitionNumber,
        "WarehouseName" => WarehouseName,
        "Status" => TmsRequisitionStatusHelper.GetLabel(Status),
        "WorkOrderKind" => FormatWorkOrderKind(WorkOrderKind),
        "WorkOrderRef" => WorkOrderRef,
        "CreatedAt" => DateTimeDisplayHelper.Format(CreatedAt),
        "UpdatedAt" => DateTimeDisplayHelper.Format(UpdatedAt),
        "Id" => Id.ToString(),
        _ => string.Empty,
    };

    public static ToolRequisitionHistoryRow FromLink(
        TmsToolRequisitionLinkModel link,
        string workOrderRef)
        => new()
        {
            Id = link.Id,
            RequisitionNumber = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId),
            WarehouseName = string.IsNullOrWhiteSpace(link.WarehouseName)
                ? ResourceStrings.Get("ToolRequisitionHistory_UnknownWarehouse")
                : link.WarehouseName,
            Status = link.LastKnownStatus,
            WorkOrderKind = link.WorkOrderKind,
            WorkOrderRef = workOrderRef,
            CreatedAt = link.CreatedAt,
            UpdatedAt = link.UpdatedAt,
        };

    private static string FormatWorkOrderKind(string kind)
        => kind switch
        {
            "request" => ResourceStrings.Get("ToolRequisition_Kind_Request"),
            "schedule" => ResourceStrings.Get("ToolRequisition_Kind_Schedule"),
            _ => kind,
        };
}
