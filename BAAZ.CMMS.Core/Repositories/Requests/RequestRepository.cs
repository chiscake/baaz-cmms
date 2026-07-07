using System.Diagnostics;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Repositories.Dtos;
using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class RequestRepository : IRequestRepository
{
    private const string DetailSelect =
        "id,request_number,title,description,type,priority,repair_zone,contractor_name,status," +
        "location_description,asset_id,inventory_id,inventory_kind,inventory_name,inventory_serial,inventory_type_name," +
        "inventory_handoff_mode,inventory_warehouse_name,inventory_received_at," +
        "requester_id,target_repair_department_id,created_at,updated_at," +
        "assets(asset_number,name)," +
        "profiles(full_name)," +
        "target_repair_department:repair_departments!requests_target_repair_department_id_fkey(name)," +
        "request_repair_departments(repair_department_id,assignee_id,added_at,repair_departments(name),technicians(full_name))";

    private readonly ISupabaseClientProvider _clientProvider;
    private readonly IWorkReportRepository _workReportRepository;

    public RequestRepository(
        ISupabaseClientProvider clientProvider,
        IWorkReportRepository workReportRepository)
    {
        _clientProvider = clientProvider;
        _workReportRepository = workReportRepository;
    }

    public async Task<DataResult<RequestCreatedDto>> CreateViaRpcAsync(
        RequestInsertDto row,
        Guid targetRepairDepartmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rpcResult = await SupabaseRestClient.CallRpcScalarOrErrorAsync<Guid>(
                _clientProvider,
                "create_request",
                new Dictionary<string, object?>
                {
                    ["p_request_number"] = row.RequestNumber,
                    ["p_type"] = row.Type,
                    ["p_priority"] = row.Priority,
                    ["p_title"] = row.Title,
                    ["p_description"] = row.Description,
                    ["p_location_description"] = row.LocationDescription,
                    ["p_asset_id"] = row.AssetId,
                    ["p_target_repair_department_id"] = targetRepairDepartmentId,
                    ["p_repair_zone"] = row.RepairZone,
                    ["p_contractor_name"] = row.ContractorName,
                },
                cancellationToken);

            if (!rpcResult.IsSuccess)
            {
                Debug.WriteLine(
                    $"[RequestRepository] CreateViaRpcAsync RPC error: {rpcResult.ErrorBody}");
                return DataResult<RequestCreatedDto>.Fail(
                    PostgrestErrorMapper.MapCreateRequestRpcErrorBody(rpcResult.ErrorBody));
            }

            var id = rpcResult.Value;
            if (id == Guid.Empty)
            {
                Debug.WriteLine("[RequestRepository] CreateViaRpcAsync: RPC returned no id");
                return DataResult<RequestCreatedDto>.Fail(DataError.Unknown("create_request не вернул id"));
            }

            return DataResult<RequestCreatedDto>.Ok(new RequestCreatedDto
            {
                Id = id,
                RequestNumber = row.RequestNumber,
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Debug.WriteLine($"[RequestRepository] CreateViaRpcAsync network error: {ex}");
            return DataResult<RequestCreatedDto>.Fail(DataError.Network(ex.Message));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RequestRepository] CreateViaRpcAsync unexpected error: {ex}");
            return DataResult<RequestCreatedDto>.Fail(DataError.Unknown(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListAllAsync(
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/requests" +
            $"?select={DetailSelect}" +
            "&order=created_at.desc";

        if (limit is > 0)
            path += $"&limit={limit.Value}";

        return await GetListResultAsync<RequestDetailRowDto>(path, cancellationToken);
    }

    public async Task<DataResult> UpdateFieldsAsync(
        Guid requestId,
        RequestPatchDto patch,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, errorBody) = await SupabaseRestClient.PatchOrErrorAsync(
                _clientProvider,
                $"/rest/v1/requests?id=eq.{requestId:D}",
                patch,
                cancellationToken);

            return success
                ? DataResult.Ok()
                : DataResult.Fail(PostgrestErrorMapper.MapRequestPatchErrorBody(errorBody));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<RequestListRowDto>>> ListByRequesterAsync(
        Guid requesterId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/requests" +
            $"?requester_id=eq.{requesterId:D}" +
            "&select=id,request_number,title,status,priority,type,created_at,updated_at," +
            "profiles(full_name)," +
            "target_repair_department:repair_departments!requests_target_repair_department_id_fkey(name)," +
            "request_repair_departments(repair_department_id,assignee_id,added_at,repair_departments(name),technicians(full_name))" +
            "&order=created_at.desc";

        if (limit is > 0)
            path += $"&limit={limit.Value}";

        return await GetListResultAsync<RequestListRowDto>(path, cancellationToken);
    }

    public async Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListIncomingAsync(
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/requests" +
            "?status=eq.new" +
            $"&select={DetailSelect}" +
            "&order=created_at.desc";

        return await GetListResultAsync<RequestDetailRowDto>(path, cancellationToken);
    }

    public async Task<DataResult<IReadOnlyList<RequestDetailRowDto>>> ListByStatusesAsync(
        IReadOnlyCollection<string> statuses,
        CancellationToken cancellationToken = default)
    {
        var statusFilter = string.Join(",", statuses);
        var path =
            "/rest/v1/requests" +
            $"?status=in.({statusFilter})" +
            $"&select={DetailSelect}" +
            "&order=updated_at.desc";

        return await GetListResultAsync<RequestDetailRowDto>(path, cancellationToken);
    }

    public async Task<DataResult<RequestDetailRowDto?>> GetDetailByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/requests" +
            $"?id=eq.{requestId:D}" +
            $"&select={DetailSelect}" +
            "&limit=1";

        var result = await GetListResultAsync<RequestDetailRowDto>(path, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<RequestDetailRowDto?>.Fail(result.Error!);

        return DataResult<RequestDetailRowDto?>.Ok(result.Value!.FirstOrDefault());
    }

    public async Task<DataResult<RequestStatusRowDto?>> GetStatusForRequesterAsync(
        Guid requestId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/requests" +
            $"?id=eq.{requestId:D}" +
            $"&requester_id=eq.{requesterId:D}" +
            "&select=id,status" +
            "&limit=1";

        var result = await GetListResultAsync<RequestStatusRowDto>(path, cancellationToken);
        if (!result.IsSuccess)
            return DataResult<RequestStatusRowDto?>.Fail(result.Error!);

        return DataResult<RequestStatusRowDto?>.Ok(result.Value!.FirstOrDefault());
    }

    public async Task<DataResult> UpdateStatusAsync(
        Guid requestId,
        string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patched = await SupabaseRestClient.PatchAsync(
                _clientProvider,
                $"/rest/v1/requests?id=eq.{requestId:D}",
                new { status },
                cancellationToken);

            return patched
                ? DataResult.Ok()
                : DataResult.Fail(DataError.Unknown("Не удалось обновить статус заявки"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> InsertStatusHistoryAsync(
        StatusHistoryInsertDto row,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var posted = await SupabaseRestClient.PostAsync(
                _clientProvider,
                "/rest/v1/request_status_history",
                row,
                cancellationToken);

            return posted
                ? DataResult.Ok()
                : DataResult.Fail(DataError.Unknown("Не удалось записать историю статуса"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<StatusHistoryRowDto>>> ListStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var path =
            "/rest/v1/request_status_history" +
            $"?request_id=eq.{requestId:D}" +
            "&select=id,old_status,new_status,comment,created_at,profiles(full_name)" +
            "&order=created_at.asc";

        return await GetListResultAsync<StatusHistoryRowDto>(path, cancellationToken);
    }

    public async Task<DataResult> CallWorkflowRpcAsync(
        string functionName,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, error) = await SupabaseRestClient.CallRpcVoidAsync(
                _clientProvider,
                functionName,
                parameters,
                cancellationToken);

            return success
                ? DataResult.Ok()
                : DataResult.Fail(PostgrestErrorMapper.MapRpcErrorBody(error));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public Task<DataResult> InsertWorkReportAsync(
        WorkReportInsertDto row,
        CancellationToken cancellationToken = default)
        => _workReportRepository.InsertAsync(row, cancellationToken);

    public Task<DataResult<IReadOnlyList<WorkReportRowDto>>> ListWorkReportsByRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
        => _workReportRepository.ListByRequestAsync(requestId, cancellationToken);

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
                ? DataResult<IReadOnlyList<T>>.Fail(DataError.Unknown("Не удалось загрузить данные"))
                : DataResult<IReadOnlyList<T>>.Ok(rows);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<T>>.Fail(DataError.Network(ex.Message));
        }
    }
}
