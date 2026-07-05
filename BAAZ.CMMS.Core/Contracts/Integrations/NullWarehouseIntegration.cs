using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>No-op заглушка для тестов — не регистрируется в production DI.</summary>
public sealed class NullWarehouseIntegration : IWarehouseIntegration
{
    public Task<DataResult<MaterialRequisitionResult>> CreateMaterialRequisitionAsync(
        MaterialRequisitionDocumentRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<MaterialRequisitionResult>.Ok(new MaterialRequisitionResult
        {
            RequisitionId = Guid.NewGuid(),
            RequisitionNumber = "ТМЦ-STUB",
            SavedFilePath = request.TargetFilePath,
        }));
}
