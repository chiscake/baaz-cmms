using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Integrations.Documents.Maintenance;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

public interface IPprWorkOrderExportService
{
    Task<DataResult<DocumentExportResult>> ExportAsync(
        MaintenanceScheduleItem scheduleItem,
        string targetFilePath,
        CancellationToken cancellationToken = default);
}

public sealed class PprWorkOrderExportService(
    IMaintenanceService maintenanceService,
    IAuthService authService,
    IPprWorkOrderDocxGenerator generator) : IPprWorkOrderExportService
{
    public async Task<DataResult<DocumentExportResult>> ExportAsync(
        MaintenanceScheduleItem scheduleItem,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        string? workDescription = null;
        var norms = await maintenanceService.GetAssetNormsDetailAsync(scheduleItem.AssetId, cancellationToken);
        if (norms.IsSuccess && norms.Value is not null)
        {
            var slot = norms.Value.Slots.FirstOrDefault(s =>
                string.Equals(s.MaintenanceType, scheduleItem.MaintenanceType, StringComparison.Ordinal));
            workDescription = slot?.EffectiveDescription ?? slot?.PresetDescription;
        }

        var author = string.IsNullOrWhiteSpace(authService.CurrentProfile?.FullName)
            ? authService.CurrentProfile?.Id.ToString() ?? "—"
            : authService.CurrentProfile!.FullName!;

        var documentRequest = DocumentExportMappers.MapPprWorkOrder(
            scheduleItem,
            workDescription,
            author,
            DateTimeOffset.Now);

        return DocumentFileHelper.WriteFile(path => generator.Generate(documentRequest, path), targetFilePath);
    }
}
