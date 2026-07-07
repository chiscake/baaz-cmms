using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models.DocumentExport;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BAAZ.CMMS.Core.Integrations.Documents.Maintenance;

public interface IMaintenanceScheduleExcelGenerator
{
    void Generate(MaintenanceScheduleExcelRequest request, string targetFilePath);
}

public sealed class MaintenanceScheduleExcelGenerator : IMaintenanceScheduleExcelGenerator
{
    private const uint StyleTitle = 1U;
    private const uint StyleMeta = 2U;
    private const uint StyleHeader = 3U;
    private const uint StyleData = 4U;
    private const uint StyleStatusScheduled = 5U;
    private const uint StyleStatusInProgress = 6U;
    private const uint StyleStatusOverdue = 7U;
    private const uint StyleStatusCompleted = 8U;
    private const uint StyleStatusCancelled = 9U;

    private static readonly string[] Headers =
    [
        "№ п/п", "Инв. №", "Наименование", "Вид ТО", "Плановая дата",
        "Статус", "Отделы", "Последнее ТО", "Следующее ТО",
    ];

    public void Generate(MaintenanceScheduleExcelRequest request, string targetFilePath)
    {
        using var document = SpreadsheetDocument.Create(targetFilePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        AddStyles(workbookPart);

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        var rows = new List<Row>();

        uint rowIndex = 1;
        rows.Add(CreateStyledRow(rowIndex++, [("Ведомость графика ППР", StyleTitle)]));
        rows.Add(CreateStyledRow(rowIndex++, [($"Период: {request.PeriodLabel}", StyleMeta)]));
        rows.Add(CreateStyledRow(rowIndex++,
            [($"Сформировано: {request.GeneratedAt:dd.MM.yyyy HH:mm}", StyleMeta)]));

        if (!string.IsNullOrWhiteSpace(request.FiltersSummary))
            rows.Add(CreateStyledRow(rowIndex++, [($"Фильтры: {request.FiltersSummary}", StyleMeta)]));

        rowIndex++;

        rows.Add(CreateStyledRow(rowIndex++, Headers.Select(h => (h, StyleHeader)).ToArray()));

        var index = 1;
        foreach (var dataRow in request.Rows)
        {
            var values = new (string Value, uint Style)[]
            {
                (index.ToString(DocxDocumentBuilder.RuCulture), StyleData),
                (dataRow.AssetNumber, StyleData),
                (dataRow.AssetName, StyleData),
                (dataRow.MaintenanceTypeLabel, StyleData),
                (dataRow.PlannedDate.ToString("dd.MM.yyyy", DocxDocumentBuilder.RuCulture), StyleData),
                (dataRow.StatusLabel, ResolveStatusStyle(dataRow.Status)),
                (dataRow.DepartmentNames, StyleData),
                (dataRow.LastMaintenanceDate ?? "—", StyleData),
                (dataRow.NextMaintenanceDate ?? "—", StyleData),
            };
            rows.Add(CreateStyledRow(rowIndex++, values));
            index++;
        }

        foreach (var row in rows)
            sheetData.Append(row);

        var columnWidths = CalculateColumnWidths(rows, Headers.Length);
        worksheetPart.Worksheet = new Worksheet(
            CreateColumns(columnWidths),
            sheetData);

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "График ППР",
        });

