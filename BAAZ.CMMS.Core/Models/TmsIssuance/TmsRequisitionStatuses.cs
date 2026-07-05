namespace BAAZ.CMMS.Core.Models.TmsIssuance;

/// <summary>Статусы заявки TMS (производные из line_status + cancelled). См. docs/use-cases/tms-tool-issuance-proposal.md.</summary>
public static class TmsRequisitionStatuses
{
    public const string New = "new";
    public const string PartiallyReserved = "partially_reserved";
    public const string ReadyForIssue = "ready_for_issue";
    public const string Issued = "issued";
    public const string PartiallyReturned = "partially_returned";
    public const string Returned = "returned";
    public const string Cancelled = "cancelled";

    /// <summary>Статусы, при которых повторная отправка для того же наряда и склада запрещена.</summary>
    public static bool BlocksDuplicateSubmission(string? status)
        => !string.IsNullOrWhiteSpace(status)
           && !string.Equals(status, Cancelled, StringComparison.Ordinal)
           && !string.Equals(status, Returned, StringComparison.Ordinal);
}

public static class TmsLineStatuses
{
    public const string Pending = "pending";
    public const string Reserved = "reserved";
    public const string Issued = "issued";
    public const string Returned = "returned";
}
