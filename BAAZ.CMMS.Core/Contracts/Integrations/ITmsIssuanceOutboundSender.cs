using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.TmsIssuance;

namespace BAAZ.CMMS.Core.Contracts.Integrations;

/// <summary>
/// Точка исходящей отправки в TMS (HTTP — будущая реализация).
/// Сейчас — <see cref="StubTmsIssuanceOutboundSender"/>.
/// </summary>
public interface ITmsIssuanceOutboundSender
{
    Task<DataResult<ToolRequisitionCreateResult>> SendCreateRequisitionAsync(
        ToolRequisitionInput input,
        CancellationToken cancellationToken = default);
}
