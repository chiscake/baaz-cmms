using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using static Supabase.Postgrest.Constants;

namespace BAAZ.CMMS.Core.Repositories;

public sealed class TmsToolRequisitionLinkRepository : ITmsToolRequisitionLinkRepository
{
    private readonly ISupabaseGateway _gateway;

    public TmsToolRequisitionLinkRepository(ISupabaseGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<DataResult<TmsToolRequisitionLinkModel>> InsertAsync(
        TmsToolRequisitionLinkModel model,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TmsToolRequisitionLinkModel>().Insert(model);
            var inserted = response.Models?.FirstOrDefault();
            if (inserted is null)
                return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Unknown("Сервер не вернул созданную запись"));

            return DataResult<TmsToolRequisitionLinkModel>.Ok(inserted);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TmsToolRequisitionLinkModel>> GetByClientReferenceIdAsync(
        Guid clientReferenceId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TmsToolRequisitionLinkModel>()
                .Filter("client_reference_id", Operator.Equals, clientReferenceId.ToString())
                .Get(ct);

            var model = response.Models?.FirstOrDefault();
            if (model is null)
                return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Unknown("Запись не найдена"));

            return DataResult<TmsToolRequisitionLinkModel>.Ok(model);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        CancellationToken ct = default)
    {
        try
        {
            var query = _gateway.From<TmsToolRequisitionLinkModel>()
                .Order("created_at", Ordering.Descending);

            query = workOrder.Kind switch
            {
                TmsWorkOrderKind.Request => query.Filter("cmms_request_id", Operator.Equals, workOrder.Id.ToString()),
                TmsWorkOrderKind.Schedule => query.Filter("cmms_schedule_id", Operator.Equals, workOrder.Id.ToString()),
                _ => query,
            };

            var response = await query.Get(ct);
            var models = response.Models ?? [];
            return DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>.Ok(models.AsReadOnly());
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TmsToolRequisitionLinkModel>> UpdateSyncStateAsync(
        Guid id,
        string lastKnownStatus,
        string? syncEtag,
        DateTimeOffset syncedAt,
        CancellationToken ct = default)
    {
        try
        {
            var patch = new TmsToolRequisitionLinkModel
            {
                Id = id,
                LastKnownStatus = lastKnownStatus,
                SyncEtag = syncEtag,
                LastSyncedAt = syncedAt,
            };

            var response = await _gateway.From<TmsToolRequisitionLinkModel>().Update(patch);
            var updated = response.Models?.FirstOrDefault();
            if (updated is null)
                return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Unknown("Сервер не вернул обновлённую запись"));

            return DataResult<TmsToolRequisitionLinkModel>.Ok(updated);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolRequisitionLinkModel>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult> UpdateStatusByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        string lastKnownStatus,
        CancellationToken ct = default)
    {
        var listResult = await ListByWorkOrderAsync(workOrder, ct);
        if (!listResult.IsSuccess || listResult.Value is null)
            return DataResult.Fail(listResult.Error ?? DataError.Unknown("Не удалось загрузить ссылки"));

        foreach (var link in listResult.Value)
        {
            if (string.Equals(link.LastKnownStatus, lastKnownStatus, StringComparison.Ordinal))
                continue;

            var update = await UpdateSyncStateAsync(
                link.Id,
                lastKnownStatus,
                link.SyncEtag,
                DateTimeOffset.UtcNow,
                ct);

            if (!update.IsSuccess)
                return DataResult.Fail(update.Error ?? DataError.Unknown("Не удалось обновить статус"));
        }

        return DataResult.Ok();
    }

    private static DataError MapPostgrestError(Supabase.Postgrest.Exceptions.PostgrestException ex)
    {
        var message = ex.Message;
        if (ex.Content is { } content && !string.IsNullOrWhiteSpace(content))
            message = content;

        return DataError.Unknown(message);
    }
}
