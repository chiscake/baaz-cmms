using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class RepairDepartmentCatalogService : IRepairDepartmentCatalogService
{
    private readonly IRepairDepartmentRepository _deptRepo;
    private readonly IAuthService _authService;

    public RepairDepartmentCatalogService(
        IRepairDepartmentRepository deptRepo,
        IAuthService authService)
    {
        _deptRepo = deptRepo;
        _authService = authService;
    }

    public async Task<DataResult<IReadOnlyList<RepairDepartmentListItem>>> GetRepairDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _deptRepo.ListAsync(includeInactive: false, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<IReadOnlyList<RepairDepartmentListItem>>.Fail(result.Error!);

        var items = result.Value!.Select(MapRepairDepartmentListItem).ToList();
        return DataResult<IReadOnlyList<RepairDepartmentListItem>>.Ok(items);
    }

    public async Task<DataResult<IReadOnlyList<RepairDepartmentAdminListItem>>> GetRepairDepartmentsAdminAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _deptRepo.ListAsync(includeInactive, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<IReadOnlyList<RepairDepartmentAdminListItem>>.Fail(result.Error!);

        var items = result.Value!.Select(MapRepairDepartmentAdminItem).ToList();
        return DataResult<IReadOnlyList<RepairDepartmentAdminListItem>>.Ok(items);
    }

    public async Task<DataResult<RepairDepartmentAdminListItem>> CreateRepairDepartmentAsync(
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<RepairDepartmentAdminListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateRepairDepartmentInput(input);
        if (validation is not null)
            return DataResult<RepairDepartmentAdminListItem>.Fail(validation);

        var model = new RepairDepartmentModel
        {
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim(),
            IsActive = true,
        };

        var result = await _deptRepo.InsertAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<RepairDepartmentAdminListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedDepartmentAsync(result.Value!, cancellationToken);
        return DataResult<RepairDepartmentAdminListItem>.Ok(MapRepairDepartmentAdminItem(persisted));
    }

    public async Task<DataResult<RepairDepartmentAdminListItem>> UpdateRepairDepartmentAsync(
        Guid departmentId,
        RepairDepartmentEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult<RepairDepartmentAdminListItem>.Fail(DataError.Unauthorized());

        var validation = ValidateRepairDepartmentInput(input);
        if (validation is not null)
            return DataResult<RepairDepartmentAdminListItem>.Fail(validation);

        var existing = await _deptRepo.GetByIdAsync(departmentId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<RepairDepartmentAdminListItem>.Fail(existing.Error!);

        var model = existing.Value!;
        model.Name = input.Name.Trim();
        model.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();

        var result = await _deptRepo.UpdateAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<RepairDepartmentAdminListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedDepartmentAsync(result.Value!, cancellationToken);
        return DataResult<RepairDepartmentAdminListItem>.Ok(MapRepairDepartmentAdminItem(persisted));
    }

    public async Task<DataResult> SetRepairDepartmentActiveAsync(
        Guid departmentId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return DataResult.Fail(DataError.Unauthorized());

        if (!isActive)
        {
            var dispatchers = await _deptRepo.HasDispatchersAsync(departmentId, cancellationToken);
            if (!dispatchers.IsSuccess)
                return DataResult.Fail(dispatchers.Error!);
            if (dispatchers.Value)
                return DataResult.Fail(DataError.Validation("RepairDepartments_Error_HasDispatchers"));

            var activeRequests = await _deptRepo.HasActiveRequestsAsync(departmentId, cancellationToken);
            if (!activeRequests.IsSuccess)
                return DataResult.Fail(activeRequests.Error!);
            if (activeRequests.Value)
                return DataResult.Fail(DataError.Validation("RepairDepartments_Error_HasActiveRequests"));
        }

        return await _deptRepo.SetActiveAsync(departmentId, isActive, cancellationToken);
    }

    public Task<DataResult> DeleteRepairDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return Task.FromResult(DataResult.Fail(DataError.Unauthorized()));

        return _deptRepo.DeleteAsync(departmentId, cancellationToken);
    }

    private static DataError? ValidateRepairDepartmentInput(RepairDepartmentEditInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            return DataError.Validation("RepairDepartments_Validation_NameRequired");

        return null;
    }

    private static RepairDepartmentListItem MapRepairDepartmentListItem(RepairDepartmentModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Code = model.Code,
    };

    private static RepairDepartmentAdminListItem MapRepairDepartmentAdminItem(RepairDepartmentModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Code = model.Code,
        IsActive = model.IsActive,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt,
    };

    private async Task<RepairDepartmentModel> ResolvePersistedDepartmentAsync(
        RepairDepartmentModel model,
        CancellationToken ct)
    {
        if (model.CreatedAt.HasValue && model.UpdatedAt.HasValue)
            return model;

        var reload = await _deptRepo.GetByIdAsync(model.Id, ct);
        return reload.IsSuccess ? reload.Value! : model;
    }
}
