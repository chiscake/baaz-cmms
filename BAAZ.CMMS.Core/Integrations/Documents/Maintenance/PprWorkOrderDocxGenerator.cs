using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models.DocumentExport;

namespace BAAZ.CMMS.Core.Integrations.Documents.Maintenance;

public interface IPprWorkOrderDocxGenerator
{
    void Generate(PprWorkOrderDocumentRequest request, string targetFilePath);
}

public sealed class PprWorkOrderDocxGenerator : IPprWorkOrderDocxGenerator
{
    public void Generate(PprWorkOrderDocumentRequest request, string targetFilePath)
    {
        using var document = DocxDocumentBuilder.CreateDocument(targetFilePath, out var body);

        var documentDate = DocxDocumentBuilder.GetLocalDate(request.GeneratedAt);
        DocxDocumentBuilder.AppendDepartmentApprovalHeader(body, documentDate.Year, request.DepartmentNames);
        DocxDocumentBuilder.AppendTitleBlock(
            body,
            "НАРЯД на проведение технического обслуживания",
            $"{request.MaintenanceTypeLabel} — {request.AssetName}",
            documentDate);

        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Инвентарный №:", request.AssetNumber);
        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Плановая дата:", DocxDocumentBuilder.FormatDate(request.PlannedDate));
        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Статус:", request.StatusLabel);
        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Ответственные отделы:", request.DepartmentNames);

        if (request.LastMaintenanceDate is not null)
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Последнее ТО:", DocxDocumentBuilder.FormatDate(request.LastMaintenanceDate));

        if (request.NextMaintenanceDate is not null)
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Следующее ТО:", DocxDocumentBuilder.FormatDate(request.NextMaintenanceDate));

        DocxDocumentBuilder.AppendSpacer(body);
        DocxDocumentBuilder.AppendParagraph(body, "Перечень работ (норматив):", bold: true);
        DocxDocumentBuilder.AppendParagraph(body, string.IsNullOrWhiteSpace(request.WorkDescription) ? "—" : request.WorkDescription);
        DocxDocumentBuilder.AppendSpacer(body);

        var table = DocxDocumentBuilder.CreateSignatureTable();
        DocxDocumentBuilder.AppendSignatureRow(table, "Выдал (диспетчер):", request.AuthorFullName);
        DocxDocumentBuilder.AppendSignatureRow(table, "Принял (мастер):", string.Empty);
        DocxDocumentBuilder.AppendSignatureRow(table, "Исполнитель:", string.Empty);
        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);

        DocxDocumentBuilder.AppendFooter(body, request.ScheduleId);
        DocxDocumentBuilder.SaveAndDispose(document, body);
    }
}
