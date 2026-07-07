using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Integrations.Documents.Requests;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

public interface IRequestCardExportService
{
    Task<DataResult<DocumentExportResult>> ExportAsync(
        Guid requestId,
        string targetFilePath,
        CancellationToken cancellationToken = default);
}

public sealed class RequestCardExportService(
    IRequestService requestService,
    IAuthService authService,
    IAssetRepository assetRepository,
    ILocationTreeCache locationTreeCache,
    IRequestCardDocxGenerator generator) : IRequestCardExportService
{
    public async Task<DataResult<DocumentExportResult>> ExportAsync(
        Guid requestId,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        var load = await RequestDocumentExportHelper.LoadRepairRequestDocumentAsync(
            requestId,
            requestService,
            authService,
            assetRepository,
            locationTreeCache,
            cancellationToken);
        if (!load.IsSuccess || load.Value is null)
            return DataResult<DocumentExportResult>.Fail(load.Error ?? DataError.Unknown());

        var history = await requestService.GetStatusHistoryAsync(requestId, cancellationToken);
        var workReports = await requestService.GetWorkReportsForRequestAsync(requestId, cancellationToken);
        var documentRequest = DocumentExportMappers.MapRequestCard(load.Value, history, workReports);

        return DocumentFileHelper.WriteFile(path => generator.Generate(documentRequest, path), targetFilePath);
    }
}
