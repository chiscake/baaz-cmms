using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.ToolRequisition;

public interface IToolRequisitionService
{
    Task<DataResult<ToolRequisitionDocxResult>> SubmitDocxAsync(
        ToolRequisitionFormInput input,
        string targetFilePath,
        CancellationToken cancellationToken = default);

    Task<DataResult<ToolRequisitionTmsResult>> SubmitToTmsAsync(
        ToolRequisitionFormInput input,
        Guid createdByProfileId,
        CancellationToken cancellationToken = default);
}
