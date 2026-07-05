using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class HttpTmsIssuanceClient(
    string baseUrl,
    string integrationSecret,
    string supabaseAnonKey,
    ITmsIssuanceOutboundSender outboundSender) : ITmsIssuanceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ITmsIssuanceOutboundSender _outboundSender = outboundSender;

    public Task<DataResult<ToolRequisitionCreateResult>> CreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
        => _outboundSender.SendCreateRequisitionAsync(input, cancellationToken);

    public async Task<DataResult<TmsWarehouseListResult>> GetWarehousesAsync(
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = TmsIssuanceHttpClientHelper.CreateClient(baseUrl, integrationSecret, supabaseAnonKey, ifNoneMatch);
            using var response = await client.GetAsync("/api/v1/integration/cmms/warehouses", cancellationToken);
            var httpError = await TmsIssuanceHttpClientHelper.ReadErrorAsync("ISS-API-3", response, cancellationToken);
            if (httpError is not null)
                return DataResult<TmsWarehouseListResult>.Fail(httpError);

            var dto = await response.Content.ReadFromJsonAsync<WarehouseListDto>(JsonOptions, cancellationToken);
            if (dto?.Warehouses is null)
                return DataResult<TmsWarehouseListResult>.Fail(DataError.Unknown("Empty warehouse list"));

            return DataResult<TmsWarehouseListResult>.Ok(new TmsWarehouseListResult
            {
                Warehouses = dto.Warehouses.Select(w => new TmsWarehouseListItem
                {
                    WarehouseId = w.WarehouseId,
                    Name = w.Name,
                }).ToList(),
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsWarehouseListResult>.Fail(
                TmsIssuanceHttpClientHelper.ConnectionError("ISS-API-3", ex));
        }
    }

    public async Task<DataResult<TmsToolCatalogResult>> GetToolsAsync(
        Guid warehouseId,
        TmsToolAvailability availability = TmsToolAvailability.Available,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = TmsIssuanceHttpClientHelper.CreateClient(baseUrl, integrationSecret, supabaseAnonKey, ifNoneMatch);
            var path = $"/api/v1/integration/cmms/warehouse-catalog?warehouse_id={warehouseId}&availability={(availability == TmsToolAvailability.Available ? "available" : "all")}";
            using var response = await client.GetAsync(path, cancellationToken);
            var httpError = await TmsIssuanceHttpClientHelper.ReadErrorAsync("ISS-API-4", response, cancellationToken);
            if (httpError is not null)
                return DataResult<TmsToolCatalogResult>.Fail(httpError);

            var dto = await response.Content.ReadFromJsonAsync<CatalogDto>(JsonOptions, cancellationToken);
            if (dto?.Items is null)
                return DataResult<TmsToolCatalogResult>.Fail(DataError.Unknown("Empty catalog"));

            return DataResult<TmsToolCatalogResult>.Ok(new TmsToolCatalogResult
            {
                WarehouseId = warehouseId,
                Availability = availability,
                Items = dto.Items.Select(i => new TmsToolCatalogItem
                {
                    ToolId = i.CatalogItemId,
                    Name = i.Name,
                    QuantityAvailable = i.QuantityAvailable,
                    QuantityTotal = i.QuantityTotal,
                }).ToList(),
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsToolCatalogResult>.Fail(
                TmsIssuanceHttpClientHelper.ConnectionError("ISS-API-4", ex));
        }
    }

    public async Task<DataResult<TmsRequisitionListResult>> GetRequisitionsByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        TmsRequisitionFields fields = TmsRequisitionFields.Summary,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = TmsIssuanceHttpClientHelper.CreateClient(baseUrl, integrationSecret, supabaseAnonKey, ifNoneMatch);
            var param = workOrder.Kind == TmsWorkOrderKind.Schedule ? "cmms_schedule_id" : "cmms_request_id";
            var path = $"/api/v1/integration/cmms/tool-requisition?{param}={workOrder.Id}&fields={fields.ToString().ToLowerInvariant()}";
            using var response = await client.GetAsync(path, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                return DataResult<TmsRequisitionListResult>.Ok(new TmsRequisitionListResult
                {
                    WorkOrder = workOrder,
                    Requisitions = [],
                    NotModified = true,
                });
            }

            var httpError = await TmsIssuanceHttpClientHelper.ReadErrorAsync("ISS-API-5", response, cancellationToken);
            if (httpError is not null)
                return DataResult<TmsRequisitionListResult>.Fail(httpError);

            var dto = await response.Content.ReadFromJsonAsync<StatusListDto>(JsonOptions, cancellationToken);
            return DataResult<TmsRequisitionListResult>.Ok(new TmsRequisitionListResult
            {
                WorkOrder = workOrder,
                Requisitions = dto?.Requisitions?.Select(MapSummary).ToList() ?? [],
                ETag = response.Headers.ETag?.Tag,
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsRequisitionListResult>.Fail(
                TmsIssuanceHttpClientHelper.ConnectionError("ISS-API-5", ex));
        }
    }

    public Task<DataResult<TmsRequisitionDetailResult>> GetRequisitionAsync(
        Guid requisitionId,
        TmsRequisitionFields fields = TmsRequisitionFields.Full,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsRequisitionDetailResult>.Fail(
            DataError.Unknown("GetRequisitionAsync not implemented for HTTP client")));

    public async Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionsAsync(
        TmsCancelRequisitionsInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = TmsIssuanceHttpClientHelper.CreateClient(baseUrl, integrationSecret, supabaseAnonKey, null);
            object payload = input.RequisitionIds is { Count: > 0 }
                ? new { schema_version = 1, requisition_ids = input.RequisitionIds, reason = input.Reason ?? "dispatcher_cancelled" }
                : new { schema_version = 1, cmms_request_id = input.CmmsRequestId ?? input.CmmsScheduleId, reason = input.Reason ?? "work_order_cancelled" };

            using var response = await client.PostAsJsonAsync("/api/v1/integration/cmms/cancel-tool-requisitions", payload, JsonOptions, cancellationToken);
            var httpError = await TmsIssuanceHttpClientHelper.ReadErrorAsync("ISS-API-2", response, cancellationToken);
            if (httpError is not null)
                return DataResult<TmsCancelRequisitionsResult>.Fail(httpError);

            var dto = await response.Content.ReadFromJsonAsync<CancelDto>(JsonOptions, cancellationToken);
            return DataResult<TmsCancelRequisitionsResult>.Ok(new TmsCancelRequisitionsResult
            {
                Cancelled = dto?.Cancelled?.Select(c => new TmsCancelRequisitionOutcome
                {
                    RequisitionId = c.RequisitionId,
                    Status = c.Status,
                }).ToList() ?? [],
                Skipped = dto?.Skipped?.Select(s => new TmsCancelRequisitionOutcome
                {
                    RequisitionId = s.RequisitionId,
                    Status = "skipped",
                    SkipReason = s.Reason,
                }).ToList() ?? [],
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<TmsCancelRequisitionsResult>.Fail(
                TmsIssuanceHttpClientHelper.ConnectionError("ISS-API-2", ex));
        }
    }

    private static TmsRequisitionSummaryItem MapSummary(StatusItemDto item) => new()
    {
        RequisitionId = item.RequisitionId,
        WarehouseId = item.WarehouseId,
        WarehouseName = item.WarehouseName,
        Status = item.Status,
        CancelledBy = item.CancelledBy,
        LinesSummary = item.LinesSummary is null ? null : new TmsRequisitionLinesSummary
        {
            Total = item.LinesSummary.Total,
            Pending = item.LinesSummary.Pending,
            Reserved = item.LinesSummary.Reserved,
            Issued = item.LinesSummary.Issued,
            Returned = item.LinesSummary.Returned,
        },
    };

    private sealed class WarehouseListDto
    {
        [JsonPropertyName("warehouses")]
        public List<WarehouseDto>? Warehouses { get; init; }
    }

    private sealed class WarehouseDto
    {
        [JsonPropertyName("warehouse_id")]
        public Guid WarehouseId { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }
    }

    private sealed class CatalogDto
    {
        [JsonPropertyName("items")]
        public List<CatalogItemDto>? Items { get; init; }
    }

    private sealed class CatalogItemDto
    {
        [JsonPropertyName("catalog_item_id")]
        public Guid CatalogItemId { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("quantity_available")]
        public int QuantityAvailable { get; init; }

        [JsonPropertyName("quantity_total")]
        public int QuantityTotal { get; init; }
    }

    private sealed class StatusListDto
    {
        [JsonPropertyName("requisitions")]
        public List<StatusItemDto>? Requisitions { get; init; }
    }

    private sealed class StatusItemDto
    {
        [JsonPropertyName("requisition_id")]
        public Guid RequisitionId { get; init; }

        [JsonPropertyName("warehouse_id")]
        public Guid WarehouseId { get; init; }

        [JsonPropertyName("warehouse_name")]
        public string? WarehouseName { get; init; }

        [JsonPropertyName("status")]
        public required string Status { get; init; }

        [JsonPropertyName("cancelled_by")]
        public string? CancelledBy { get; init; }

        [JsonPropertyName("lines_summary")]
        public LinesSummaryDto? LinesSummary { get; init; }
    }

    private sealed class LinesSummaryDto
    {
        [JsonPropertyName("total")]
        public int Total { get; init; }

        [JsonPropertyName("pending")]
        public int Pending { get; init; }

        [JsonPropertyName("reserved")]
        public int Reserved { get; init; }

        [JsonPropertyName("issued")]
        public int Issued { get; init; }

        [JsonPropertyName("returned")]
        public int Returned { get; init; }
    }

    private sealed class CancelDto
    {
        [JsonPropertyName("cancelled")]
        public List<CancelItemDto>? Cancelled { get; init; }

        [JsonPropertyName("skipped")]
        public List<SkipItemDto>? Skipped { get; init; }
    }

    private sealed class CancelItemDto
    {
        [JsonPropertyName("requisition_id")]
        public Guid RequisitionId { get; init; }

        [JsonPropertyName("status")]
        public required string Status { get; init; }
    }

    private sealed class SkipItemDto
    {
        [JsonPropertyName("requisition_id")]
        public Guid RequisitionId { get; init; }

        [JsonPropertyName("reason")]
        public required string Reason { get; init; }
    }
}
