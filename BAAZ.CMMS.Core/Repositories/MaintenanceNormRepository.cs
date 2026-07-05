using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class MaintenanceNormRepository : IMaintenanceNormRepository
{
    private readonly ISupabaseGateway _gateway;

    public MaintenanceNormRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceNormModel>>> ListByAssetAsync(
        Guid assetId, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceNormModel>()
                .Filter("asset_id", Operator.Equals, assetId.ToString())
                .Get(ct);

            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<MaintenanceNormModel>>> ListAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceNormModel>().Get(ct);
            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<MaintenanceNormModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<MaintenanceNormModel>> InsertAsync(
        MaintenanceNormModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceNormModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<MaintenanceNormModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<MaintenanceNormModel>.Ok(inserted);
        }
        catch (PostgrestException ex)
        {
            return DataResult<MaintenanceNormModel>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<MaintenanceNormModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<MaintenanceNormModel>> UpdateAsync(
        MaintenanceNormModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<MaintenanceNormModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<MaintenanceNormModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<MaintenanceNormModel>.Ok(updated);
        }
        catch (PostgrestException ex)
        {
            return DataResult<MaintenanceNormModel>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<MaintenanceNormModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<MaintenanceNormModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Delete();

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
