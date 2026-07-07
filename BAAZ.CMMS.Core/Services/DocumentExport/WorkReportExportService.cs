using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Integrations.Documents.WorkReports;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

public interface IWorkReportExportService
{
    Task<DataResult<DocumentExportResult>> ExportAsync(
        Guid workReportId,
        string targetFilePath,
        CancellationToken cancellationToken = default);
}

public sealed class WorkReportExportService(
    IWorkReportRepository workReportRepository,
    IWorkReportDocxGenerator generator) : IWorkReportExportService
{
    public async Task<DataResult<DocumentExportResult>> ExportAsync(
        Guid workReportId,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        var load = await workReportRepository.GetByIdAsync(workReportId, cancellationToken);
        if (!load.IsSuccess || load.Value is null)
            return DataResult<DocumentExportResult>.Fail(load.Error ?? DataError.Unknown());

        var documentRequest = DocumentExportMappers.MapWorkReport(load.Value);
        return DocumentFileHelper.WriteFile(path => generator.Generate(documentRequest, path), targetFilePath);
    }
}
