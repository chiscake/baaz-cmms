using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class AssetMaintenanceStatusRepository : IAssetMaintenanceStatusRepository
{
    private readonly ISupabaseGateway _gateway;

    public AssetMaintenanceStatusRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>> ListByAssetAsync(
        Guid assetId, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<AssetMaintenanceStatusModel>()
                .Filter("asset_id", Operator.Equals, assetId.ToString())
                .Get(ct);

            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>> ListAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<AssetMaintenanceStatusModel>().Get(ct);
            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<AssetMaintenanceStatusModel>>.Fail(DataError.Network(ex.Message));
        }
    }
}
