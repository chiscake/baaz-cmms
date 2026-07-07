using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models.DocumentExport;

namespace BAAZ.CMMS.Core.Integrations.Documents.Requests;

public interface IRequestCardDocxGenerator
{
    void Generate(RequestCardDocumentRequest request, string targetFilePath);
}

public sealed class RequestCardDocxGenerator : IRequestCardDocxGenerator
{
    public void Generate(RequestCardDocumentRequest request, string targetFilePath)
    {
        using var document = DocxDocumentBuilder.CreateDocument(targetFilePath, out var body);

        RepairRequestDocxLayout.AppendDocument(
            body,
            $"КАРТОЧКА ЗАЯВКИ № {request.Request.RequestNumber}",
            request.Request,
            RepairRequestDocxMetadataMode.Full,
            request.History,
            request.WorkReports);

        DocxDocumentBuilder.AppendFooter(body, request.Request.RequestId);
        DocxDocumentBuilder.SaveAndDispose(document, body);
    }
}
