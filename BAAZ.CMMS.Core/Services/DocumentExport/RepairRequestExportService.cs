using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Integrations.Documents.Requests;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

public interface IRepairRequestExportService
{
    Task<DataResult<DocumentExportResult>> ExportAsync(
        Guid requestId,
        string targetFilePath,
        CancellationToken cancellationToken = default);
}

public sealed class RepairRequestExportService(
    IRequestService requestService,
    IAuthService authService,
    IAssetRepository assetRepository,
    ILocationTreeCache locationTreeCache,
    IRepairRequestDocxGenerator generator) : IRepairRequestExportService
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

        return DocumentFileHelper.WriteFile(path => generator.Generate(load.Value, path), targetFilePath);
    }
}
