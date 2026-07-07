using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models.DocumentExport;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BAAZ.CMMS.Core.Integrations.Documents.Requests;

public interface IRepairRequestDocxGenerator
{
    void Generate(RepairRequestDocumentRequest request, string targetFilePath);
}

public sealed class RepairRequestDocxGenerator : IRepairRequestDocxGenerator
{
    public void Generate(RepairRequestDocumentRequest request, string targetFilePath)
    {
        using var document = DocxDocumentBuilder.CreateDocument(targetFilePath, out var body);

        RepairRequestDocxLayout.AppendDocument(
            body,
            $"ЗАЯВКА НА РЕМОНТ № {request.RequestNumber}",
            request,
            RepairRequestDocxMetadataMode.Printable);

        DocxDocumentBuilder.AppendFooter(body, request.RequestId);
        DocxDocumentBuilder.SaveAndDispose(document, body);
    }
}
