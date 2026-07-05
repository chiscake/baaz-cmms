using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.App.Helpers;

public static class ToolRequisitionLabels
{
    public static string FormatTmsStatus(string status) => status switch
    {
        TmsRequisitionStatuses.New => ResourceStrings.Get("ToolRequisition_TmsStatus_New"),
        TmsRequisitionStatuses.PartiallyReserved => ResourceStrings.Get("ToolRequisition_TmsStatus_PartiallyReserved"),
        TmsRequisitionStatuses.ReadyForIssue => ResourceStrings.Get("ToolRequisition_TmsStatus_ReadyForIssue"),
        TmsRequisitionStatuses.Issued => ResourceStrings.Get("ToolRequisition_TmsStatus_Issued"),
        TmsRequisitionStatuses.PartiallyReturned => ResourceStrings.Get("ToolRequisition_TmsStatus_PartiallyReturned"),
        TmsRequisitionStatuses.Returned => ResourceStrings.Get("ToolRequisition_TmsStatus_Returned"),
        TmsRequisitionStatuses.Cancelled => ResourceStrings.Get("ToolRequisition_TmsStatus_Cancelled"),
        _ => status,
    };
}
