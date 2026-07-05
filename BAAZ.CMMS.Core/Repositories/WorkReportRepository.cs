using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;
using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class WorkReportRepository : IWorkReportRepository
{
    private const string RowSelect =
        "id,request_id,schedule_id,repair_department_id,maintenance_type,maintenance_types,work_performed,actual_duration_hours,defects_found,notes,created_at," +
        "technicians(full_name),repair_departments(name)";

    private const string ListSelect =
        "id,request_id,schedule_id,repair_department_id,maintenance_type,work_performed,actual_duration_hours,defects_found,notes,created_at," +
        "technicians(full_name),repair_departments(name)," +
        "requests(request_number)," +
        "maintenance_schedule(maintenance_type,assets(name,asset_number))";

    private readonly ISupabaseClientProvider _clientProvider;

    public WorkReportRepository(ISupabaseClientProvider clientProvider)
    {
        _clientProvider = clientProvider;
    }

    public async Task<DataResult> InsertAsync(
        WorkReportInsertDto row, CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, errorBody) = await SupabaseRestClient.PostOrErrorAsync(
                _clientProvider,
                "/rest/v1/work_reports",
                row,
                cancellationToken);

            if (success)
                return DataResult.Ok();

            var mapped = PostgrestErrorMapper.MapPostErrorBody(errorBody);
            return DataResult.Fail(mapped);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByRequestAsync(
        Guid requestId, CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/work_reports" +
            $"?request_id=eq.{requestId:D}" +
            $"&select={RowSelect}" +
            "&order=created_at.asc";

        return GetListResultAsync<WorkReportRowDto>(path, cancellationToken);
    }

    public Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByScheduleAsync(
        Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/work_reports" +
            $"?schedule_id=eq.{scheduleId:D}" +
            $"&select={RowSelect}" +
            "&order=created_at.asc";

        return GetListResultAsync<WorkReportRowDto>(path, cancellationToken);
    }

    public async Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListByScheduleIdsAsync(
        IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken = default)
    {
        if (scheduleIds.Count == 0)
            return DataResult<IReadOnlyList<WorkReportRowDto>>.Ok([]);

        var idList = string.Join(",", scheduleIds.Select(id => id.ToString("D")));
        var path =
            "/rest/v1/work_reports" +
            $"?schedule_id=in.({idList})" +
            "&select=id,schedule_id,repair_department_id,work_performed,actual_duration_hours,defects_found,notes,created_at," +
            "technicians(full_name),repair_departments(name)" +
            "&order=created_at.asc";

        return await GetListResultAsync<WorkReportRowDto>(path, cancellationToken);
    }

    public Task<DataResult<IReadOnlyList<WorkReportListRowDto>>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/work_reports" +
            $"?select={ListSelect}" +
            "&order=created_at.desc";

        return GetListResultAsync<WorkReportListRowDto>(path, cancellationToken);
    }

    private async Task<DataResult<IReadOnlyList<T>>> GetListResultAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await SupabaseRestClient.GetListAsync<T>(
                _clientProvider,
                path,
                cancellationToken);

            return rows is null
                ? DataResult<IReadOnlyList<T>>.Fail(DataError.Unknown("Не удалось загрузить отчёты о работах"))
                : DataResult<IReadOnlyList<T>>.Ok(rows);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<T>>.Fail(DataError.Network(ex.Message));
        }
    }
}
