using System.Net.Http.Json;
using System.Text.Json;using System.Text.Json.Serialization;
using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class HttpTmsIssuanceOutboundSender(
    string baseUrl,
    string integrationSecret,
    string supabaseAnonKey) : ITmsIssuanceOutboundSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<DataResult<ToolRequisitionCreateResult>> SendCreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = TmsIssuanceHttpClientHelper.CreateClient(baseUrl, integrationSecret, supabaseAnonKey, null);
            var payload = MapCreatePayload(input);
            using var response = await client.PostAsJsonAsync(
                "/api/v1/integration/cmms/tool-requisitions",
                payload,
                JsonOptions,
                cancellationToken);

            var httpError = await TmsIssuanceHttpClientHelper.ReadErrorAsync("ISS-API-1", response, cancellationToken);
            if (httpError is not null)
                return DataResult<ToolRequisitionCreateResult>.Fail(httpError);

            var dto = await response.Content.ReadFromJsonAsync<CreateResponseDto>(JsonOptions, cancellationToken);
            if (dto is null)
                return DataResult<ToolRequisitionCreateResult>.Fail(DataError.Unknown("Empty TMS response"));

            return DataResult<ToolRequisitionCreateResult>.Ok(MapCreateResult(dto));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return DataResult<ToolRequisitionCreateResult>.Fail(
                TmsIssuanceHttpClientHelper.ConnectionError("ISS-API-1", ex));
        }
    }

    private static object MapCreatePayload(ToolRequisitionInput input) => new
    {
        schema_version = 1,
        client_reference_id = input.ClientReferenceId,
        warehouse_id = input.WarehouseId,
        work_order = new
        {
            kind = input.WorkOrder.Kind == TmsWorkOrderKind.Schedule ? "schedule" : "request",
            id = input.WorkOrder.Id,
            number = input.WorkOrder.Number,
            status = input.WorkOrder.Status,
            title = input.WorkOrder.Title,
            asset_name = input.WorkOrder.AssetName,
            location_name = input.WorkOrder.LocationName,
        },
        technician = new
        {
            id = input.Technician.Id,
            full_name = input.Technician.FullName,
        },
        lines = input.Lines.Select(l => new
        {
            line_client_id = l.LineClientId,
            kind = l.Kind == ToolRequisitionLineKind.FreeText ? "free_text" : "catalog",
            catalog_item_id = l.Kind == ToolRequisitionLineKind.Catalog ? l.ToolId : null,
            description = l.Kind == ToolRequisitionLineKind.FreeText ? l.Description : null,
            quantity = l.Quantity,
        }),
        notes = input.Notes,
    };

    private static ToolRequisitionCreateResult MapCreateResult(CreateResponseDto dto) => new()
    {
        RequisitionId = dto.RequisitionId,
        RequisitionNumber = TmsRequisitionDisplayNumber.Format(dto.RequisitionId),
        ClientReferenceId = dto.ClientReferenceId,
        WarehouseId = dto.WarehouseId,
        WarehouseName = dto.WarehouseName,
        Status = dto.Status,
        CreatedAt = dto.CreatedAt,
        Lines = dto.Lines?.Select(l => new ToolRequisitionLineResult
        {
            LineId = l.LineId,
            LineClientId = l.LineClientId,
            LineStatus = l.LineStatus,
            Kind = l.Kind == "free_text" ? ToolRequisitionLineKind.FreeText : ToolRequisitionLineKind.Catalog,
            Description = l.Description,
        }).ToList() ?? [],
    };

    private sealed class CreateResponseDto
    {
        [JsonPropertyName("requisition_id")]
        public Guid RequisitionId { get; init; }

        [JsonPropertyName("client_reference_id")]
        public Guid ClientReferenceId { get; init; }

        [JsonPropertyName("warehouse_id")]
        public Guid WarehouseId { get; init; }

        [JsonPropertyName("warehouse_name")]
        public string? WarehouseName { get; init; }

        [JsonPropertyName("status")]
        public required string Status { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("lines")]
        public List<LineDto>? Lines { get; init; }
    }

    private sealed class LineDto
    {
        [JsonPropertyName("line_id")]
        public Guid LineId { get; init; }

        [JsonPropertyName("line_client_id")]
        public Guid LineClientId { get; init; }

        [JsonPropertyName("line_status")]
        public required string LineStatus { get; init; }

        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
