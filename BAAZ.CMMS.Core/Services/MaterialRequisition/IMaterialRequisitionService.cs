using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services.MaterialRequisition;

public interface IMaterialRequisitionService
{
    Task<DataResult<MaterialRequisitionResult>> SubmitAsync(
        MaterialRequisitionInput input,
        string targetFilePath,
        CancellationToken cancellationToken = default);
}
