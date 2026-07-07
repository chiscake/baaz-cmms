using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using Supabase.Postgrest.Exceptions;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class CategoryMaintenanceNormRepository : ICategoryMaintenanceNormRepository
{
    private readonly ISupabaseGateway _gateway;

    public CategoryMaintenanceNormRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>> ListByCategoryAsync(
        Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<CategoryMaintenanceNormModel>()
                .Filter("category_id", Operator.Equals, categoryId.ToString())
                .Get(ct);

            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Ok((response.Models ?? []).AsReadOnly());
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>> ListByCategoryIdsAsync(
        IReadOnlyList<Guid> categoryIds, CancellationToken ct = default)
    {
        if (categoryIds.Count == 0)
            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Ok([]);

        try
        {
            var response = await _gateway.From<CategoryMaintenanceNormModel>().Get(ct);
            var idSet = categoryIds.ToHashSet();
            var filtered = (response.Models ?? []).Where(m => idSet.Contains(m.CategoryId)).ToList();

            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Ok(filtered);
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<CategoryMaintenanceNormModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<CategoryMaintenanceNormModel>> InsertAsync(
        CategoryMaintenanceNormModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<CategoryMaintenanceNormModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<CategoryMaintenanceNormModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<CategoryMaintenanceNormModel>.Ok(inserted);
        }
        catch (PostgrestException ex)
        {
            return DataResult<CategoryMaintenanceNormModel>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<CategoryMaintenanceNormModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<CategoryMaintenanceNormModel>> UpdateAsync(
        CategoryMaintenanceNormModel model, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<CategoryMaintenanceNormModel>().Update(model);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<CategoryMaintenanceNormModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<CategoryMaintenanceNormModel>.Ok(updated);
        }
        catch (PostgrestException ex)
        {
            return DataResult<CategoryMaintenanceNormModel>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<CategoryMaintenanceNormModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<CategoryMaintenanceNormModel>()
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
