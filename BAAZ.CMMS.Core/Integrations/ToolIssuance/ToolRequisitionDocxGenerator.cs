using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BAAZ.CMMS.Core.Integrations.ToolIssuance;

public sealed class ToolRequisitionDocxGenerator : IToolRequisitionDocxGenerator
{
    private static readonly int[] LineTableColumnWidthsTwips =
    [
        DocxDocumentBuilder.MmToTwips(8),
        DocxDocumentBuilder.MmToTwips(28),
        DocxDocumentBuilder.MmToTwips(62),
        DocxDocumentBuilder.MmToTwips(14),
        DocxDocumentBuilder.MmToTwips(18),
        DocxDocumentBuilder.MmToTwips(30),
    ];

    public void Generate(ToolRequisitionDocumentRequest request, string targetFilePath)
    {
        using var document = DocxDocumentBuilder.CreateDocument(targetFilePath, out var body);

        var today = DateTime.Now.Date;
        var ctx = request.Context;
        var input = request.Input;

        DocxDocumentBuilder.AppendDepartmentApprovalHeader(body, today.Year, ctx.RepairDepartmentName);
        DocxDocumentBuilder.AppendTitleBlock(
            body,
            $"ЗАЯВКА № {ctx.RequisitionNumber}",
            "на выдачу инструмента со склада",
            today);
        AppendInfoBlock(body, request);
        AppendLinesTable(body, input.Lines);
        AppendSignatures(body, request.AuthorFullName, ctx.TechnicianFullName);
        DocxDocumentBuilder.AppendFooter(body, ctx.RequisitionId);
        DocxDocumentBuilder.SaveAndDispose(document, body);
    }

    private static void AppendInfoBlock(Body body, ToolRequisitionDocumentRequest request)
    {
        var ctx = request.Context;
        var input = request.Input;

        var basis = ctx.IsRequestWorkOrder
            ? $"Заявка на ремонт № {ctx.RequestNumber} (Объект: {ctx.AssetName}, Инв. № {ctx.AssetNumber})"
            : $"Позиция графика ППР ({ctx.MaintenanceTypeLabel}, план {DocxDocumentBuilder.FormatDate(ctx.PlannedDate)}, оборудование {ctx.AssetName}, инв. № {ctx.AssetNumber})";

        var rows = new (string Label, string Value)[]
        {
            ("Склад выдачи:", input.WarehouseName),
            ("Цех/Отдел-получатель:", ctx.RepairDepartmentName),
            ("Основание для выдачи:", basis),
            ("Исполнитель:", ctx.TechnicianFullName),
        };

        foreach (var (label, value) in rows)
            DocxDocumentBuilder.AppendLabelValueParagraph(body, label, value);

        if (!string.IsNullOrWhiteSpace(input.Notes))
            DocxDocumentBuilder.AppendLabelValueParagraph(body, "Примечание:", input.Notes);

        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static void AppendLinesTable(Body body, IReadOnlyList<ToolRequisitionLine> lines)
    {
        var headers = new[]
        {
            "№ п/п", "Инв. № / код", "Наименование инструмента",
            "Затребовано", "Выдано", "Примечание",
        };

        var table = DocxDocumentBuilder.CreateBorderedTable(LineTableColumnWidthsTwips);
        DocxDocumentBuilder.AppendHeaderRow(table, headers);

        var index = 1;
        foreach (var line in lines)
        {
            var row = new TableRow();
            row.Append(DocxDocumentBuilder.CreateTableCell(index.ToString(DocxDocumentBuilder.RuCulture), fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.InventoryNumber ?? string.Empty, fontSizeHalfPoints: 20));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.Name, fontSizeHalfPoints: 20));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.Quantity.ToString(DocxDocumentBuilder.RuCulture), fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(DocxDocumentBuilder.CreateTableCell(string.Empty, fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(DocxDocumentBuilder.CreateTableCell(line.LineNote ?? string.Empty, fontSizeHalfPoints: 20));
            table.Append(row);
            index++;
        }

        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }

    private static void AppendSignatures(Body body, string authorFullName, string technicianFullName)
    {
        var table = DocxDocumentBuilder.CreateSignatureTable();
        DocxDocumentBuilder.AppendSignatureRow(table, "Затребовал (Диспетчер/Мастер):", authorFullName);
        DocxDocumentBuilder.AppendSignatureRow(table, "Разрешил (Ответственное лицо):", string.Empty);
        DocxDocumentBuilder.AppendSignatureRow(table, "Выдал (Кладовщик):", string.Empty);
        DocxDocumentBuilder.AppendSignatureRow(table, "Получил (Исполнитель):", technicianFullName);
        body.Append(table);
        DocxDocumentBuilder.AppendSpacer(body);
    }
}
