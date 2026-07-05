using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Реализация <see cref="ITmsIssuanceClient"/> для локальной разработки:
/// Create — через <see cref="ITmsIssuanceOutboundSender"/>;
/// каталоги складов/инструментов — демо-фикстуры.
/// </summary>
public sealed class TmsIssuanceClient(ITmsIssuanceOutboundSender outboundSender) : ITmsIssuanceClient
{
    private static readonly Guid DemoWarehouseCentralId = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    private static readonly Guid DemoWarehouseMeasuringId = Guid.Parse("a1000000-0000-4000-8000-000000000002");

    private readonly ITmsIssuanceOutboundSender _outboundSender = outboundSender;

    public Task<DataResult<ToolRequisitionCreateResult>> CreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
        => _outboundSender.SendCreateRequisitionAsync(input, cancellationToken);

    public Task<DataResult<TmsWarehouseListResult>> GetWarehousesAsync(
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new TmsWarehouseListResult
        {
            Warehouses =
            [
                new TmsWarehouseListItem { WarehouseId = DemoWarehouseCentralId, Name = "Центральный склад инструмента" },
                new TmsWarehouseListItem { WarehouseId = DemoWarehouseMeasuringId, Name = "Склад измерительного инструмента" },
            ],
            CatalogVersion = "demo-v1",
            ETag = "\"demo-warehouses-v1\"",
        };

        return Task.FromResult(DataResult<TmsWarehouseListResult>.Ok(result));
    }

    public Task<DataResult<TmsToolCatalogResult>> GetToolsAsync(
        Guid warehouseId,
        TmsToolAvailability availability = TmsToolAvailability.Available,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = warehouseId == DemoWarehouseMeasuringId
            ? GetMeasuringTools()
            : GetCentralTools();

        if (availability == TmsToolAvailability.Available)
            items = items.Where(t => t.QuantityAvailable > 0).ToList();

        var result = new TmsToolCatalogResult
        {
            WarehouseId = warehouseId,
            Availability = availability,
            CatalogVersion = "demo-v1",
            ETag = $"\"demo-tools-{warehouseId:N}\"",
            Items = items,
        };

        return Task.FromResult(DataResult<TmsToolCatalogResult>.Ok(result));
    }

    public Task<DataResult<TmsRequisitionListResult>> GetRequisitionsByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        TmsRequisitionFields fields = TmsRequisitionFields.Summary,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DataResult<TmsRequisitionListResult>.Ok(new TmsRequisitionListResult
        {
            WorkOrder = workOrder,
            Requisitions = [],
            ETag = null,
            NotModified = false,
        }));
    }

    public Task<DataResult<TmsRequisitionDetailResult>> GetRequisitionAsync(
        Guid requisitionId,
        TmsRequisitionFields fields = TmsRequisitionFields.Full,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DataResult<TmsRequisitionDetailResult>.Fail(
            DataError.Unknown("TMS requisition detail is not available in stub mode")));

    public Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionsAsync(
        TmsCancelRequisitionsInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DataResult<TmsCancelRequisitionsResult>.Ok(new TmsCancelRequisitionsResult
        {
            Cancelled = [],
            Skipped = [],
        }));
    }

    private static List<TmsToolCatalogItem> GetCentralTools() =>
    [
        new() { ToolId = Guid.Parse("b1000000-0000-4000-8000-000000000001"), Name = "Набор гаечных ключей 6–24", ToolTypeName = "Ручной", QuantityAvailable = 5, QuantityTotal = 8 },
        new() { ToolId = Guid.Parse("b1000000-0000-4000-8000-000000000002"), Name = "Динамометрический ключ 1/2\"", ToolTypeName = "Ручной", QuantityAvailable = 2, QuantityTotal = 3 },
        new() { ToolId = Guid.Parse("b1000000-0000-4000-8000-000000000003"), Name = "Съёмник подшипников", ToolTypeName = "Спец.", QuantityAvailable = 1, QuantityTotal = 2 },
        new() { ToolId = Guid.Parse("b1000000-0000-4000-8000-000000000004"), Name = "Молоток слесарный 500 г", ToolTypeName = "Ручной", QuantityAvailable = 10, QuantityTotal = 12 },
    ];

    private static List<TmsToolCatalogItem> GetMeasuringTools() =>
    [
        new() { ToolId = Guid.Parse("c1000000-0000-4000-8000-000000000001"), Name = "Штангенциркуль 0–150 мм", ToolTypeName = "Измерительный", QuantityAvailable = 4, QuantityTotal = 5 },
        new() { ToolId = Guid.Parse("c1000000-0000-4000-8000-000000000002"), Name = "Микрометр 0–25 мм", ToolTypeName = "Измерительный", QuantityAvailable = 3, QuantityTotal = 4 },
        new() { ToolId = Guid.Parse("c1000000-0000-4000-8000-000000000003"), Name = "Нутромер 18–35 мм", ToolTypeName = "Измерительный", QuantityAvailable = 2, QuantityTotal = 2 },
    ];
}
