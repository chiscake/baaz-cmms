using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.ToolIssuance;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>UC-D8 — генерация .docx и сохранение по пути, выбранному пользователем.</summary>
public sealed class DocxFileToolRequisitionIntegration(IToolRequisitionDocxGenerator docxGenerator) : IToolRequisitionDocxIntegration
{
    private readonly IToolRequisitionDocxGenerator _docxGenerator = docxGenerator;

    public Task<DataResult<ToolRequisitionDocxResult>> CreateToolRequisitionDocxAsync(
        ToolRequisitionDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var directory = Path.GetDirectoryName(request.TargetFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            _docxGenerator.Generate(request, request.TargetFilePath);

            var result = new ToolRequisitionDocxResult
            {
                RequisitionId = request.Context.RequisitionId,
                RequisitionNumber = request.Context.RequisitionNumber,
                SavedFilePath = request.TargetFilePath,
            };

            return Task.FromResult(DataResult<ToolRequisitionDocxResult>.Ok(result));
        }
        catch (IOException ex)
        {
            return Task.FromResult(DataResult<ToolRequisitionDocxResult>.Fail(
                DataError.Unknown($"ToolRequisition_Error_SaveFailed: {ex.Message}")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(DataResult<ToolRequisitionDocxResult>.Fail(
                DataError.Unknown($"ToolRequisition_Error_SaveFailed: {ex.Message}")));
        }
    }
}
