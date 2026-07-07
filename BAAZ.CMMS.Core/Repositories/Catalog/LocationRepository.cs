using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Diagnostics;
using BAAZ.CMMS.Core.Services;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly ISupabaseGateway _gateway;
    private readonly ISupabaseClientProvider _clientProvider;

    public LocationRepository(ISupabaseGateway gateway, ISupabaseClientProvider clientProvider)
    {
        _gateway = gateway;
        _clientProvider = clientProvider;
    }

    public async Task<DataResult<IReadOnlyList<LocationModel>>> ListAsync(
        bool includeInactive = true,
        CancellationToken ct = default)
    {
        using var step = PerfDebug.Step(
            "LocationRepository.ListAsync",
            $"includeInactive={includeInactive}");
        try
        {
            var query = _gateway.From<LocationModel>().Order("name", Ordering.Ascending);
            if (!includeInactive)
                query = query.Filter("is_active", Operator.Equals, "true");

            var response = await query.Get(ct);
            var count = response.Models?.Count ?? 0;
            PerfDebug.Mark("LocationRepository.ListAsync", $"rows={count}");
            return DataResult<IReadOnlyList<LocationModel>>.Ok(
                (response.Models ?? []).AsReadOnly());
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<LocationModel>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<LocationModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<LocationModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<LocationModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<LocationModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<LocationModel>.Ok(response);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<LocationModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<LocationModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<LocationModel>> InsertAsync(LocationModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<LocationModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<LocationModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<LocationModel>.Ok(inserted);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<LocationModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<LocationModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<LocationModel>> UpdateAsync(LocationModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<LocationModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<LocationModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<LocationModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<LocationModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<LocationModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public Task<DataResult> ArchiveBranchAsync(Guid locationId, CancellationToken ct = default)
        => InvokeBranchRpcAsync("archive_location_branch", locationId, ct);

    public Task<DataResult> RestoreBranchAsync(Guid locationId, CancellationToken ct = default)
        => InvokeBranchRpcAsync("restore_location_branch", locationId, ct);

    public Task<DataResult> HardDeleteBranchAsync(Guid locationId, CancellationToken ct = default)
        => InvokeBranchRpcAsync("hard_delete_location_branch", locationId, ct);

    private async Task<DataResult> InvokeBranchRpcAsync(
        string functionName,
        Guid locationId,
        CancellationToken ct)
    {
        using var step = PerfDebug.Step(
            "LocationRepository.Rpc",
            $"{functionName} id={locationId}");
        try
        {
            await _clientProvider.Client.Rpc(
                functionName,
                new Dictionary<string, object> { { "p_location_id", locationId } });

            return DataResult.Ok();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult.Fail(MapLocationBranchError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    private static DataError MapLocationBranchError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        var message = ex.Message;
        if (message.Contains("LOCATIONS_HAS_PROFILES", StringComparison.Ordinal))
            return DataError.Validation("Locations_Error_HasProfiles", message);

        if (message.Contains("LOCATIONS_HAS_ASSETS", StringComparison.Ordinal))
            return DataError.Validation("Locations_Error_HasAssets", message);

        if (IsForeignKeyViolation(ex))
            return DataError.Validation("Locations_Error_HasAssets", message);

        return MapPostgrestError(ex);
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

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ex.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        if (IsUniqueViolation(ex) && IsLocationsCodeConstraint(ex.Message))
            return DataError.Validation("Locations_Validation_CodeDuplicate", ex.Message);

        return DataError.Unknown(ex.Message);
    }

    private static bool IsUniqueViolation(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Conflict)
            return true;

        var message = ex.Message;
        return message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocationsCodeConstraint(string message) =>
        message.Contains("locations_code", StringComparison.OrdinalIgnoreCase)
        || (message.Contains("locations", StringComparison.OrdinalIgnoreCase)
            && message.Contains("code", StringComparison.OrdinalIgnoreCase));
}
