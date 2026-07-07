using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models.DocumentExport;

namespace BAAZ.CMMS.Core.Integrations.Documents;

public static class DocumentFileHelper
{
    public static DataResult<DocumentExportResult> WriteFile(
        Action<string> write,
        string targetFilePath)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
            return DataResult<DocumentExportResult>.Fail(DataError.Validation("DocumentExport_Error_NoTargetPath"));

        try
        {
            var directory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);

            write(targetFilePath);

            return DataResult<DocumentExportResult>.Ok(new DocumentExportResult
            {
                SavedFilePath = targetFilePath,
            });
        }
        catch (IOException ex)
        {
            return DataResult<DocumentExportResult>.Fail(
                DataError.Validation("DocumentExport_Error_SaveFailed", ex.Message));
        }
        catch (Exception ex)
        {
            return DataResult<DocumentExportResult>.Fail(
                DataError.Validation("DocumentExport_Error_SaveFailed", ex.Message));
        }
    }
}
