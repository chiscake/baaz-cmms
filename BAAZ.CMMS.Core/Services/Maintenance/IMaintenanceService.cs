using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

/// <summary>
/// Планово-предупредительный ремонт: график, нормативы, отчёты о работах.
/// Покрывает UC-D3, UC-D4, UC-D5, UC-A5.
/// </summary>
public interface IMaintenanceService
{
    // UC-D3 — просмотр asset_maintenance_status + maintenance_schedule
    Task<IReadOnlyList<MaintenanceScheduleItem>> GetScheduleAsync(
        CancellationToken cancellationToken = default,
        bool markOverdue = true);

    /// <summary>
    /// Перезагрузка schedule + junction без повторной загрузки справочников (assets, departments, status).
    /// </summary>
    Task<IReadOnlyList<MaintenanceScheduleItem>> RefreshScheduleItemsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Сброс кэша справочников (полный <see cref="GetScheduleAsync"/> перезагружает их).</summary>
    void InvalidateScheduleReferenceCache();

    /// <summary>Бейдж навигации: overdue + in_progress + scheduled на сегодня (локальная дата).</summary>
    Task<int> GetScheduleNavBadgeCountAsync(CancellationToken cancellationToken = default);

    // UC-D5 — отмена позиции, пометка overdue, начало работ
    Task<bool> CancelScheduleItemAsync(Guid scheduleId, string? comment = null, CancellationToken cancellationToken = default);

    Task<bool> MarkScheduleOverdueAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<bool> StartScheduleWorkAsync(Guid scheduleId, string? comment = null, CancellationToken cancellationToken = default);

    Task<DataResult<int>> CancelAllOpenScheduleItemsAsync(CancellationToken cancellationToken = default);

    Task<DataResult<Guid>> CreateScheduleEntryAsync(
        CreateScheduleInput input, CancellationToken cancellationToken = default);

    Task<DataResult<int>> GeneratePprScheduleAsync(
        int horizonDays = 30, CancellationToken cancellationToken = default);

    // UC-D4 — создание отчёта о выполненных работах (ППР)
    Task<DataResult> CreateWorkReportAsync(WorkReportInput input, CancellationToken cancellationToken = default);

    // UC-D4 — список отчётов (реестр)
    Task<IReadOnlyList<WorkReportItem>> GetWorkReportsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForScheduleAsync(
        Guid scheduleId, CancellationToken cancellationToken = default);

    // UC-A5 — категории оборудования (пресеты)
    Task<DataResult<IReadOnlyList<EquipmentCategoryListItem>>> GetCategoriesAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<CategoryNormSlot>>> GetCategoryNormsAsync(
        Guid categoryId, CancellationToken cancellationToken = default);

    Task<DataResult<EquipmentCategoryListItem>> CreateCategoryAsync(
        EquipmentCategoryEditInput input, CancellationToken cancellationToken = default);

    Task<DataResult<EquipmentCategoryListItem>> UpdateCategoryAsync(
        Guid categoryId, EquipmentCategoryEditInput input, CancellationToken cancellationToken = default);

    Task<DataResult> SaveCategoryNormsAsync(
        Guid categoryId, IReadOnlyList<CategoryNormSlotInput> slots, CancellationToken cancellationToken = default);

    // UC-A5 — нормативы объекта (пресет + индивидуальные override)
    Task<DataResult<AssetNormsDetail>> GetAssetNormsDetailAsync(
        Guid assetId, CancellationToken cancellationToken = default);

    Task<DataResult> SaveAssetNormOverridesAsync(
        AssetNormOverridesInput input, CancellationToken cancellationToken = default);

    // UC-A5 — плоский список для вкладки «Все нормативы»
    Task<DataResult<IReadOnlyList<MaintenanceNormItem>>> GetAllEffectiveNormsAsync(
        CancellationToken cancellationToken = default);

    Task<DataResult<bool>> HasPendingScheduleAsync(
        Guid assetId, string maintenanceType, CancellationToken cancellationToken = default);
}
