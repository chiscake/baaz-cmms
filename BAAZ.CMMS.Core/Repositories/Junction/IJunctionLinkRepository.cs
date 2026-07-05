using BAAZ.CMMS.Core.Data;

namespace BAAZ.CMMS.Core.Repositories.Junction;

/// <summary>
/// Общий паттерн для junction-таблиц вида (key_id, value_id): чтение значений по ключу
/// и полная замена набора (delete-all + insert-distinct) — как в отделах нормативов
/// (<c>maintenance_norms_departments</c>, <c>category_maintenance_norms_departments</c>).
/// </summary>
public interface IJunctionLinkRepository<TModel>
{
    Task<DataResult<IReadOnlyList<Guid>>> GetValuesAsync(
        string keyColumn,
        Guid keyValue,
        Func<TModel, Guid> valueSelector,
        CancellationToken ct = default);

    Task<DataResult> ReplaceAsync(
        string keyColumn,
        Guid keyValue,
        IReadOnlyList<Guid> valueIds,
        Func<Guid, Guid, TModel> rowFactory,
        CancellationToken ct = default);
}
