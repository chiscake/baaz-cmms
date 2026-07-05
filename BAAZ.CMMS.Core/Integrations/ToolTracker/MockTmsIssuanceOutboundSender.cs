using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class MockTmsIssuanceOutboundSender : ITmsIssuanceOutboundSender
{
    public Task<DataResult<ToolRequisitionCreateResult>> SendCreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fixture = TmsFixtureLoader.Load<ToolRequisitionCreateResult>("create_tool_requisition_response.json");
        var requisitionId = Guid.NewGuid();
        var result = new ToolRequisitionCreateResult
        {
            RequisitionId = requisitionId,
            RequisitionNumber = fixture.RequisitionNumber ?? TmsRequisitionDisplayNumber.Format(requisitionId),
            ClientReferenceId = input.ClientReferenceId,
            WarehouseId = input.WarehouseId,
            WarehouseName = fixture.WarehouseName,
            Status = TmsRequisitionStatuses.New,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = input.Lines.Select((line, i) => new ToolRequisitionLineResult
            {
                LineId = Guid.NewGuid(),
                LineClientId = line.LineClientId,
                LineStatus = TmsLineStatuses.Pending,
                Kind = line.Kind,
                ToolId = line.ToolId,
                Description = line.Description ?? $"Line {i + 1}",
            }).ToList(),
        };

        return Task.FromResult(DataResult<ToolRequisitionCreateResult>.Ok(result));
    }
}
