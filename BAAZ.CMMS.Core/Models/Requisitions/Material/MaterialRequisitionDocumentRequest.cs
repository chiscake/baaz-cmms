namespace BAAZ.CMMS.Core.Models;

public sealed class MaterialRequisitionDocumentRequest
{
    public required MaterialRequisitionInput Input { get; init; }

    public required MaterialRequisitionDocumentContext Context { get; init; }

    public required string AuthorFullName { get; init; }

    public required string TargetFilePath { get; init; }
}
