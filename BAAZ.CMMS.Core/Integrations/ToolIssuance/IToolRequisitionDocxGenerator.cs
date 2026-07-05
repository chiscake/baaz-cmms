using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Integrations.ToolIssuance;

public interface IToolRequisitionDocxGenerator
{
    void Generate(ToolRequisitionDocumentRequest request, string targetFilePath);
}
