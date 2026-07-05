using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Warehouse;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>UC-D7 — генерация .docx и сохранение по пути, выбранному пользователем.</summary>
public sealed class DocxFileWarehouseIntegration(IMaterialRequisitionDocxGenerator docxGenerator) : IWarehouseIntegration
{
    private readonly IMaterialRequisitionDocxGenerator _docxGenerator = docxGenerator;

    public Task<DataResult<MaterialRequisitionResult>> CreateMaterialRequisitionAsync(
        MaterialRequisitionDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var directory = Path.GetDirectoryName(request.TargetFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            _docxGenerator.Generate(request, request.TargetFilePath);

            var result = new MaterialRequisitionResult
            {
                RequisitionId = request.Context.RequisitionId,
                RequisitionNumber = request.Context.RequisitionNumber,
                SavedFilePath = request.TargetFilePath,
            };

            return Task.FromResult(DataResult<MaterialRequisitionResult>.Ok(result));
        }
        catch (IOException ex)
        {
            return Task.FromResult(DataResult<MaterialRequisitionResult>.Fail(
                DataError.Unknown($"MaterialRequisition_Error_SaveFailed: {ex.Message}")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(DataResult<MaterialRequisitionResult>.Fail(
                DataError.Unknown($"MaterialRequisition_Error_SaveFailed: {ex.Message}")));
        }
    }
}
