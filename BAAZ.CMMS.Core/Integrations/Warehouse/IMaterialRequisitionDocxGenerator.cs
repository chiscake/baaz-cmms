using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Integrations.Warehouse;

public interface IMaterialRequisitionDocxGenerator
{
    void Generate(MaterialRequisitionDocumentRequest request, string targetFilePath);
}
