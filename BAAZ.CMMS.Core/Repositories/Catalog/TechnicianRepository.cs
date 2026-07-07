using System.Diagnostics;

using System;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class TechnicianRepository : ITechnicianRepository
{
    private readonly ISupabaseGateway _gateway;

    public TechnicianRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<TechnicianModel>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TechnicianModel>()
                .Order("full_name", Ordering.Ascending)
                .Get(ct);

            var models = response.Models ?? [];
            Debug.WriteLine($"[TechnicianRepository] ListAsync: {models.Count} row(s) from API");
            if (models.Count > 0)
                Debug.WriteLine($"[TechnicianRepository] First: id={models[0].Id}, name={models[0].FullName}");

            return DataResult<IReadOnlyList<TechnicianModel>>.Ok(models.AsReadOnly());
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<TechnicianModel>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<TechnicianModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TechnicianModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TechnicianModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<TechnicianModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<TechnicianModel>.Ok(response);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TechnicianModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TechnicianModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TechnicianModel>> InsertAsync(TechnicianModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TechnicianModel>().Insert(model);

            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<TechnicianModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<TechnicianModel>.Ok(inserted);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TechnicianModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TechnicianModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TechnicianModel>> UpdateAsync(TechnicianModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TechnicianModel>().Update(model);

            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<TechnicianModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<TechnicianModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TechnicianModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TechnicianModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<TechnicianModel>()
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
            await _gateway.From<TechnicianModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Delete();

            return DataResult.Ok();
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            if (IsForeignKeyViolation(ex))
                return DataResult.Fail(DataError.Validation("Personnel_Delete_Referenced", ex.Message));

            return DataResult.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
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
