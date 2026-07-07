using BAAZ.CMMS.Core.Models.DocumentExport;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace BAAZ.CMMS.Core.Integrations.Documents.Requests;

internal enum RepairRequestDocxMetadataMode
{
    Printable,
    Full,
}

internal static class RepairRequestDocxLayout
{
    private static readonly int[] HistoryColumnWidthsTwips =
    [
        DocxDocumentBuilder.MmToTwips(28),
        DocxDocumentBuilder.MmToTwips(28),
        DocxDocumentBuilder.MmToTwips(22),
        DocxDocumentBuilder.MmToTwips(22),
        DocxDocumentBuilder.MmToTwips(50),
    ];

    public static void AppendDocument(
        Body body,
        string titleLine1,
        RepairRequestDocumentRequest request,
        RepairRequestDocxMetadataMode mode,
        IReadOnlyList<RequestCardHistoryLine>? history = null,
        IReadOnlyList<RequestCardWorkReportSummary>? workReports = null)
    {
        var documentDate = DocxDocumentBuilder.GetLocalDate(request.GeneratedAt);
        DocxDocumentBuilder.AppendDepartmentApprovalHeader(body, documentDate.Year, request.TargetDepartmentName);
        DocxDocumentBuilder.AppendTitleBlock(body, titleLine1, titleLine2: null, documentDate);
        AppendMetadataTable(body, request, mode);

        if (mode == RepairRequestDocxMetadataMode.Full && workReports is { Count: > 0 })
        {
            DocxDocumentBuilder.AppendParagraph(body, "Сводка отчётов о работах:", bold: true);
            foreach (var report in workReports)
            {
                DocxDocumentBuilder.AppendParagraph(
                    body,
                    $"• {report.CreatedAtText} — {report.DepartmentName}, {report.TechnicianName}");
            }

            DocxDocumentBuilder.AppendSpacer(body);
        }

        if (mode == RepairRequestDocxMetadataMode.Full && history is { Count: > 0 })
        {
            DocxDocumentBuilder.AppendParagraph(body, "История статусов:", bold: true);
            AppendHistoryTable(body, history);
        }

        AppendSignatures(body, request.RequesterName ?? request.AuthorFullName);
    }

    private static void AppendMetadataTable(
        Body body,
        RepairRequestDocumentRequest request,
        RepairRequestDocxMetadataMode mode)
    {
        var table = DocxDocumentBuilder.CreateMetadataTable();
        DocxDocumentBuilder.AppendMetadataRow(table, "Тема", request.Title);
        DocxDocumentBuilder.AppendMetadataRow(
            table,
            "Дата создания",
            DocxDocumentBuilder.FormatDateTime(request.CreatedAt));
        DocxDocumentBuilder.AppendMetadataRow(table, "Заявитель", request.RequesterName ?? "—");
        DocxDocumentBuilder.AppendMetadataRow(table, "Тип", request.TypeLabel);
        DocxDocumentBuilder.AppendMetadataRow(table, "Приоритет", request.PriorityLabel);

        if (mode == RepairRequestDocxMetadataMode.Full)
        {
            DocxDocumentBuilder.AppendMetadataRow(table, "Статус", request.StatusLabel);
            DocxDocumentBuilder.AppendMetadataRow(table, "Зона ремонта", request.RepairZoneLabel);
            DocxDocumentBuilder.AppendMetadataRow(
                table,
                "Дата обновления",
                DocxDocumentBuilder.FormatDateTime(request.UpdatedAt));
        }

        if (!string.IsNullOrWhiteSpace(request.ContractorName))
            DocxDocumentBuilder.AppendMetadataRow(table, "Подрядчик", request.ContractorName);

        if (!string.IsNullOrWhiteSpace(request.AssetDisplay))
            DocxDocumentBuilder.AppendMetadataRow(table, "Оборудование", request.AssetDisplay);

        if (!string.IsNullOrWhiteSpace(request.InventoryDisplay))
            DocxDocumentBuilder.AppendMetadataRow(table, "Инвентарь (ТМС)", request.InventoryDisplay);

        DocxDocumentBuilder.AppendMetadataRow(table, "Местоположение", request.LocationDescription);

        if (mode == RepairRequestDocxMetadataMode.Full && request.Departments.Count > 0)
        {
            DocxDocumentBuilder.AppendMetadataRow(
                table,
                "Отделы и исполнители",
                FormatDepartments(request.Departments));
        }

        DocxDocumentBuilder.AppendMetadataDescriptionRow(
            table,
            "Описание неисправности / работ",
            request.Description);

        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static string FormatDepartments(IReadOnlyList<RepairRequestDepartmentLine> departments) =>
        string.Join(
            Environment.NewLine,
            departments.Select(d =>
            {
                var assignee = string.IsNullOrWhiteSpace(d.AssigneeName) ? "не назначен" : d.AssigneeName;
                return $"• {d.DepartmentName} — {assignee}";
            }));

    private static void AppendHistoryTable(Body body, IReadOnlyList<RequestCardHistoryLine> history)
    {
        var headers = new[] { "Дата и время", "Пользователь", "Было", "Стало", "Комментарий" };
        var table = DocxDocumentBuilder.CreateBorderedTable(HistoryColumnWidthsTwips);
        DocxDocumentBuilder.AppendHeaderRow(table, headers);

        foreach (var line in history)
        {
            var row = new TableRow();
            row.Append(DocxDocumentBuilder.CreateTableCell(line.ChangedAtText, fontSizeHalfPoints: 18));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.ChangedByName, fontSizeHalfPoints: 18));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.OldStatusLabel, fontSizeHalfPoints: 18));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.NewStatusLabel, fontSizeHalfPoints: 18));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.Comment ?? string.Empty, fontSizeHalfPoints: 18));
            table.Append(row);
        }

        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static void AppendSignatures(Body body, string requesterName)
    {
        var table = DocxDocumentBuilder.CreateSignatureTable();
        DocxDocumentBuilder.AppendSignatureRow(table, "Заявитель:", requesterName);
        DocxDocumentBuilder.AppendSignatureRow(table, "Принял (диспетчер):", string.Empty);
        DocxDocumentBuilder.AppendSignatureRow(table, "Исполнитель:", string.Empty);
        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }
}
