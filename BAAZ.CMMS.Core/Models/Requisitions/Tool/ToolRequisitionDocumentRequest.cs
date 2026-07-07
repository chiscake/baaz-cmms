namespace BAAZ.CMMS.Core.Models;

public sealed class ToolRequisitionDocumentRequest
{
    public required ToolRequisitionFormInput Input { get; init; }

    public required ToolRequisitionDocumentContext Context { get; init; }

    public required string AuthorFullName { get; init; }

    public required string TargetFilePath { get; init; }
}
