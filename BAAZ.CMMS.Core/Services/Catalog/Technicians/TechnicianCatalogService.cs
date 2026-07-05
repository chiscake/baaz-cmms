using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.Catalog;

public sealed class TechnicianCatalogService : ITechnicianCatalogService
{
    private readonly ITechnicianRepository _technicianRepo;
    private readonly IRepairDepartmentRepository _deptRepo;
    private readonly IAuthService _authService;

    public TechnicianCatalogService(
        ITechnicianRepository technicianRepo,
        IRepairDepartmentRepository deptRepo,
        IAuthService authService)
    {
        _technicianRepo = technicianRepo;
        _deptRepo = deptRepo;
        _authService = authService;
    }

    public async Task<DataResult<IReadOnlyList<TechnicianListItem>>> GetTechniciansAsync(
        CancellationToken cancellationToken = default)
    {
        var techResult = await _technicianRepo.ListAsync(cancellationToken);
        if (!techResult.IsSuccess)
            return DataResult<IReadOnlyList<TechnicianListItem>>.Fail(techResult.Error!);

        var deptResult = await _deptRepo.ListAsync(includeInactive: true, cancellationToken);
        var deptById = deptResult.IsSuccess
            ? deptResult.Value!.ToDictionary(d => d.Id)
            : [];

        var items = techResult.Value!.Select(t => new TechnicianListItem
        {
            Id = t.Id,
            FullName = t.FullName,
            Specialty = t.Specialty,
            IsActive = t.IsActive,
            RepairDepartmentId = t.RepairDepartmentId,
            RepairDepartmentName = t.RepairDepartmentId.HasValue && deptById.TryGetValue(t.RepairDepartmentId.Value, out var d)
                ? d.Name
                : null,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
        }).ToList();

        return DataResult<IReadOnlyList<TechnicianListItem>>.Ok(items);
    }

    public async Task<DataResult<TechnicianListItem>> CreateTechnicianAsync(
        TechnicianEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            return DataResult<TechnicianListItem>.Fail(
                DataError.Validation("Personnel_Validation_FullNameRequired"));

        var deptId = ResolveDepartmentId(input.RepairDepartmentId, out var validationError);
        if (validationError is not null)
            return DataResult<TechnicianListItem>.Fail(validationError);

        var model = new TechnicianModel
        {
            FullName = input.FullName.Trim(),
            Specialty = input.Specialty?.Trim() ?? string.Empty,
            IsActive = true,
            RepairDepartmentId = deptId,
        };

        var result = await _technicianRepo.InsertAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<TechnicianListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedTechnicianAsync(result.Value!, cancellationToken);
        return DataResult<TechnicianListItem>.Ok(await EnrichAsync(persisted, cancellationToken));
    }

    public async Task<DataResult<TechnicianListItem>> UpdateTechnicianAsync(
        Guid technicianId,
        TechnicianEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            return DataResult<TechnicianListItem>.Fail(
                DataError.Validation("Personnel_Validation_FullNameRequired"));

        var existing = await _technicianRepo.GetByIdAsync(technicianId, cancellationToken);
        if (!existing.IsSuccess)
            return DataResult<TechnicianListItem>.Fail(existing.Error!);

        var profile = _authService.CurrentProfile;
        Guid? deptId;

        if (profile?.Role == UserRole.Admin)
            deptId = input.RepairDepartmentId ?? existing.Value!.RepairDepartmentId;
        else
            deptId = existing.Value!.RepairDepartmentId;

        var model = existing.Value!;
        model.FullName = input.FullName.Trim();
        model.Specialty = input.Specialty?.Trim() ?? string.Empty;
        model.RepairDepartmentId = deptId;

        var result = await _technicianRepo.UpdateAsync(model, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<TechnicianListItem>.Fail(result.Error!);

        var persisted = await ResolvePersistedTechnicianAsync(result.Value!, cancellationToken);
        return DataResult<TechnicianListItem>.Ok(await EnrichAsync(persisted, cancellationToken));
    }

    public Task<DataResult> SetTechnicianActiveAsync(
        Guid technicianId,
        bool isActive,
        CancellationToken cancellationToken = default)
        => _technicianRepo.SetActiveAsync(technicianId, isActive, cancellationToken);

    public Task<DataResult> DeleteTechnicianAsync(
        Guid technicianId,
        CancellationToken cancellationToken = default)
    {
        if (_authService.CurrentProfile?.Role != UserRole.Admin)
            return Task.FromResult(DataResult.Fail(DataError.Unauthorized()));

        return _technicianRepo.DeleteAsync(technicianId, cancellationToken);
    }

    private Guid? ResolveDepartmentId(Guid? inputDeptId, out DataError? error)
    {
        error = null;
        var profile = _authService.CurrentProfile;

        if (profile?.Role == UserRole.Dispatcher)
            return profile.RepairDepartmentId;

        if (profile?.Role == UserRole.Admin)
        {
            if (!inputDeptId.HasValue)
            {
                error = DataError.Validation("Personnel_Validation_DepartmentRequired");
                return null;
            }

            return inputDeptId;
        }

        error = DataError.Unauthorized();
        return null;
    }

    private async Task<TechnicianModel> ResolvePersistedTechnicianAsync(
        TechnicianModel model,
        CancellationToken ct)
    {
        if (model.CreatedAt.HasValue && model.UpdatedAt.HasValue)
            return model;

        var reload = await _technicianRepo.GetByIdAsync(model.Id, ct);
        return reload.IsSuccess ? reload.Value! : model;
    }

    private async Task<TechnicianListItem> EnrichAsync(TechnicianModel model, CancellationToken ct)
    {
        string? deptName = null;
        if (model.RepairDepartmentId.HasValue)
        {
            var deptResult = await _deptRepo.ListAsync(includeInactive: true, ct);
            if (deptResult.IsSuccess)
            {
                deptName = deptResult.Value!
                    .FirstOrDefault(d => d.Id == model.RepairDepartmentId.Value)?.Name;
            }
        }

        return new TechnicianListItem
        {
            Id = model.Id,
            FullName = model.FullName,
            Specialty = model.Specialty,
            IsActive = model.IsActive,
            RepairDepartmentId = model.RepairDepartmentId,
            RepairDepartmentName = deptName,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
        };
    }
}
