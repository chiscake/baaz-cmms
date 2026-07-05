using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>UC-D8 — формирование заявки на инструмент в .docx (демо-канал).</summary>
public interface IToolRequisitionDocxIntegration
{
    Task<DataResult<ToolRequisitionDocxResult>> CreateToolRequisitionDocxAsync(
        ToolRequisitionDocumentRequest request,
        CancellationToken cancellationToken = default);
}
