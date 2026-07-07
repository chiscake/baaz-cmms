using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

internal static class RequestDocumentExportHelper
{
    public static async Task<DataResult<RepairRequestDocumentRequest>> LoadRepairRequestDocumentAsync(
        Guid requestId,
        IRequestService requestService,
        IAuthService authService,
        IAssetRepository assetRepository,
        ILocationTreeCache locationTreeCache,
        CancellationToken cancellationToken = default)
    {
        var detail = await requestService.GetRequestByIdAsync(requestId, cancellationToken);
        if (detail is null)
            return DataResult<RepairRequestDocumentRequest>.Fail(DataError.NotFound("Request_NotFound"));

        var locationDescription = await DocumentLocationResolver.ResolveForRequestAsync(
            detail,
            assetRepository,
            locationTreeCache,
            cancellationToken);

        var author = ResolveAuthorName(authService.CurrentProfile);
        var documentRequest = DocumentExportMappers.MapRepairRequest(
            detail,
            locationDescription,
            author,
            DateTimeOffset.Now);

        return DataResult<RepairRequestDocumentRequest>.Ok(documentRequest);
    }

    private static string ResolveAuthorName(UserProfile? profile) =>
        string.IsNullOrWhiteSpace(profile?.FullName) ? profile?.Id.ToString() ?? "—" : profile.FullName;
}
