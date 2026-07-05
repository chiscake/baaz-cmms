using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class ProfileLocationScopeRepository : IProfileLocationScopeRepository
{
    private readonly ISupabaseGateway _gateway;

    public ProfileLocationScopeRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<Guid>>> GetLocationIdsByProfileIdAsync(
        Guid profileId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<ProfileLocationScopeModel>()
                .Filter("profile_id", Operator.Equals, profileId.ToString())
                .Get(ct);

            var ids = (response.Models ?? [])
                .Select(m => m.LocationId)
                .ToList();

            return DataResult<IReadOnlyList<Guid>>.Ok(ids);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<Guid>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<Guid>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> ReplaceForProfileAsync(
        Guid profileId,
        IReadOnlyList<Guid> locationIds,
        CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<ProfileLocationScopeModel>()
                .Filter("profile_id", Operator.Equals, profileId.ToString())
                .Delete();

            if (locationIds.Count == 0)
                return DataResult.Ok();

            var rows = locationIds
                .Distinct()
                .Select(id => new ProfileLocationScopeModel
                {
                    ProfileId = profileId,
                    LocationId = id,
                })
                .ToList();

            await _gateway.From<ProfileLocationScopeModel>().Insert(rows);

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

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode is System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        return DataError.Unknown(ex.Message);
    }
}
