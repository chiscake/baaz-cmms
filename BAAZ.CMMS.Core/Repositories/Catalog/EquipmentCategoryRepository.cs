using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class EquipmentCategoryRepository : IEquipmentCategoryRepository
{
    private readonly ISupabaseGateway _gateway;

    public EquipmentCategoryRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<EquipmentCategoryModel>>> ListAsync(
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _gateway.From<EquipmentCategoryModel>().Order("name", Ordering.Ascending);
            if (!includeInactive)
                query = query.Filter("is_active", Operator.Equals, "true");

            var response = await query.Get(ct);
            return DataResult<IReadOnlyList<EquipmentCategoryModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<EquipmentCategoryModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<EquipmentCategoryModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<EquipmentCategoryModel>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<EquipmentCategoryModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Single(ct);

            if (response is null)
                return DataResult<EquipmentCategoryModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<EquipmentCategoryModel>.Ok(response);
        }
        catch (PostgrestException ex)
        {
            return DataResult<EquipmentCategoryModel>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<EquipmentCategoryModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<EquipmentCategoryModel>> InsertAsync(
        EquipmentCategoryModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<EquipmentCategoryModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<EquipmentCategoryModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<EquipmentCategoryModel>.Ok(inserted);
        }
        catch (PostgrestException ex)
        {
            return DataResult<EquipmentCategoryModel>.Fail(MapCategoryError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<EquipmentCategoryModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<EquipmentCategoryModel>> UpdateAsync(
        EquipmentCategoryModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<EquipmentCategoryModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<EquipmentCategoryModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<EquipmentCategoryModel>.Ok(updated);
        }
        catch (PostgrestException ex)
        {
            return DataResult<EquipmentCategoryModel>.Fail(MapCategoryError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<EquipmentCategoryModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<EquipmentCategoryModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Delete();

            return DataResult.Ok();
        }
        catch (PostgrestException ex)
        {
            return DataResult.Fail(MapCategoryError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult.Fail(DataError.Network(ex.Message));
        }
    }

    private static DataError MapCategoryError(PostgrestException ex)
    {
        if (PostgrestErrorMapper.IsUniqueViolation(ex))
            return DataError.Validation("EquipmentCategory_Error_DuplicateName", ex.Message);

        return PostgrestErrorMapper.Map(ex);
    }
}
