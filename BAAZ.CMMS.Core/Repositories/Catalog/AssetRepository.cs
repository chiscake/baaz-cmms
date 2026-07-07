using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly ISupabaseGateway _gateway;

    public AssetRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<AssetModel>>> ListAsync(
        bool includeDecommissioned = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _gateway.From<AssetModel>().Order("asset_number", Ordering.Ascending);
            if (!includeDecommissioned)
                query = query.Filter("status", Operator.NotEqual, "decommissioned");

            var response = await query.Get(ct);

            return DataResult<IReadOnlyList<AssetModel>>.Ok(
                (response.Models ?? []).AsReadOnly());
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<AssetModel>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<AssetModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<AssetModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<AssetModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<AssetModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<AssetModel>.Ok(response);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<AssetModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<AssetModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<AssetModel>> InsertAsync(AssetModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<AssetModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<AssetModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<AssetModel>.Ok(inserted);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<AssetModel>.Fail(MapAssetError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<AssetModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<AssetModel>> UpdateAsync(AssetModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<AssetModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<AssetModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<AssetModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<AssetModel>.Fail(MapAssetError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<AssetModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> SetStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<AssetModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Set(x => x.Status, status)
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
            await _gateway.From<AssetModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Delete();

            return DataResult.Ok();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult.Fail(MapAssetError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    private static DataError MapAssetError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (IsUniqueViolation(ex))
            return DataError.Validation("Assets_Error_DuplicateNumber", ex.Message);

        if (IsForeignKeyViolation(ex))
            return DataError.Validation("Assets_Error_HasRequests", ex.Message);

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

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ex.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        return DataError.Unknown(ex.Message);
    }
}
