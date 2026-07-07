using System.Text.Json.Serialization;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Services;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class RepairDepartmentRepository : IRepairDepartmentRepository
{
    private readonly ISupabaseGateway _gateway;
    private readonly ISupabaseClientProvider _clientProvider;

    public RepairDepartmentRepository(ISupabaseGateway gateway, ISupabaseClientProvider clientProvider)
    {
        _gateway = gateway;
        _clientProvider = clientProvider;
    }

    public async Task<DataResult<IReadOnlyList<RepairDepartmentModel>>> ListAsync(
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _gateway.From<RepairDepartmentModel>().Order("name", Ordering.Ascending);
            if (!includeInactive)
                query = query.Filter("is_active", Operator.Equals, "true");

            var response = await query.Get(ct);

            return DataResult<IReadOnlyList<RepairDepartmentModel>>.Ok(
                (response.Models ?? []).AsReadOnly());
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<RepairDepartmentModel>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<RepairDepartmentModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<RepairDepartmentModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<RepairDepartmentModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<RepairDepartmentModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<RepairDepartmentModel>.Ok(response);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<RepairDepartmentModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<RepairDepartmentModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<RepairDepartmentModel>> InsertAsync(
        RepairDepartmentModel model,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<RepairDepartmentModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<RepairDepartmentModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<RepairDepartmentModel>.Ok(inserted);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<RepairDepartmentModel>.Fail(MapDepartmentError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<RepairDepartmentModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<RepairDepartmentModel>> UpdateAsync(
        RepairDepartmentModel model,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<RepairDepartmentModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<RepairDepartmentModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<RepairDepartmentModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<RepairDepartmentModel>.Fail(MapDepartmentError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<RepairDepartmentModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<RepairDepartmentModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Set(x => x.IsActive, isActive)
                .Update();

            return DataResult.Ok();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<RepairDepartmentModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Delete();

            return DataResult.Ok();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult.Fail(MapDepartmentError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<bool>> HasDispatchersAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<ProfileModel>()
                .Filter("repair_department_id", Operator.Equals, id.ToString())
                .Filter("role", Operator.Equals, "dispatcher")
                .Limit(1)
                .Get(ct);

            return DataResult<bool>.Ok((response.Models?.Count ?? 0) > 0);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<bool>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<bool>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<bool>> HasActiveRequestsAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var path =
                $"/rest/v1/request_repair_departments?repair_department_id=eq.{id}" +
                "&select=request_id,requests!inner(status)" +
                "&requests.status=not.in.(closed,rejected,cancelled)" +
                "&limit=1";

            var rows = await SupabaseRestClient.GetListAsync<ActiveRequestLinkRow>(
                _clientProvider,
                path,
                ct);

            if (rows is null)
                return DataResult<bool>.Fail(DataError.Unknown("Не удалось проверить заявки отдела"));

            return DataResult<bool>.Ok(rows.Count > 0);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<bool>.Fail(DataError.Network(ex.Message));
        }
    }

    private static DataError MapDepartmentError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (IsUniqueViolation(ex))
            return DataError.Validation("RepairDepartments_Error_DuplicateCode", ex.Message);

        if (IsForeignKeyViolation(ex) || IsCheckViolation(ex))
            return DataError.Validation("RepairDepartments_Error_InUse", ex.Message);

        return MapPostgrestError(ex);
    }

    private static bool IsUniqueViolation(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        var message = ex.Message;
        return message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsForeignKeyViolation(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Conflict)
            return true;

        var message = ex.Message;
        return message.Contains("23503", StringComparison.Ordinal)
            || message.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("violates foreign key constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCheckViolation(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        var message = ex.Message;
        return message.Contains("23514", StringComparison.Ordinal)
            || message.Contains("check constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ex.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        return DataError.Unknown(ex.Message);
    }

    private sealed class ActiveRequestLinkRow
    {
        [JsonPropertyName("request_id")]
        public Guid RequestId { get; init; }
    }
}
