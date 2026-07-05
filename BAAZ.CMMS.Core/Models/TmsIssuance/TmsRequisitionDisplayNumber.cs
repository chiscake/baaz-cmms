namespace BAAZ.CMMS.Core.Models.TmsIssuance;

public static class TmsRequisitionDisplayNumber
{
    public static string Format(Guid requisitionId)
    {
        var seq = (BitConverter.ToUInt16(requisitionId.ToByteArray(), 0) % 999) + 1;
        return $"TMS-{DateTime.Today:yyyyMMdd}-{seq:D3}";
    }
}
