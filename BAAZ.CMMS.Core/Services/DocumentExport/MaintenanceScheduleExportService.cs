using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Integrations.Documents.Maintenance;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

public interface IMaintenanceScheduleExportService
{
    DataResult<DocumentExportResult> ExportExcel(
        IReadOnlyList<MaintenanceScheduleItem> items,
        string periodLabel,
        string? filtersSummary,
        string targetFilePath);
}

public sealed class MaintenanceScheduleExportService(
    IMaintenanceScheduleExcelGenerator generator) : IMaintenanceScheduleExportService
{
    public DataResult<DocumentExportResult> ExportExcel(
        IReadOnlyList<MaintenanceScheduleItem> items,
        string periodLabel,
        string? filtersSummary,
        string targetFilePath)
    {
        var documentRequest = DocumentExportMappers.MapScheduleExcel(
            items,
            periodLabel,
            filtersSummary,
            DateTime.Now);

        return DocumentFileHelper.WriteFile(path => generator.Generate(documentRequest, path), targetFilePath);
    }
}
