using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ISupabaseGateway _gateway;

    public ProfileRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<ProfileModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<ProfileModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<ProfileModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<ProfileModel>.Ok(response);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<ProfileModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<ProfileModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<ProfileModel>> UpdateAsync(ProfileModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<ProfileModel>().Update(model);

            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<ProfileModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            if (!updated.CreatedAt.HasValue || !updated.UpdatedAt.HasValue)
            {
                var reload = await GetByIdAsync(model.Id, ct);
                if (reload.IsSuccess)
                    updated = reload.Value!;
            }

            return DataResult<ProfileModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<ProfileModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<ProfileModel>.Fail(DataError.Network(ex.Message));
        }
    }

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        if (ex.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            ex.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return DataError.Unauthorized(ex.Message);

        return DataError.Unknown(ex.Message);
    }
}
