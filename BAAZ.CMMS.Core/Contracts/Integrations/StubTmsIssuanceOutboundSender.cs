using System.Diagnostics;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Заглушка TMS-API-1: принимает payload, логирует, возвращает синтетический успех.
/// Заменить на HTTP-клиент при появлении кода TMS.
/// </summary>
public sealed class StubTmsIssuanceOutboundSender : ITmsIssuanceOutboundSender
{
    public Task<DataResult<ToolRequisitionCreateResult>> SendCreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requisitionId = Guid.NewGuid();
        Debug.WriteLine(
            $"[StubTmsIssuance] CreateRequisition warehouse={input.WarehouseId} " +
            $"workOrder={input.WorkOrder.Kind}:{input.WorkOrder.Id} lines={input.Lines.Count} " +
            $"clientRef={input.ClientReferenceId} -> requisitionId={requisitionId}");

        var lines = input.Lines.Select(line => new ToolRequisitionLineResult
        {
            LineId = Guid.NewGuid(),
            LineClientId = line.LineClientId,
            LineStatus = TmsLineStatuses.Pending,
            Kind = line.Kind,
            ToolId = line.ToolId,
            Description = line.Description,
        }).ToList();

        var result = new ToolRequisitionCreateResult
        {
            RequisitionId = requisitionId,
            ClientReferenceId = input.ClientReferenceId,
            WarehouseId = input.WarehouseId,
            WarehouseName = null,
            Status = TmsRequisitionStatuses.New,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = lines,
        };

        return Task.FromResult(DataResult<ToolRequisitionCreateResult>.Ok(result));
    }
}
