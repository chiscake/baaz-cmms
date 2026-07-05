using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

/// <summary>UC-A3 — персонал ТОиР.</summary>
public interface ITechnicianCatalogService
{
    Task<DataResult<IReadOnlyList<TechnicianListItem>>> GetTechniciansAsync(
        CancellationToken cancellationToken = default);

    Task<DataResult<TechnicianListItem>> CreateTechnicianAsync(
        TechnicianEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult<TechnicianListItem>> UpdateTechnicianAsync(
        Guid technicianId,
        TechnicianEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult> SetTechnicianActiveAsync(
        Guid technicianId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<DataResult> DeleteTechnicianAsync(
        Guid technicianId,
        CancellationToken cancellationToken = default);
}
