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

    public async Task<DataResult<TmsToolRequisitionLinkModel?>> GetByClientReferenceIdAsync(
        Guid clientReferenceId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TmsToolRequisitionLinkModel>()
                .Filter("client_reference_id", Operator.Equals, clientReferenceId.ToString())
                .Get(ct);

            var model = response.Models?.FirstOrDefault();
            return DataResult<TmsToolRequisitionLinkModel?>.Ok(model);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TmsToolRequisitionLinkModel?>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolRequisitionLinkModel?>.Fail(DataError.Network(ex.Message));
        }
    }

    public async Task<DataResult<TmsToolRequisitionLinkModel?>> FindBlockingDuplicateAsync(
        TmsWorkOrderRef workOrder,
        Guid warehouseId,
        CancellationToken ct = default)
    {
        var listResult = await ListByWorkOrderAsync(workOrder, ct);
        if (!listResult.IsSuccess || listResult.Value is null)
            return DataResult<TmsToolRequisitionLinkModel?>.Fail(listResult.Error ?? DataError.Unknown("Не удалось загрузить ссылки"));

        var duplicate = listResult.Value.FirstOrDefault(link =>
            link.WarehouseId == warehouseId
            && TmsRequisitionStatuses.BlocksDuplicateSubmission(link.LastKnownStatus));

        return DataResult<TmsToolRequisitionLinkModel?>.Ok(duplicate);
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

    public async Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListAllAsync(
        int limit = 500,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TmsToolRequisitionLinkModel>()
                .Order("updated_at", Ordering.Descending)
                .Limit(limit)
                .Get(ct);

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

    public async Task<DataResult<TmsToolRequisitionLinkModel?>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _gateway.From<TmsToolRequisitionLinkModel>()
                .Filter("id", Operator.Equals, id.ToString())
                .Get(ct);

            var model = response.Models?.FirstOrDefault();
            return DataResult<TmsToolRequisitionLinkModel?>.Ok(model);
        }
        catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
        {
            return DataResult<TmsToolRequisitionLinkModel?>.Fail(MapPostgrestError(ex));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolRequisitionLinkModel?>.Fail(DataError.Network(ex.Message));
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

        if (message.Contains("23505", StringComparison.Ordinal))
        {
            if (message.Contains("tms_requisition_id", StringComparison.OrdinalIgnoreCase))
                return DataError.Validation("ToolRequisition_Error_DuplicateTmsRequisition");

            if (message.Contains("client_reference_id", StringComparison.OrdinalIgnoreCase))
                return DataError.Validation("ToolRequisition_Error_DuplicateClientReference");

            if (message.Contains("tms_tool_requisition_links_request_warehouse_active_uidx", StringComparison.OrdinalIgnoreCase)
                || message.Contains("tms_tool_requisition_links_schedule_warehouse_active_uidx", StringComparison.OrdinalIgnoreCase))
                return DataError.Validation("ToolRequisition_Error_DuplicateActiveLink");
        }

        return DataError.Unknown(message);
    }
}
