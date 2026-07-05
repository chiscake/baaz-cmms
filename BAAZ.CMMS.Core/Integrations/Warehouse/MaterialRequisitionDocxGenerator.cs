using System.Globalization;
using System.Linq;
using BAAZ.CMMS.Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BAAZ.CMMS.Core.Integrations.Warehouse;

public sealed class MaterialRequisitionDocxGenerator : IMaterialRequisitionDocxGenerator
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly int PageWidthTwips = MmToTwips(210);
    private static readonly int PageHeightTwips = MmToTwips(297);

    // A4 content width ≈ 210mm − 20mm left − 10mm right
    private static readonly int[] LineTableColumnWidthsTwips =
    [
        MmToTwips(8),   // № п/п
        MmToTwips(32),  // SKU
        MmToTwips(58),  // Наименование
        MmToTwips(14),  // Ед. изм.
        MmToTwips(18),  // Затребовано
        MmToTwips(18),  // Выдано
        MmToTwips(32),  // Примечание
    ];

    private const int CellPaddingTwips = 100; // ~5 pt top/bottom
    private static readonly int SignatureLineWidthTwips = MmToTwips(35);

    public void Generate(MaterialRequisitionDocumentRequest request, string targetFilePath)
    {
        using var document = WordprocessingDocument.Create(targetFilePath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        var today = DateTime.Today;
        var ctx = request.Context;
        var input = request.Input;

        AppendApprovalHeader(body, today.Year);
        AppendTitle(body, ctx.RequisitionNumber, today);
        AppendInfoBlock(body, request);
        AppendLinesTable(body, input.Lines);
        AppendSignatures(body, request.AuthorFullName, ctx.TechnicianFullName);
        AppendFooter(body, ctx.RequisitionId);
        ApplySectionProperties(body);

        mainPart.Document.Save();
    }

    private static void ApplySectionProperties(Body body)
    {
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            paragraph.Elements<SectionProperties>().FirstOrDefault()?.Remove();
        }

        body.Elements<SectionProperties>().FirstOrDefault()?.Remove();

        body.Append(CreateSectionProperties());
    }

    private static SectionProperties CreateSectionProperties() =>
        new(
            new PageSize
            {
                Width = (UInt32Value)(uint)PageWidthTwips,
                Height = (UInt32Value)(uint)PageHeightTwips,
                Orient = PageOrientationValues.Portrait,
            },
            new PageMargin
            {
                Top = MmToTwips(20),
                Bottom = MmToTwips(20),
                Left = (UInt32Value)(uint)MmToTwips(20),
                Right = (UInt32Value)(uint)MmToTwips(10),
                Gutter = (UInt32Value)0U,
            });

    private static void AppendApprovalHeader(Body body, int year)
    {
        AppendParagraph(body, "УТВЕРЖДАЮ", bold: true, alignment: JustificationValues.Right);
        AppendParagraph(body, "Начальник ОМТО / Главный механик", alignment: JustificationValues.Right);
        AppendParagraph(body, "ОАО «БААЗ»", alignment: JustificationValues.Right);
        AppendParagraph(body, "___________ / __________________", alignment: JustificationValues.Right);
        AppendParagraph(body, $"«___» ____________ {year} г.", alignment: JustificationValues.Right);
        AppendSpacer(body);
    }

    private static void AppendTitle(Body body, string requisitionNumber, DateTime documentDate)
    {
        var monthName = RuCulture.DateTimeFormat.GetMonthName(documentDate.Month);
        AppendParagraph(body, $"ЗАЯВКА-ТРЕБОВАНИЕ № {requisitionNumber}", bold: true, fontSizeHalfPoints: 28,
            alignment: JustificationValues.Center);
        AppendParagraph(body, "на отпуск материалов (расходников) со склада", bold: true, fontSizeHalfPoints: 28,
            alignment: JustificationValues.Center);
        AppendParagraph(body,
            $"от «{documentDate.Day:00}» {monthName} {documentDate.Year} г.",
            alignment: JustificationValues.Center);
        AppendSpacer(body);
    }

    private static void AppendInfoBlock(Body body, MaterialRequisitionDocumentRequest request)
    {
        var ctx = request.Context;
        var input = request.Input;

        var basis = ctx.IsRequestWorkOrder
            ? $"Заявка на ремонт № {ctx.RequestNumber} (Объект: {ctx.AssetName}, Инв. № {ctx.AssetNumber})"
            : $"Позиция графика ППР ({ctx.MaintenanceTypeLabel}, план {FormatDate(ctx.PlannedDate)}, оборудование {ctx.AssetName}, инв. № {ctx.AssetNumber})";

        var rows = new (string Label, string Value)[]
        {
            ("Склад-отправитель:", input.WarehouseName),
            ("Цех/Отдел-получатель:", ctx.RepairDepartmentName),
            ("Основание для выдачи:", basis),
            ("Исполнитель:", ctx.TechnicianFullName),
        };

        foreach (var (label, value) in rows)
            AppendLabelValueParagraph(body, label, value);

        if (!string.IsNullOrWhiteSpace(input.Notes))
            AppendLabelValueParagraph(body, "Примечание:", input.Notes);

        AppendSpacer(body);
    }

    private static void AppendLinesTable(Body body, IReadOnlyList<MaterialRequisitionLine> lines)
    {
        var headers = new[]
        {
            "№ п/п", "Номенклатурный номер (SKU)", "Наименование материала / запчасти",
            "Ед. изм.", "Затребовано (Кол-во)", "Выдано (Кол-во)", "Примечание (Серийный номер)",
        };

        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Fixed },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        var grid = new TableGrid();
        foreach (var width in LineTableColumnWidthsTwips)
            grid.Append(new GridColumn { Width = width.ToString(RuCulture) });

        table.Append(grid);

        var headerRow = new TableRow();
        foreach (var header in headers)
        {
            headerRow.Append(CreateTableCell(
                header,
                bold: true,
                shaded: true,
                fontSizeHalfPoints: 20,
                alignment: JustificationValues.Center));
        }

        table.Append(headerRow);

        var index = 1;
        foreach (var line in lines)
        {
            var row = new TableRow();
            row.Append(CreateTableCell(index.ToString(RuCulture), fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(CreateTableCell(line.Sku ?? string.Empty, fontSizeHalfPoints: 20));
            row.Append(CreateTableCell(line.Name, fontSizeHalfPoints: 20));
            row.Append(CreateTableCell(line.Unit, fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(CreateTableCell(FormatQuantity(line.Quantity), fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(CreateTableCell(string.Empty, fontSizeHalfPoints: 20, alignment: JustificationValues.Center));
            row.Append(CreateTableCell(line.LineNote ?? string.Empty, fontSizeHalfPoints: 20));
            table.Append(row);
            index++;
        }

        body.Append(table);
        AppendSpacer(body);
    }

    private static void AppendSignatures(Body body, string authorFullName, string technicianFullName)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Fixed },
            CreateBorderlessTableBorders()));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = MmToTwips(70).ToString(RuCulture) });
        grid.Append(new GridColumn { Width = SignatureLineWidthTwips.ToString(RuCulture) });
        grid.Append(new GridColumn { Width = MmToTwips(55).ToString(RuCulture) });
        table.Append(grid);

        AppendSignatureRow(table, "Затребовал (Диспетчер/Мастер):", authorFullName);
        AppendSignatureRow(table, "Разрешил (Ответственное лицо):", string.Empty);
        AppendSignatureRow(table, "Выдал (Кладовщик склада):", string.Empty);
        AppendSignatureRow(table, "Получил (Исполнитель/Техник):", technicianFullName);

        body.Append(table);
        AppendSpacer(body);
    }

    private static void AppendSignatureRow(Table table, string role, string name)
    {
        var row = new TableRow();
        row.Append(CreateBorderlessTableCell(role, bold: false));
        row.Append(CreateBorderlessTableCell("___________", alignment: JustificationValues.Center));
        row.Append(CreateBorderlessTableCell(string.IsNullOrWhiteSpace(name) ? string.Empty : name));
        table.Append(row);
    }

    private static void AppendFooter(Body body, Guid requisitionId)
    {
        AppendParagraph(body, string.Empty);
        AppendParagraph(body, string.Empty);
        AppendParagraph(body,
            $"ID: {requisitionId} | Сгенерировано в BAAZ.CMMS",
            fontSizeHalfPoints: 16,
            colorHex: "666666");
    }

    private static TableCell CreateTableCell(
        string text,
        bool bold = false,
        bool shaded = false,
        int fontSizeHalfPoints = 24,
        JustificationValues? alignment = null)
    {
        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            new TableCellMargin(
                new TopMargin { Width = CellPaddingTwips.ToString(RuCulture), Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = CellPaddingTwips.ToString(RuCulture), Type = TableWidthUnitValues.Dxa },
                new LeftMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "80", Type = TableWidthUnitValues.Dxa }));

        if (shaded)
        {
            cellProps.Append(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Fill = "D9D9D9",
                Color = "auto",
            });
        }

        var cell = new TableCell(cellProps);

        var paragraphProps = new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

        if (alignment is not null)
            paragraphProps.Append(new Justification { Val = alignment });

        var paragraph = new Paragraph(paragraphProps);
        paragraph.Append(CreateRun(text, bold, fontSizeHalfPoints));
        cell.Append(paragraph);
        return cell;
    }

    private static TableCell CreateBorderlessTableCell(string text, bool bold = false, JustificationValues? alignment = null)
    {
        var cell = new TableCell(new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            CreateBorderlessTableBorders()));

        var paragraphProps = new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

        if (alignment is not null)
            paragraphProps.Append(new Justification { Val = alignment });

        var paragraph = new Paragraph(paragraphProps);
        paragraph.Append(CreateRun(text, bold));
        cell.Append(paragraph);
        return cell;
    }

    private static TableBorders CreateBorderlessTableBorders() => new(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil },
        new InsideHorizontalBorder { Val = BorderValues.Nil },
        new InsideVerticalBorder { Val = BorderValues.Nil });

    private static void AppendLabelValueParagraph(Body body, string label, string value)
    {
        var paragraph = new Paragraph();
        paragraph.Append(CreateRun(label, bold: true));
        paragraph.Append(CreateRun(" " + value));
        body.Append(paragraph);
    }

    private static void AppendParagraph(
        Body body,
        string text,
        bool bold = false,
        int fontSizeHalfPoints = 24,
        JustificationValues? alignment = null,
        string? colorHex = null)
    {
        var paragraph = new Paragraph();
        if (alignment is not null)
            paragraph.Append(new ParagraphProperties(new Justification { Val = alignment }));

        paragraph.Append(CreateRun(text, bold, fontSizeHalfPoints, colorHex));
        body.Append(paragraph);
    }

    private static Run CreateRun(string text, bool bold = false, int fontSizeHalfPoints = 24, string? colorHex = null)
    {
        var run = new Run();
        var props = new RunProperties();
        props.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
        props.Append(new FontSize { Val = fontSizeHalfPoints.ToString(RuCulture) });
        if (bold)
            props.Append(new Bold());
        if (!string.IsNullOrEmpty(colorHex))
            props.Append(new Color { Val = colorHex });

        run.Append(props);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    private static void AppendSpacer(Body body) =>
        body.Append(new Paragraph(new Run(new Text(string.Empty))));

    private static int MmToTwips(int millimeters) =>
        (int)Math.Round(millimeters * 1440.0 / 25.4);

    private static string FormatDate(DateOnly? date) =>
        date is null ? "—" : date.Value.ToString("dd.MM.yyyy", RuCulture);

    private static string FormatQuantity(decimal quantity) =>
        quantity % 1 == 0
            ? quantity.ToString("0", RuCulture)
            : quantity.ToString("0.##", RuCulture);
}
