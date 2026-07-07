using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface ICategoryMaintenanceNormRepository
{
    Task<DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>> ListByCategoryAsync(
        Guid categoryId, CancellationToken ct = default);

    Task<DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>> ListByCategoryIdsAsync(
        IReadOnlyList<Guid> categoryIds, CancellationToken ct = default);

    Task<DataResult<CategoryMaintenanceNormModel>> InsertAsync(
        CategoryMaintenanceNormModel model, CancellationToken ct = default);

    Task<DataResult<CategoryMaintenanceNormModel>> UpdateAsync(
        CategoryMaintenanceNormModel model, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
