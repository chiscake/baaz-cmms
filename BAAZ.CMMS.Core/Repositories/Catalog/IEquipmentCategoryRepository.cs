using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;

namespace BAAZ.CMMS.Core.Repositories;

public interface IEquipmentCategoryRepository
{
    Task<DataResult<IReadOnlyList<EquipmentCategoryModel>>> ListAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<DataResult<EquipmentCategoryModel>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DataResult<EquipmentCategoryModel>> InsertAsync(EquipmentCategoryModel model, CancellationToken ct = default);

    Task<DataResult<EquipmentCategoryModel>> UpdateAsync(EquipmentCategoryModel model, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
