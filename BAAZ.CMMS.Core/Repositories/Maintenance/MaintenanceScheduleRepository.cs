using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class MaintenanceScheduleRepository : IMaintenanceScheduleRepository
{
    private readonly ISupabaseGateway _gateway;

    public MaintenanceScheduleRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceScheduleModel>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceScheduleModel>()
                .Order("planned_date", Ordering.Ascending)
                .Get(ct);

            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceScheduleModel>>> ListByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceScheduleModel>()
                .Filter("planned_date", Operator.GreaterThanOrEqual, from.ToString("yyyy-MM-dd"))
                .Filter("planned_date", Operator.LessThanOrEqual, to.ToString("yyyy-MM-dd"))
                .Order("planned_date", Ordering.Ascending)
                .Get(ct);

            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceScheduleDepartmentModel>>> ListDepartmentsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceScheduleDepartmentModel>().Get(ct);
            return DataResult<IReadOnlyList<MaintenanceScheduleDepartmentModel>>.Ok(
                (response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleDepartmentModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<MaintenanceScheduleDepartmentModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<MaintenanceScheduleModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Set(x => x.Status, status)
                .Update();

            return DataResult.Ok();
        }
        catch (PostgrestException ex)
        {
            return DataResult.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }
}
