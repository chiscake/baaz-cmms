using System.Text.Json.Serialization;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Repositories.Junction;
using BAAZ.CMMS.Core.Services.Integrations;

namespace BAAZ.CMMS.Core.Services;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IEquipmentCategoryRepository _categoryRepo;
    private readonly ICategoryMaintenanceNormRepository _categoryNormRepo;
    private readonly IJunctionLinkRepository<CategoryMaintenanceNormDepartmentModel> _categoryNormDeptRepo;
    private readonly IMaintenanceNormRepository _normRepo;
    private readonly IJunctionLinkRepository<MaintenanceNormDepartmentModel> _normDeptRepo;
    private readonly IAssetMaintenanceStatusRepository _statusRepo;
    private readonly IMaintenanceScheduleRepository _scheduleRepo;
    private readonly IWorkReportRepository _workReportRepository;
    private readonly IRepairDepartmentRepository _deptRepo;
    private readonly IAssetRepository _assetRepo;
    private readonly ISupabaseClientProvider _clientProvider;
    private readonly IAuthService _authService;
    private readonly IRequestIntegrationHooks _integrationHooks;

    private ScheduleReferenceData? _referenceData;

    private sealed class ScheduleReferenceData
    {
        public required Dictionary<Guid, AssetModel> AssetsById { get; init; }

        public required Dictionary<Guid, string> DeptNamesById { get; init; }

        public required Dictionary<(Guid AssetId, string MaintenanceType), AssetMaintenanceStatusModel> StatusByKey { get; init; }
    }

    public MaintenanceService(
        IEquipmentCategoryRepository categoryRepo,
        ICategoryMaintenanceNormRepository categoryNormRepo,
        IJunctionLinkRepository<CategoryMaintenanceNormDepartmentModel> categoryNormDeptRepo,
        IMaintenanceNormRepository normRepo,
        IJunctionLinkRepository<MaintenanceNormDepartmentModel> normDeptRepo,
        IAssetMaintenanceStatusRepository statusRepo,
        IMaintenanceScheduleRepository scheduleRepo,
        IWorkReportRepository workReportRepository,
        IRepairDepartmentRepository deptRepo,
        IAssetRepository assetRepo,
        ISupabaseClientProvider clientProvider,
        IAuthService authService,
        IRequestIntegrationHooks integrationHooks)
    {
        _categoryRepo = categoryRepo;
        _categoryNormRepo = categoryNormRepo;
        _categoryNormDeptRepo = categoryNormDeptRepo;
        _normRepo = normRepo;
        _normDeptRepo = normDeptRepo;
        _statusRepo = statusRepo;
        _scheduleRepo = scheduleRepo;
        _workReportRepository = workReportRepository;
        _deptRepo = deptRepo;
        _assetRepo = assetRepo;
        _clientProvider = clientProvider;
        _authService = authService;
        _integrationHooks = integrationHooks;
    }

    public async Task<IReadOnlyList<MaintenanceScheduleItem>> GetScheduleAsync(
        CancellationToken cancellationToken = default,
        bool markOverdue = true)
    {
        if (markOverdue)
            await MarkOverdueScheduleItemsAsync(cancellationToken);

        var refs = await ReloadReferenceDataAsync(cancellationToken);
        return await LoadScheduleItemsAsync(refs, cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceScheduleItem>> RefreshScheduleItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var refs = await EnsureReferenceDataAsync(cancellationToken);
        if (refs is null)
            return await GetScheduleAsync(cancellationToken, markOverdue: false);

        return await LoadScheduleItemsAsync(refs, cancellationToken);
    }

    public void InvalidateScheduleReferenceCache() => _referenceData = null;

    private async Task<ScheduleReferenceData?> EnsureReferenceDataAsync(CancellationToken cancellationToken)
    {
        if (_referenceData is not null)
            return _referenceData;

        try
        {
            return await ReloadReferenceDataAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ScheduleReferenceData> ReloadReferenceDataAsync(CancellationToken cancellationToken)
    {
        var assetsResult = await _assetRepo.ListAsync(includeDecommissioned: true, cancellationToken);
        var assetsById = assetsResult.IsSuccess
            ? assetsResult.Value!.ToDictionary(a => a.Id)
            : new Dictionary<Guid, AssetModel>();

        var statusResult = await _statusRepo.ListAllAsync(cancellationToken);
        var statusByKey = statusResult.IsSuccess
            ? statusResult.Value!.ToDictionary(s => (s.AssetId, s.MaintenanceType))
            : new Dictionary<(Guid, string), AssetMaintenanceStatusModel>();

        var deptResult = await _deptRepo.ListAsync(includeInactive: true, cancellationToken);
        var deptNamesById = deptResult.IsSuccess
            ? deptResult.Value!.ToDictionary(d => d.Id, d => d.Name)
            : new Dictionary<Guid, string>();

        _referenceData = new ScheduleReferenceData
        {
            AssetsById = assetsById,
            DeptNamesById = deptNamesById,
            StatusByKey = statusByKey,
        };

        return _referenceData;
    }

    private async Task<IReadOnlyList<MaintenanceScheduleItem>> LoadScheduleItemsAsync(
        ScheduleReferenceData refs,
        CancellationToken cancellationToken)
    {
        var scheduleResult = await _scheduleRepo.ListAsync(cancellationToken);
        if (!scheduleResult.IsSuccess)
            return [];

        var schedules = scheduleResult.Value!;
        if (schedules.Count == 0)
            return [];

        var scheduleDeptResult = await _scheduleRepo.ListDepartmentsAsync(cancellationToken);
        var deptIdsBySchedule = scheduleDeptResult.IsSuccess
            ? scheduleDeptResult.Value!
                .GroupBy(x => x.ScheduleId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RepairDepartmentId).ToList())
            : new Dictionary<Guid, List<Guid>>();

        var openScheduleIds = schedules
            .Where(s => s.Status is "scheduled" or "overdue" or "in_progress" or "completed")
            .Select(s => s.Id)
            .ToList();

        var reportsBySchedule = new Dictionary<Guid, List<Guid>>();
        if (openScheduleIds.Count > 0)
        {
            var reportsResult = await _workReportRepository.ListByScheduleIdsAsync(openScheduleIds, cancellationToken);
            if (reportsResult.IsSuccess)
            {
                reportsBySchedule = reportsResult.Value!
                    .GroupBy(r => r.ScheduleId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.RepairDepartmentId).Distinct().ToList());
            }
        }

        return MapScheduleItems(
            schedules,
            refs.AssetsById,
            refs.StatusByKey,
            refs.DeptNamesById,
            deptIdsBySchedule,
            reportsBySchedule);
    }

    private static IReadOnlyList<MaintenanceScheduleItem> MapScheduleItems(
        IReadOnlyList<MaintenanceScheduleModel> schedules,
        IReadOnlyDictionary<Guid, AssetModel> assetsById,
        IReadOnlyDictionary<(Guid AssetId, string MaintenanceType), AssetMaintenanceStatusModel> statusByKey,
        IReadOnlyDictionary<Guid, string> deptNamesById,
        IReadOnlyDictionary<Guid, List<Guid>> deptIdsBySchedule,
        IReadOnlyDictionary<Guid, List<Guid>> reportsBySchedule)
    {
        return schedules.Select(row =>
        {
            assetsById.TryGetValue(row.AssetId, out var asset);
            statusByKey.TryGetValue((row.AssetId, row.MaintenanceType), out var status);
            deptIdsBySchedule.TryGetValue(row.Id, out var deptIds);
            reportsBySchedule.TryGetValue(row.Id, out var reportedIds);

            var departmentIds = deptIds ?? [];
            var departmentNames = departmentIds
                .Select(id => deptNamesById.TryGetValue(id, out var name) ? name : id.ToString())
                .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new MaintenanceScheduleItem
            {
                Id = row.Id,
                AssetId = row.AssetId,
                LocationId = asset?.LocationId,
                AssetNumber = asset?.AssetNumber ?? string.Empty,
                AssetName = asset?.Name ?? string.Empty,
                MaintenanceType = row.MaintenanceType,
                PlannedDate = row.PlannedDate,
                Status = row.Status,
                LastMaintenanceDate = status?.LastMaintenanceDate,
                NextMaintenanceDate = status?.NextMaintenanceDate,
                DepartmentNames = departmentNames,
                DepartmentIds = departmentIds,
                ReportedDepartmentIds = reportedIds ?? [],
            };
        }).ToList();
    }

    public async Task<int> GetScheduleNavBadgeCountAsync(CancellationToken cancellationToken = default)
    {
        await MarkOverdueScheduleItemsAsync(cancellationToken);

        var scheduleResult = await _scheduleRepo.ListAsync(cancellationToken);
        if (!scheduleResult.IsSuccess || scheduleResult.Value is null)
            return 0;

        var today = DateOnly.FromDateTime(DateTime.Today);
        return MaintenanceScheduleNavBadgeCount.ComputeFromModels(scheduleResult.Value, today);
    }

    public async Task<bool> CancelScheduleItemAsync(
        Guid scheduleId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var result = await _scheduleRepo.UpdateStatusAsync(scheduleId, "cancelled", cancellationToken);
        if (result.IsSuccess)
            await _integrationHooks.AfterScheduleCancelledAsync(scheduleId, cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> MarkScheduleOverdueAsync(
        Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var result = await _scheduleRepo.UpdateStatusAsync(scheduleId, "overdue", cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> StartScheduleWorkAsync(
        Guid scheduleId, string? comment = null, CancellationToken cancellationToken = default)
    {
        var (success, _) = await SupabaseRestClient.CallRpcVoidAsync(
            _clientProvider,
            "start_schedule_work",
            new { p_schedule_id = scheduleId, p_comment = comment },
            cancellationToken);

        return success;
    }

    public async Task<DataResult<int>> CancelAllOpenScheduleItemsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult<int>.Fail(DataError.Unauthorized());

        var scheduleResult = await _scheduleRepo.ListAsync(cancellationToken);
        if (!scheduleResult.IsSuccess)
            return DataResult<int>.Fail(scheduleResult.Error!);

        var openIds = scheduleResult.Value!
            .Where(s => s.Status is "scheduled" or "overdue" or "in_progress")
            .Select(s => s.Id)
            .ToList();

        if (openIds.Count == 0)
            return DataResult<int>.Ok(0);

        var cancelled = 0;
        foreach (var id in openIds)
        {
            var result = await _scheduleRepo.UpdateStatusAsync(id, "cancelled", cancellationToken);
            if (result.IsSuccess)
            {
                cancelled++;
                await _integrationHooks.AfterScheduleCancelledAsync(id, cancellationToken);
            }
        }

        if (cancelled == 0)
            return DataResult<int>.Fail(DataError.Unknown("Не удалось отменить позиции графика"));

        return DataResult<int>.Ok(cancelled);
    }

    public async Task<DataResult<Guid>> CreateScheduleEntryAsync(
        CreateScheduleInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var departmentIds = input.DepartmentIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? [];

            if (departmentIds.Count == 0
                && _authService.CurrentProfile?.Role == UserRole.Dispatcher)
            {
                departmentIds = await ResolveDispatcherScheduleDepartmentsAsync(
                    input.AssetId, input.MaintenanceType, cancellationToken);
            }

            var body = new Dictionary<string, object?>
            {
                ["p_asset_id"] = input.AssetId,
                ["p_maintenance_type"] = input.MaintenanceType,
                ["p_planned_date"] = input.PlannedDate.ToString("yyyy-MM-dd"),
            };

            if (departmentIds.Count > 0)
                body["p_department_ids"] = departmentIds;

            var rpcResult = await SupabaseRestClient.CallRpcScalarOrErrorAsync<Guid>(
                _clientProvider,
                "create_schedule_entry",
                body,
                cancellationToken);

            if (!rpcResult.IsSuccess)
                return DataResult<Guid>.Fail(PostgrestErrorMapper.MapRpcErrorBody(rpcResult.ErrorBody));

            var scheduleId = rpcResult.Value;
            if (scheduleId == Guid.Empty)
                return DataResult<Guid>.Fail(DataError.Unknown("create_schedule_entry не вернул id"));

            return DataResult<Guid>.Ok(scheduleId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<Guid>.Fail(DataError.Network(ex.Message));
        }
        catch (Exception ex)
        {
            return DataResult<Guid>.Fail(DataError.Unknown(ex.Message));
        }
    }

    public async Task<DataResult<int>> GeneratePprScheduleAsync(
        int horizonDays = 30, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult<int>.Fail(DataError.Unauthorized());

        try
        {
            var count = await SupabaseRestClient.CallRpcScalarAsync<int?>(
                _clientProvider,
                "generate_ppr_schedule",
                new { p_horizon_days = horizonDays },
                cancellationToken);

            return DataResult<int>.Ok(count ?? 0);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<int>.Fail(DataError.Network(ex.Message));
        }
        catch (Exception ex)
        {
            return DataResult<int>.Fail(DataError.Unknown(ex.Message));
        }
    }

    public async Task<DataResult> CreateWorkReportAsync(
        WorkReportInput input, CancellationToken cancellationToken = default)
    {
        if (input.ScheduleId is null || input.ScheduleId == Guid.Empty)
            return DataResult.Fail(DataError.Validation("WorkReport_Error_ScheduleRequired"));

        var profile = _authService.CurrentProfile;
        if (profile is null)
            return DataResult.Fail(DataError.Unauthorized());

        var scheduleResult = await _scheduleRepo.ListAsync(cancellationToken);
        if (!scheduleResult.IsSuccess)
            return DataResult.Fail(scheduleResult.Error!);

        var schedule = scheduleResult.Value!.FirstOrDefault(s => s.Id == input.ScheduleId);
        if (schedule is null)
            return DataResult.Fail(DataError.Validation("WorkReport_Error_ScheduleNotFound"));

        if (schedule.Status is not "in_progress")
            return DataResult.Fail(DataError.Validation("WorkReport_Error_ScheduleNotInProgress"));

        var deptResult = await _scheduleRepo.ListDepartmentsAsync(cancellationToken);
        var assignedDeptIds = deptResult.IsSuccess
            ? deptResult.Value!
                .Where(d => d.ScheduleId == input.ScheduleId)
                .Select(d => d.RepairDepartmentId)
                .ToList()
            : [];

        if (assignedDeptIds.Count == 0)
            return DataResult.Fail(DataError.Validation("WorkReport_Error_NoDepartments"));

        var repairDepartmentId = ResolveWorkReportDepartmentId(profile, input.RepairDepartmentId);
        if (repairDepartmentId is null)
            return DataResult.Fail(DataError.Validation("WorkReport_Error_DepartmentRequired"));

        if (!assignedDeptIds.Contains(repairDepartmentId.Value))
            return DataResult.Fail(DataError.Validation("WorkReport_Error_DepartmentNotAssigned"));

        var existingResult = await _workReportRepository.ListByScheduleAsync(input.ScheduleId.Value, cancellationToken);
        if (existingResult.IsSuccess
            && existingResult.Value!.Any(r => r.RepairDepartmentId == repairDepartmentId))
            return DataResult.Fail(DataError.Validation("WorkReport_Error_Duplicate"));

        var row = new Repositories.Dtos.WorkReportInsertDto
        {
            ScheduleId = input.ScheduleId,
            RepairDepartmentId = repairDepartmentId.Value,
            AuthorId = profile.Id,
            TechnicianId = input.TechnicianId,
            WorkPerformed = input.WorkPerformed.Trim(),
            ActualDurationHours = input.ActualDurationHours,
            PartsUsed = input.PartsUsed,
            DefectsFound = string.IsNullOrWhiteSpace(input.DefectsFound) ? null : input.DefectsFound.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };

        return await _workReportRepository.InsertAsync(row, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkReportItem>> GetWorkReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _workReportRepository.ListAllAsync(cancellationToken);
        if (!result.IsSuccess)
            return [];

        return result.Value!.Select(MapListRow).ToList();
    }

    public async Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForScheduleAsync(
        Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var result = await _workReportRepository.ListByScheduleAsync(scheduleId, cancellationToken);
        if (!result.IsSuccess)
            return [];

        return result.Value!.Select(MapRow).ToList();
    }

    private static WorkReportItem MapRow(Repositories.Dtos.WorkReportRowDto r) => new()
    {
        Id = r.Id,
        RequestId = r.RequestId,
        ScheduleId = r.ScheduleId,
        RepairDepartmentId = r.RepairDepartmentId,
        RepairDepartmentName = r.RepairDepartments?.Name,
        TechnicianName = r.Technicians?.FullName ?? string.Empty,
        WorkPerformed = r.WorkPerformed ?? string.Empty,
        ActualDurationHours = r.ActualDurationHours,
        DefectsFound = r.DefectsFound,
        Notes = r.Notes,
        MaintenanceType = r.MaintenanceType,
        MaintenanceTypes = r.MaintenanceTypes,
        PartsUsed = WorkReportPartsUsedFormatter.Format(r.PartsUsed),
        CreatedAt = r.CreatedAt,
    };

    private static WorkReportItem MapListRow(Repositories.Dtos.WorkReportListRowDto r) => new()
    {
        Id = r.Id,
        RequestId = r.RequestId,
        ScheduleId = r.ScheduleId,
        RepairDepartmentId = r.RepairDepartmentId,
        RepairDepartmentName = r.RepairDepartments?.Name,
        TechnicianName = r.Technicians?.FullName ?? string.Empty,
        WorkPerformed = r.WorkPerformed ?? string.Empty,
        ActualDurationHours = r.ActualDurationHours,
        DefectsFound = r.DefectsFound,
        Notes = r.Notes,
        MaintenanceType = r.MaintenanceType,
        MaintenanceTypes = r.MaintenanceTypes,
        PartsUsed = WorkReportPartsUsedFormatter.Format(r.PartsUsed),
        CreatedAt = r.CreatedAt,
        RequestNumber = r.Requests?.RequestNumber,
        ScheduleAssetName = r.MaintenanceSchedule?.Assets?.Name,
        ScheduleAssetNumber = r.MaintenanceSchedule?.Assets?.AssetNumber,
        ScheduleMaintenanceType = r.MaintenanceSchedule?.MaintenanceType,
    };

    private static Guid? ResolveWorkReportDepartmentId(UserProfile profile, Guid? inputDepartmentId)
    {
        if (profile.Role == UserRole.Admin)
            return inputDepartmentId is { } id && id != Guid.Empty ? id : null;

        return profile.RepairDepartmentId;
    }

    // -------------------------------------------------------------------
    // UC-A5: категории оборудования (пресеты)
    // -------------------------------------------------------------------

    public async Task<DataResult<IReadOnlyList<EquipmentCategoryListItem>>> GetCategoriesAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await _categoryRepo.ListAsync(includeInactive, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<IReadOnlyList<EquipmentCategoryListItem>>.Fail(result.Error!);

        var items = result.Value!.Select(MapCategory).ToList();
        return DataResult<IReadOnlyList<EquipmentCategoryListItem>>.Ok(items);
    }

    public async Task<DataResult<IReadOnlyList<CategoryNormSlot>>> GetCategoryNormsAsync(
        Guid categoryId, CancellationToken cancellationToken = default)
    {
        var normsResult = await _categoryNormRepo.ListByCategoryAsync(categoryId, cancellationToken);
        if (!normsResult.IsSuccess)
            return DataResult<IReadOnlyList<CategoryNormSlot>>.Fail(normsResult.Error!);

        var normsByType = normsResult.Value!.ToDictionary(n => n.MaintenanceType);
        var slots = new List<CategoryNormSlot>();

        foreach (var type in MaintenanceTypes)
        {
            if (!normsByType.TryGetValue(type, out var norm))
            {
                slots.Add(new CategoryNormSlot { MaintenanceType = type, IsEnabled = false });
                continue;
            }

            var deptResult = await _categoryNormDeptRepo.GetValuesAsync(
                "category_norm_id", norm.Id, m => m.RepairDepartmentId, cancellationToken);

            slots.Add(new CategoryNormSlot
            {
                MaintenanceType = type,
                IsEnabled = true,
                NormId = norm.Id,
                IntervalDays = norm.IntervalDays,
                Description = norm.Description,
                DepartmentIds = deptResult.IsSuccess ? deptResult.Value! : [],
            });
        }

        return DataResult<IReadOnlyList<CategoryNormSlot>>.Ok(slots);
    }

    public async Task<DataResult<EquipmentCategoryListItem>> CreateCategoryAsync(
        EquipmentCategoryEditInput input, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult<EquipmentCategoryListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateCategoryInput(input);
        if (validation is not null)
            return DataResult<EquipmentCategoryListItem>.Fail(validation);

        var model = new EquipmentCategoryModel
        {
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            IsActive = true,
        };

        var result = await _categoryRepo.InsertAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<EquipmentCategoryListItem>.Fail(result.Error!);

        return DataResult<EquipmentCategoryListItem>.Ok(MapCategory(result.Value!));
    }

    public async Task<DataResult<EquipmentCategoryListItem>> UpdateCategoryAsync(
        Guid categoryId, EquipmentCategoryEditInput input, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult<EquipmentCategoryListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateCategoryInput(input);
        if (validation is not null)
            return DataResult<EquipmentCategoryListItem>.Fail(validation);

        var existing = await _categoryRepo.GetByIdAsync(categoryId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<EquipmentCategoryListItem>.Fail(existing.Error!);

        var model = existing.Value!;
        model.Name = input.Name.Trim();
        model.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

        var result = await _categoryRepo.UpdateAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<EquipmentCategoryListItem>.Fail(result.Error!);

        return DataResult<EquipmentCategoryListItem>.Ok(MapCategory(result.Value!));
    }

    public async Task<DataResult> SaveCategoryNormsAsync(
        Guid categoryId, IReadOnlyList<CategoryNormSlotInput> slots, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult.Fail(DataError.Unauthorized());

        var existingResult = await _categoryNormRepo.ListByCategoryAsync(categoryId, cancellationToken);
        if (!existingResult.IsSuccess)
            return DataResult.Fail(existingResult.Error!);

        var existingByType = existingResult.Value!.ToDictionary(n => n.MaintenanceType);

        foreach (var slot in slots)
        {
            existingByType.TryGetValue(slot.MaintenanceType, out var existingNorm);

            if (!slot.IsEnabled)
            {
                if (existingNorm is not null)
                {
                    var delete = await _categoryNormRepo.DeleteAsync(existingNorm.Id, cancellationToken);
                    if (!delete.IsSuccess)
                        return delete;
                }
                continue;
            }

            if (slot.IntervalDays is null || slot.IntervalDays <= 0)
                return DataResult.Fail(DataError.Validation("EquipmentCategory_Validation_IntervalRequired"));

            Guid normId;
            if (existingNorm is null)
            {
                var inserted = await _categoryNormRepo.InsertAsync(new CategoryMaintenanceNormModel
                {
                    CategoryId = categoryId,
                    MaintenanceType = slot.MaintenanceType,
                    IntervalDays = slot.IntervalDays.Value,
                    Description = string.IsNullOrWhiteSpace(slot.Description) ? null : slot.Description.Trim(),
                }, cancellationToken);
                if (!inserted.IsSuccess)
                    return DataResult.Fail(inserted.Error!);
                normId = inserted.Value!.Id;
            }
            else
            {
                existingNorm.IntervalDays = slot.IntervalDays.Value;
                existingNorm.Description = string.IsNullOrWhiteSpace(slot.Description) ? null : slot.Description.Trim();
                var updated = await _categoryNormRepo.UpdateAsync(existingNorm, cancellationToken);
                if (!updated.IsSuccess)
                    return DataResult.Fail(updated.Error!);
                normId = existingNorm.Id;
            }

            var deptReplace = await _categoryNormDeptRepo.ReplaceAsync(
                "category_norm_id",
                normId,
                slot.DepartmentIds,
                (key, val) => new CategoryMaintenanceNormDepartmentModel { CategoryNormId = key, RepairDepartmentId = val },
                cancellationToken);
            if (!deptReplace.IsSuccess)
                return deptReplace;
        }

        return DataResult.Ok();
    }

    // -------------------------------------------------------------------
    // UC-A5: нормативы объекта (effective = COALESCE(override, preset))
    // -------------------------------------------------------------------

    public async Task<DataResult<AssetNormsDetail>> GetAssetNormsDetailAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var assetResult = await _assetRepo.GetByIdAsync(assetId, cancellationToken);
        if (!assetResult.IsSuccess)
            return DataResult<AssetNormsDetail>.Fail(assetResult.Error!);
        var asset = assetResult.Value!;

        string? categoryName = null;
        IReadOnlyList<CategoryMaintenanceNormModel> categoryNorms = [];
        if (asset.CategoryId is Guid categoryId)
        {
            var categoryResult = await _categoryRepo.GetByIdAsync(categoryId, cancellationToken);
            if (categoryResult.IsSuccess)
                categoryName = categoryResult.Value!.Name;

            var categoryNormsResult = await _categoryNormRepo.ListByCategoryAsync(categoryId, cancellationToken);
            if (categoryNormsResult.IsSuccess)
                categoryNorms = categoryNormsResult.Value!;
        }

        var overridesResult = await _normRepo.ListByAssetAsync(assetId, cancellationToken);
        if (!overridesResult.IsSuccess)
            return DataResult<AssetNormsDetail>.Fail(overridesResult.Error!);

        var statusResult = await _statusRepo.ListByAssetAsync(assetId, cancellationToken);
        var statusByType = statusResult.IsSuccess
            ? statusResult.Value!.ToDictionary(s => s.MaintenanceType)
            : new Dictionary<string, AssetMaintenanceStatusModel>();

        var categoryNormsByType = categoryNorms.ToDictionary(n => n.MaintenanceType);
        var overridesByType = overridesResult.Value!.ToDictionary(n => n.MaintenanceType);

        var slots = new List<EffectiveNormSlot>();
        foreach (var type in MaintenanceTypes)
        {
            categoryNormsByType.TryGetValue(type, out var categoryNorm);
            overridesByType.TryGetValue(type, out var overrideNorm);
            statusByType.TryGetValue(type, out var status);

            IReadOnlyList<Guid> presetDepartmentIds = [];
            if (categoryNorm is not null)
            {
                var presetDeptResult = await _categoryNormDeptRepo.GetValuesAsync(
                    "category_norm_id", categoryNorm.Id, m => m.RepairDepartmentId, cancellationToken);
                if (presetDeptResult.IsSuccess)
                    presetDepartmentIds = presetDeptResult.Value!;
            }

            IReadOnlyList<Guid> overrideDepartmentIds = [];
            if (overrideNorm is not null)
            {
                var overrideDeptResult = await _normDeptRepo.GetValuesAsync(
                    "norm_id", overrideNorm.Id, m => m.RepairDepartmentId, cancellationToken);
                if (overrideDeptResult.IsSuccess)
                    overrideDepartmentIds = overrideDeptResult.Value!;
            }

            var effectiveInterval = overrideNorm?.IntervalDays ?? categoryNorm?.IntervalDays;
            var effectiveDescription = overrideNorm?.Description ?? categoryNorm?.Description;
            var effectiveDepartmentIds = overrideNorm?.OverrideDepartments == true
                ? overrideDepartmentIds
                : presetDepartmentIds;

            PendingScheduleInfo? pending = null;
            var pendingRows = await SupabaseRestClient.CallRpcAsync<PendingScheduleRpcRow>(
                _clientProvider, "get_pending_schedule_entry",
                new { p_asset_id = assetId, p_maintenance_type = type }, cancellationToken);
            var pendingRow = pendingRows?.FirstOrDefault();
            if (pendingRow is not null)
            {
                pending = new PendingScheduleInfo
                {
                    PlannedDate = pendingRow.PlannedDate,
                    Status = pendingRow.ScheduleStatus,
                };
            }

            slots.Add(new EffectiveNormSlot
            {
                MaintenanceType = type,
                IsEnabled = effectiveInterval is not null,
                PresetIntervalDays = categoryNorm?.IntervalDays,
                PresetDescription = categoryNorm?.Description,
                PresetDepartmentIds = presetDepartmentIds,
                OverrideNormId = overrideNorm?.Id,
                OverrideIntervalDays = overrideNorm?.IntervalDays,
                OverrideDescription = overrideNorm?.Description,
                OverrideDepartments = overrideNorm?.OverrideDepartments ?? false,
                OverrideDepartmentIds = overrideDepartmentIds,
                EffectiveIntervalDays = effectiveInterval,
                EffectiveDescription = effectiveDescription,
                EffectiveDepartmentIds = effectiveDepartmentIds,
                IsIntervalOverridden = overrideNorm?.IntervalDays is not null,
                IsDescriptionOverridden = overrideNorm?.Description is not null,
                LastMaintenanceDate = status?.LastMaintenanceDate,
                NextMaintenanceDate = status?.NextMaintenanceDate,
                PendingSchedule = pending,
            });
        }

        return DataResult<AssetNormsDetail>.Ok(new AssetNormsDetail
        {
            AssetId = asset.Id,
            AssetNumber = asset.AssetNumber,
            AssetName = asset.Name,
            CategoryId = asset.CategoryId,
            CategoryName = categoryName,
            CommissioningDate = asset.CommissioningDate,
            Slots = slots,
        });
    }

    public async Task<DataResult> SaveAssetNormOverridesAsync(
        AssetNormOverridesInput input, CancellationToken cancellationToken = default)
    {
        if (!IsAdmin())
            return DataResult.Fail(DataError.Unauthorized());

        var existingResult = await _normRepo.ListByAssetAsync(input.AssetId, cancellationToken);
        if (!existingResult.IsSuccess)
            return DataResult.Fail(existingResult.Error!);

        var existingByType = existingResult.Value!.ToDictionary(n => n.MaintenanceType);

        foreach (var slot in input.Slots)
        {
            existingByType.TryGetValue(slot.MaintenanceType, out var existingNorm);

            if (!slot.HasOverride)
            {
                if (existingNorm is not null)
                {
                    var delete = await _normRepo.DeleteAsync(existingNorm.Id, cancellationToken);
                    if (!delete.IsSuccess)
                        return delete;
                }
                continue;
            }

            Guid normId;
            if (existingNorm is null)
            {
                var inserted = await _normRepo.InsertAsync(new MaintenanceNormModel
                {
                    AssetId = input.AssetId,
                    MaintenanceType = slot.MaintenanceType,
                    IntervalDays = slot.IntervalDays,
                    Description = string.IsNullOrWhiteSpace(slot.Description) ? null : slot.Description.Trim(),
                    OverrideDepartments = slot.OverrideDepartments,
                }, cancellationToken);
                if (!inserted.IsSuccess)
                    return DataResult.Fail(inserted.Error!);
                normId = inserted.Value!.Id;
            }
            else
            {
                existingNorm.IntervalDays = slot.IntervalDays;
                existingNorm.Description = string.IsNullOrWhiteSpace(slot.Description) ? null : slot.Description.Trim();
                existingNorm.OverrideDepartments = slot.OverrideDepartments;
                var updated = await _normRepo.UpdateAsync(existingNorm, cancellationToken);
                if (!updated.IsSuccess)
                    return DataResult.Fail(updated.Error!);
                normId = existingNorm.Id;
            }

            if (slot.OverrideDepartments)
            {
                var deptReplace = await _normDeptRepo.ReplaceAsync(
                    "norm_id",
                    normId,
                    slot.DepartmentIds,
                    (key, val) => new MaintenanceNormDepartmentModel { NormId = key, RepairDepartmentId = val },
                    cancellationToken);
                if (!deptReplace.IsSuccess)
                    return deptReplace;
            }

            if (slot.Policy is NormChangePolicy policy)
            {
                var policyValue = policy switch
                {
                    NormChangePolicy.RecalculatePending => "recalculate_pending",
                    NormChangePolicy.NextCycleOnly => "next_cycle_only",
                    _ => "norm_only",
                };

                await SupabaseRestClient.CallRpcVoidAsync(
                    _clientProvider, "sync_schedule_after_norm_change",
                    new { p_asset_id = input.AssetId, p_maintenance_type = slot.MaintenanceType, p_policy = policyValue },
                    cancellationToken);
            }
        }

        return DataResult.Ok();
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceNormItem>>> GetAllEffectiveNormsAsync(
        CancellationToken cancellationToken = default)
    {
        var statusResult = await _statusRepo.ListAllAsync(cancellationToken);
        if (!statusResult.IsSuccess)
            return DataResult<IReadOnlyList<MaintenanceNormItem>>.Fail(statusResult.Error!);

        var assetsResult = await _assetRepo.ListAsync(includeDecommissioned: true, cancellationToken);
        var assetsById = assetsResult.IsSuccess
            ? assetsResult.Value!.ToDictionary(a => a.Id)
            : new Dictionary<Guid, AssetModel>();

        var categoriesResult = await _categoryRepo.ListAsync(includeInactive: true, cancellationToken);
        var categoriesById = categoriesResult.IsSuccess
            ? categoriesResult.Value!.ToDictionary(c => c.Id)
            : new Dictionary<Guid, EquipmentCategoryModel>();

        var overridesResult = await _normRepo.ListAllAsync(cancellationToken);
        var overrideKeys = overridesResult.IsSuccess
            ? overridesResult.Value!
                .Where(n => n.IntervalDays is not null)
                .Select(n => (n.AssetId, n.MaintenanceType))
                .ToHashSet()
            : [];

        var items = new List<MaintenanceNormItem>();
        foreach (var status in statusResult.Value!)
        {
            if (!assetsById.TryGetValue(status.AssetId, out var asset))
                continue;

            string? categoryName = asset.CategoryId is Guid catId && categoriesById.TryGetValue(catId, out var cat)
                ? cat.Name
                : null;

            items.Add(new MaintenanceNormItem
            {
                AssetId = asset.Id,
                AssetNumber = asset.AssetNumber,
                AssetName = asset.Name,
                CategoryName = categoryName,
                MaintenanceType = status.MaintenanceType,
                IntervalDays = status.IntervalDays,
                IsIntervalOverridden = overrideKeys.Contains((status.AssetId, status.MaintenanceType)),
                NextMaintenanceDate = status.NextMaintenanceDate,
            });
        }

        return DataResult<IReadOnlyList<MaintenanceNormItem>>.Ok(
            items.OrderBy(i => i.AssetNumber).ThenBy(i => i.MaintenanceType).ToList());
    }

    public async Task<DataResult<bool>> HasPendingScheduleAsync(
        Guid assetId, string maintenanceType, CancellationToken cancellationToken = default)
    {
        var value = await SupabaseRestClient.CallRpcScalarAsync<bool?>(
            _clientProvider, "has_pending_schedule",
            new { p_asset_id = assetId, p_maintenance_type = maintenanceType }, cancellationToken);

        return DataResult<bool>.Ok(value ?? false);
    }

    private async Task MarkOverdueScheduleItemsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SupabaseRestClient.CallRpcScalarAsync<int?>(
                _clientProvider,
                "mark_overdue_schedule_items",
                new { },
                cancellationToken);
        }
        catch
        {
            // best effort — не блокируем загрузку графика при сбое RPC
        }
    }

    private async Task<List<Guid>> ResolveDispatcherScheduleDepartmentsAsync(
        Guid assetId, string maintenanceType, CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();

        var normDeptIds = await SupabaseRestClient.CallRpcScalarAsync<Guid[]>(
            _clientProvider,
            "get_effective_norm_departments",
            new { p_asset_id = assetId, p_maintenance_type = maintenanceType },
            cancellationToken);

        if (normDeptIds is { Length: > 0 })
        {
            foreach (var id in normDeptIds)
                ids.Add(id);
        }

        var dispatcherDeptId = _authService.CurrentProfile?.RepairDepartmentId;
        if (dispatcherDeptId is Guid deptId && deptId != Guid.Empty)
            ids.Add(deptId);

        return ids.ToList();
    }

    private bool IsAdmin() => _authService.CurrentProfile?.Role == UserRole.Admin;

    private static DataError? ValidateCategoryInput(EquipmentCategoryEditInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            return DataError.Validation("EquipmentCategory_Validation_NameRequired");

        return null;
    }

    private static EquipmentCategoryListItem MapCategory(EquipmentCategoryModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description,
        IsActive = model.IsActive,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt,
    };

    private static readonly string[] MaintenanceTypes = ["to1", "to2", "kr"];

    private sealed class PendingScheduleRpcRow
    {
        [JsonPropertyName("planned_date")]
        public DateOnly PlannedDate { get; set; }

        [JsonPropertyName("schedule_status")]
        public string ScheduleStatus { get; set; } = string.Empty;
    }
}
