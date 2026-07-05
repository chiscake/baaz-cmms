using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.Catalog;

/// <summary>UC-A6 — отделы ремонта.</summary>
public interface IRepairDepartmentCatalogService
{
    Task<DataResult<IReadOnlyList<RepairDepartmentListItem>>> GetRepairDepartmentsAsync(
        CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<RepairDepartmentAdminListItem>>> GetRepairDepartmentsAdminAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<DataResult<RepairDepartmentAdminListItem>> CreateRepairDepartmentAsync(
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult<RepairDepartmentAdminListItem>> UpdateRepairDepartmentAsync(
        Guid departmentId,
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default);

    Task<DataResult> SetRepairDepartmentActiveAsync(
        Guid departmentId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<DataResult> DeleteRepairDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
}
