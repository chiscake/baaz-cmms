using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models.DocumentExport;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BAAZ.CMMS.Core.Integrations.Documents.WorkReports;

public interface IWorkReportDocxGenerator
{
    void Generate(WorkReportDocumentRequest request, string targetFilePath);
}

public sealed class WorkReportDocxGenerator : IWorkReportDocxGenerator
{
    public void Generate(WorkReportDocumentRequest request, string targetFilePath)
    {
        using var document = DocxDocumentBuilder.CreateDocument(targetFilePath, out var body);

        var documentDate = DocxDocumentBuilder.GetLocalDate(request.CreatedAt);
        var reportNumber = request.ReportId.ToString("N")[..8].ToUpperInvariant();

        DocxDocumentBuilder.AppendDepartmentApprovalHeader(body, documentDate.Year, request.RepairDepartmentName);
        DocxDocumentBuilder.AppendTitleBlock(
            body,
            $"АКТ выполненных работ № {reportNumber}",
            null,
            documentDate);

        AppendInfoBlock(body, request);
        AppendBodyBlock(body, request);
        AppendSignatures(body, request.AuthorName, request.TechnicianName);
        DocxDocumentBuilder.AppendFooter(body, request.ReportId);
        DocxDocumentBuilder.SaveAndDispose(document, body);
    }

    private static void AppendInfoBlock(Body body, WorkReportDocumentRequest request)
    {
        if (request.IsRequestSource)
        {
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Заявка на ремонт №:", request.RequestNumber ?? "—");
            if (!string.IsNullOrWhiteSpace(request.RequestTitle))
                DocxDocumentBuilder.AppendLabelValueParagraph(body, "Тема:", request.RequestTitle);
        }
        else
        {
            DocxDocumentBuilder.AppendLabelValueParagraph(
                body,
                "Позиция графика ППР:",
                $"{request.ScheduleMaintenanceType}, план {DocxDocumentBuilder.FormatDate(request.SchedulePlannedDate)}");
        }

        if (!string.IsNullOrWhiteSpace(request.AssetName))
        {
            DocxDocumentBuilder.AppendLabelValueParagraph(
                body,
                "Оборудование:",
                $"{request.AssetName} (инв. № {request.AssetNumber ?? "—"})");
        }

        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Ремонтный отдел:", request.RepairDepartmentName);
        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Исполнитель:", request.TechnicianName);
        DocxDocumentBuilder.AppendLabelValueParagraph(body, "Диспетчер (автор отчёта):", request.AuthorName);
        DocxDocumentBuilder.AppendLabelValueParagraph(
            body,
            "Дата регистрации:",
            DocxDocumentBuilder.FormatDateTime(request.CreatedAt));
        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static void AppendBodyBlock(Body body, WorkReportDocumentRequest request)
    {
        DocxDocumentBuilder.AppendParagraph(body, "Выполненные работы:", bold: true);
        DocxDocumentBuilder.AppendParagraph(body, request.WorkPerformed);
        DocxDocumentBuilder.AppendSpacer(body);

        DocxDocumentBuilder.AppendLabelValueParagraph(
            body,
            "Фактическое время:",
            $"{DocxDocumentBuilder.FormatQuantity(request.ActualDurationHours)} ч");

        if (!string.IsNullOrWhiteSpace(request.MaintenanceTypesText))
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Виды ТО:", request.MaintenanceTypesText);

        if (!string.IsNullOrWhiteSpace(request.DefectsFound))
        {
            DocxDocumentBuilder.AppendParagraph(body, "Обнаруженные дефекты:", bold: true);
            DocxDocumentBuilder.AppendParagraph(body, request.DefectsFound);
        }

        if (!string.IsNullOrWhiteSpace(request.PartsUsed))
        {
            DocxDocumentBuilder.AppendParagraph(body, "Использованные материалы:", bold: true);
            DocxDocumentBuilder.AppendParagraph(body, request.PartsUsed);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Примечание:", request.Notes);

        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static void AppendSignatures(Body body, string authorName, string technicianName)
    {
        var table = DocxDocumentBuilder.CreateSignatureTable();
        DocxDocumentBuilder.AppendSignatureRow(table, "Диспетчер:", authorName);
        DocxDocumentBuilder.AppendSignatureRow(table, "Исполнитель:", technicianName);
        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }
}