        workbookPart.Workbook.Save();
    }

    private static uint ResolveStatusStyle(string status) => status switch
    {
        "scheduled" => StyleStatusScheduled,
        "in_progress" => StyleStatusInProgress,
        "overdue" => StyleStatusOverdue,
        "completed" => StyleStatusCompleted,
        "cancelled" => StyleStatusCancelled,
        _ => StyleData,
    };

    private static Row CreateStyledRow(uint rowIndex, IReadOnlyList<(string Value, uint Style)> cells)
    {
        var row = new Row { RowIndex = rowIndex };
        for (var i = 0; i < cells.Count; i++)
        {
            row.Append(new Cell
            {
                CellReference = $"{ColumnLetter(i)}{rowIndex}",
                DataType = CellValues.String,
                CellValue = new CellValue(cells[i].Value ?? string.Empty),
                StyleIndex = cells[i].Style,
            });
        }

        return row;
    }

    private static Columns CreateColumns(double[] widths)
    {
        var columns = new Columns();
        for (var i = 0; i < widths.Length; i++)
        {
            columns.Append(new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = widths[i],
                CustomWidth = true,
            });
        }

        return columns;
    }

    private static double[] CalculateColumnWidths(IReadOnlyList<Row> rows, int columnCount)
    {
        var maxLengths = new int[columnCount];
        foreach (var row in rows)
        {
            var col = 0;
            foreach (var cell in row.Elements<Cell>())
            {
                if (col >= columnCount)
                    break;

                var text = cell.CellValue?.Text ?? string.Empty;
                maxLengths[col] = Math.Max(maxLengths[col], text.Length);
                col++;
            }
        }

        var widths = new double[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            widths[i] = i == 0
                ? Math.Clamp(maxLengths[i] * 0.9 + 1.5, 4, 8)
                : Math.Clamp(maxLengths[i] * 1.15 + 2.5, 8, 48);
        }

        return widths;
    }

    private static void AddStyles(WorkbookPart workbookPart)
    {
        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = new Stylesheet(
            CreateFonts(),
            CreateFills(),
            CreateBorders(),
            new CellStyleFormats(new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 }),
            CreateCellFormats(),
            new CellStyles(new CellStyle { Name = "Normal", FormatId = 0, BuiltinId = 0 }));
        stylesPart.Stylesheet.Save();
    }

    private const string WorksheetFontName = "Liberation Sans";

    private static FontName CreateWorksheetFontFace() => new() { Val = WorksheetFontName };

    private static Fonts CreateFonts() => new(
        new Font(CreateWorksheetFontFace()),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 14 }),
        new Font(CreateWorksheetFontFace(), new FontSize { Val = 11 }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FF1565C0" }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FFF57F17" }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FFC62828" }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FF2E7D32" }),
        new Font(CreateWorksheetFontFace(), new Bold(), new FontSize { Val = 11 }, new Color { Rgb = "FF5D4037" }));

    private static Fills CreateFills() => new(
        new Fill(new PatternFill { PatternType = PatternValues.None }),
        new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFE8E8E8" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFE3F2FD" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFFFF8E1" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFFFEBEE" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFE8F5E9" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }),
        new Fill(new PatternFill(
            new ForegroundColor { Rgb = "FFEFEBE9" },
            new BackgroundColor { Indexed = 64 })
        { PatternType = PatternValues.Solid }));

    private static Borders CreateBorders() => new(
        new Border(),
        new Border(
            new LeftBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFB0B0B0" } },
            new RightBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFB0B0B0" } },
            new TopBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFB0B0B0" } },
            new BottomBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFB0B0B0" } },
            new DiagonalBorder()));

    private static CellFormats CreateCellFormats() => new(
        new CellFormat(),
        new CellFormat { FontId = 1, ApplyFont = true },
        new CellFormat { FontId = 2, ApplyFont = true },
        new CellFormat
        {
            FontId = 3,
            FillId = 2,
            BorderId = 1,
            ApplyFont = true,
            ApplyFill = true,
            ApplyBorder = true,
            Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
        },
        new CellFormat
        {
            FontId = 2,
            BorderId = 1,
            ApplyFont = true,
            ApplyBorder = true,
            Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true },
        },
        CreateStatusFormat(4, 3),
        CreateStatusFormat(5, 4),
        CreateStatusFormat(6, 5),
        CreateStatusFormat(7, 6),
        CreateStatusFormat(8, 7));

    private static CellFormat CreateStatusFormat(uint fontId, uint fillId) => new()
    {
        FontId = fontId,
        FillId = fillId,
        BorderId = 1,
        ApplyFont = true,
        ApplyFill = true,
        ApplyBorder = true,
        Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
    };

    private static string ColumnLetter(int index)
    {
        var dividend = index + 1;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }
}
