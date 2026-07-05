using System.Globalization;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Контракт интеграции со складской системой (расходники, запчасти).
/// UC-D7 — формирование заявки на материалы (см. docs/use-cases/overview.md).
/// </summary>
public interface IWarehouseIntegration
{
    /// <summary>UC-D7 — сформировать заявку на выдачу материалов (docx на диск).</summary>
    Task<DataResult<MaterialRequisitionResult>> CreateMaterialRequisitionAsync(
        MaterialRequisitionDocumentRequest request,
        CancellationToken cancellationToken = default);
}
