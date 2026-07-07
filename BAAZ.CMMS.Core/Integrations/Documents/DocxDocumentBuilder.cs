using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BAAZ.CMMS.Core.Integrations.Documents;

/// <summary>Общие примитивы печатных форм DOCX (A4, шапка, таблицы, подписи).</summary>
public static class DocxDocumentBuilder
{
    public static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    public const int CellPaddingTwips = 100;

    public static readonly int SignatureLineWidthTwips = MmToTwips(35);

    private static readonly int PageWidthTwips = MmToTwips(210);
    private static readonly int PageHeightTwips = MmToTwips(297);

    public static WordprocessingDocument CreateDocument(string targetFilePath, out Body body)
    {
        var document = WordprocessingDocument.Create(targetFilePath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        body = mainPart.Document.Body!;
        return document;
    }

    public static void SaveAndDispose(WordprocessingDocument document, Body body)
    {
        ApplySectionProperties(body);
        document.MainDocumentPart!.Document.Save();
        document.Dispose();
    }

    public static void ApplySectionProperties(Body body)
    {
        foreach (var paragraph in body.Elements<Paragraph>())
            paragraph.Elements<SectionProperties>().FirstOrDefault()?.Remove();

        body.Elements<SectionProperties>().FirstOrDefault()?.Remove();
        body.Append(CreateSectionProperties());
    }

    public static void AppendApprovalHeader(Body body, int year, string approverTitle)
    {
        AppendParagraph(body, "УТВЕРЖДАЮ", bold: true, alignment: JustificationValues.Right);
        AppendParagraph(body, approverTitle, alignment: JustificationValues.Right);
        AppendParagraph(body, "ОАО «БААЗ»", alignment: JustificationValues.Right);
        AppendParagraph(body, "___________ / __________________", alignment: JustificationValues.Right);
        AppendParagraph(body, $"«___» ____________ {year} г.", alignment: JustificationValues.Right);
        AppendSpacer(body);
    }

    /// <summary>Шапка «УТВЕРЖДАЮ» с указанием ремонтного подразделения.</summary>
    public static void AppendDepartmentApprovalHeader(Body body, int year, string? departmentName)
    {
        AppendParagraph(body, "УТВЕРЖДАЮ", bold: true, alignment: JustificationValues.Right);
        AppendParagraph(body, "Начальник подразделения:", alignment: JustificationValues.Right);
        AppendParagraph(body, string.IsNullOrWhiteSpace(departmentName) ? "—" : departmentName,
            alignment: JustificationValues.Right);
        AppendParagraph(body, "ОАО «БААЗ»", alignment: JustificationValues.Right);
        AppendParagraph(body, "___________ / __________________", alignment: JustificationValues.Right);
        AppendParagraph(body, $"«___» ____________ {year} г.", alignment: JustificationValues.Right);
        AppendSpacer(body);
    }

    public static void AppendTitleBlock(
        Body body,
        string titleLine1,
        string? titleLine2,
        DateTime documentDate)
    {
        var monthName = RuCulture.DateTimeFormat.GetMonthName(documentDate.Month);
        AppendParagraph(body, titleLine1, bold: true, fontSizeHalfPoints: 28,
            alignment: JustificationValues.Center);
        if (!string.IsNullOrWhiteSpace(titleLine2))
        {
            AppendParagraph(body, titleLine2, bold: true, fontSizeHalfPoints: 28,
                alignment: JustificationValues.Center);
        }

        AppendParagraph(body,
            $"от «{documentDate.Day:00}» {monthName} {documentDate.Year} г.",
            alignment: JustificationValues.Center);
        AppendSpacer(body);
    }

    public static void AppendLabelValueParagraph(Body body, string label, string value)
    {
        var paragraph = new Paragraph();
        paragraph.Append(CreateRun(label, bold: true));
        paragraph.Append(CreateRun(" " + value));
        body.Append(paragraph);
    }

    public static void AppendParagraph(
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

    public static void AppendSpacer(Body body) =>
        body.Append(new Paragraph(new Run(new Text(string.Empty))));

    public static Table CreateBorderedTable(int[] columnWidthsTwips)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Fixed },
            CreateBorderedTableBorders()));

        var grid = new TableGrid();
        foreach (var width in columnWidthsTwips)
            grid.Append(new GridColumn { Width = width.ToString(RuCulture) });

        table.Append(grid);
        return table;
    }

    public static void AppendHeaderRow(Table table, IReadOnlyList<string> headers)
    {
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
    }

    public static Table CreateSignatureTable()
    {
        var separatorWidth = MmToTwips(10).ToString(RuCulture);
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Fixed },
            CreateBorderlessTableBorders()));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = MmToTwips(70).ToString(RuCulture) });
        grid.Append(new GridColumn { Width = separatorWidth });
        grid.Append(new GridColumn { Width = SignatureLineWidthTwips.ToString(RuCulture) });
        grid.Append(new GridColumn { Width = separatorWidth });
        grid.Append(new GridColumn { Width = MmToTwips(55).ToString(RuCulture) });
        table.Append(grid);
        return table;
    }

    public static void AppendSignatureRow(Table table, string role, string name)
    {
        var row = new TableRow();
        row.Append(CreateBorderlessTableCell(role));
        row.Append(CreateSignatureSeparatorCell());
        row.Append(CreateSignatureUnderlineCell(alignment: JustificationValues.Center));
        row.Append(CreateSignatureSeparatorCell());
        row.Append(CreateSignatureUnderlineCell(name));
        table.Append(row);
    }

    public static Table CreateMetadataTable()
    {
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Fixed },
            CreateBorderlessTableBorders()));

        var grid = new TableGrid();
        grid.Append(new GridColumn { Width = MmToTwips(52).ToString(RuCulture) });
        grid.Append(new GridColumn { Width = MmToTwips(118).ToString(RuCulture) });
        table.Append(grid);
        return table;
    }

    public static void AppendMetadataRow(Table table, string label, string value)
    {
        var row = new TableRow();
        row.Append(CreateMetadataLabelCell(label));
        row.Append(CreateMetadataValueCell(value));
        table.Append(row);
    }

    public static void AppendMetadataDescriptionRow(Table table, string label, string? description)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            var row = new TableRow();
            row.Append(CreateMetadataLabelCell(label));
            row.Append(CreateMetadataValueCell(description));
            table.Append(row);
            return;
        }

        const int blankLineCount = 3;
        for (var i = 0; i < blankLineCount; i++)
        {
            var row = new TableRow();
            row.Append(i == 0
                ? CreateMetadataLabelCell(label, verticalMergeRestart: true)
                : CreateMetadataLabelContinuationCell());
            row.Append(CreateMetadataUnderlineValueCell());
            table.Append(row);
        }
    }

    public static void AppendFooter(Body body, Guid recordId, string? suffix = null)
    {
        AppendParagraph(body, string.Empty);
        AppendParagraph(body, string.Empty);
        var text = string.IsNullOrWhiteSpace(suffix)
            ? $"ID: {recordId} | Сгенерировано в BAAZ.CMMS"
            : $"ID: {recordId} | {suffix} | Сгенерировано в BAAZ.CMMS";
        AppendParagraph(body, text, fontSizeHalfPoints: 16, colorHex: "666666");
    }

    public static TableCell CreateTableCell(
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

    public static TableCell CreateBorderlessTableCell(
        string text,
        bool bold = false,
        JustificationValues? alignment = null)
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

    public static TableCell CreateSignatureSeparatorCell()
    {
        var cell = new TableCell(new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            CreateBorderlessTableBorders()));
        cell.Append(new Paragraph(new Run(new Text(string.Empty))));
        return cell;
    }

    public static TableCell CreateSignatureUnderlineCell(
        string text = "",
        JustificationValues? alignment = null)
    {
        var cell = new TableCell(new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom },
            new TableCellBorders(new BottomBorder { Val = BorderValues.Single, Size = 4 })));

        var paragraphProps = new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

        if (alignment is not null)
            paragraphProps.Append(new Justification { Val = alignment });

        var paragraph = new Paragraph(paragraphProps);
        if (!string.IsNullOrEmpty(text))
            paragraph.Append(CreateRun(text));

        cell.Append(paragraph);
        return cell;
    }

    public static TableCell CreateMetadataLabelCell(string label, bool verticalMergeRestart = false)
    {
        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
            CreateBorderlessTableBorders());

        if (verticalMergeRestart)
            cellProps.Append(new VerticalMerge { Val = MergedCellValues.Restart });

        var cell = new TableCell(cellProps);
        var paragraph = new Paragraph(new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }));
        paragraph.Append(CreateRun($"{label}:", bold: true));
        cell.Append(paragraph);
        return cell;
    }

    public static TableCell CreateMetadataLabelContinuationCell()
    {
        var cellProps = new TableCellProperties(
            CreateBorderlessTableBorders(),
            new VerticalMerge());

        return new TableCell(cellProps, new Paragraph());
    }

    public static TableCell CreateMetadataValueCell(string value) =>
        CreateBorderlessTableCell(value);

    public static TableCell CreateMetadataUnderlineValueCell()
    {
        var cell = new TableCell(new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom },
            new TableCellBorders(new BottomBorder { Val = BorderValues.Single, Size = 4 })));

        var paragraph = new Paragraph(new ParagraphProperties(
            new SpacingBetweenLines { After = "0", Line = "360", LineRule = LineSpacingRuleValues.Auto }));
        paragraph.Append(new Run(new Text(string.Empty)));
        cell.Append(paragraph);
        return cell;
    }

    public static TableBorders CreateBorderedTableBorders() => new(
        new TopBorder { Val = BorderValues.Single, Size = 4 },
        new LeftBorder { Val = BorderValues.Single, Size = 4 },
        new BottomBorder { Val = BorderValues.Single, Size = 4 },
        new RightBorder { Val = BorderValues.Single, Size = 4 },
        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 });

    public static TableBorders CreateBorderlessTableBorders() => new(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil },
        new InsideHorizontalBorder { Val = BorderValues.Nil },
        new InsideVerticalBorder { Val = BorderValues.Nil });

    public static Run CreateRun(string text, bool bold = false, int fontSizeHalfPoints = 24, string? colorHex = null)
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

    public static int MmToTwips(int millimeters) =>
        (int)Math.Round(millimeters * 1440.0 / 25.4);

    public static string FormatDate(DateOnly? date) =>
        date is null ? "—" : date.Value.ToString("dd.MM.yyyy", RuCulture);

    public static DateTime GetLocalDate(DateTimeOffset value) => value.ToLocalTime().DateTime.Date;

    public static DateTime GetLocalDate(DateTime value) => value.Date;

    public static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", RuCulture);

    public static string FormatQuantity(decimal quantity) =>
        quantity % 1 == 0
            ? quantity.ToString("0", RuCulture)
            : quantity.ToString("0.##", RuCulture);

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
}
