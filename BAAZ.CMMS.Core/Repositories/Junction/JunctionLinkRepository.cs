using BAAZ.CMMS.Core.Data;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories.Junction;

public sealed class JunctionLinkRepository<TModel> : IJunctionLinkRepository<TModel>
    where TModel : BaseModel, new()
{
    private readonly ISupabaseGateway _gateway;

    public JunctionLinkRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<IReadOnlyList<Guid>>> GetValuesAsync(
        string keyColumn,
        Guid keyValue,
        Func<TModel, Guid> valueSelector,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TModel>()
                .Filter(keyColumn, Operator.Equals, keyValue.ToString())
                .Get(ct);

            var ids = (response.Models ?? []).Select(valueSelector).ToList();
            return DataResult<IReadOnlyList<Guid>>.Ok(ids);
        }
        catch (PostgrestException ex)
        {
            return DataResult<IReadOnlyList<Guid>>.Fail(PostgrestErrorMapper.Map(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<Guid>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> ReplaceAsync(
        string keyColumn,
        Guid keyValue,
        IReadOnlyList<Guid> valueIds,
        Func<Guid, Guid, TModel> rowFactory,
        CancellationToken ct = default)
    {
        try
        {
            await _gateway.From<TModel>()
                .Filter(keyColumn, Operator.Equals, keyValue.ToString())
                .Delete();

            if (valueIds.Count == 0)
                return DataResult.Ok();

            var rows = valueIds.Distinct().Select(id => rowFactory(keyValue, id)).ToList();
            await _gateway.From<TModel>().Insert(rows);

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
