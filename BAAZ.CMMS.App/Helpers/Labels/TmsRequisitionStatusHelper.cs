namespace BAAZ.CMMS.App.Helpers;

using BAAZ.CMMS.Core.Models.TmsIssuance;

public static class TmsRequisitionStatusHelper
{
    public static string GetLabel(string status) => ToolRequisitionLabels.FormatTmsStatus(status);

    public static string GetBadgeBackgroundKey(string status)
        => StatusBadgeFactory.ForTmsRequisition(status).BackgroundKey;

    public static string GetBadgeForegroundKey(string status)
        => StatusBadgeFactory.ForTmsRequisition(status).ForegroundKey;
}
