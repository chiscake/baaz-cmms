using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Services.Catalog;
using BAAZ.CMMS.Core.Services.Requisitions;

namespace BAAZ.CMMS.Core.Services.MaterialRequisition;

public sealed class MaterialRequisitionService(
    IWarehouseIntegration warehouseIntegration,
    IRequestService requestService,
    IMaintenanceService maintenanceService,
    ITechnicianCatalogService technicianCatalogService,
    IAuthService authService) : IMaterialRequisitionService
{
    private readonly IWarehouseIntegration _warehouseIntegration = warehouseIntegration;
    private readonly IRequestService _requestService = requestService;
    private readonly IMaintenanceService _maintenanceService = maintenanceService;
    private readonly ITechnicianCatalogService _technicianCatalogService = technicianCatalogService;
    private readonly IAuthService _authService = authService;

    public async Task<DataResult<MaterialRequisitionResult>> SubmitAsync(
        MaterialRequisitionInput input,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateInput(input, targetFilePath);
        if (!validation.IsSuccess)
            return DataResult<MaterialRequisitionResult>.Fail(validation.Error!);

        var profile = _authService.CurrentProfile;
        if (profile is null)
            return DataResult<MaterialRequisitionResult>.Fail(DataError.Unauthorized());

        var techniciansResult = await _technicianCatalogService.GetTechniciansAsync(cancellationToken);
        if (!techniciansResult.IsSuccess || techniciansResult.Value is null)
        {
            return DataResult<MaterialRequisitionResult>.Fail(
                techniciansResult.Error ?? DataError.Unknown("MaterialRequisition_Error_LoadTechnicians"));
        }

        var technician = techniciansResult.Value.FirstOrDefault(t => t.Id == input.TechnicianId);
        if (technician is null || !technician.IsActive)
        {
            return DataResult<MaterialRequisitionResult>.Fail(
                DataError.Validation("MaterialRequisition_Error_NoTechnician"));
        }

        var contextResult = await BuildContextAsync(input, technician, cancellationToken);
        if (!contextResult.IsSuccess || contextResult.Value is null)
            return DataResult<MaterialRequisitionResult>.Fail(contextResult.Error!);

        var authorName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.Id.ToString()
            : profile.FullName;

        var request = new MaterialRequisitionDocumentRequest
        {
            Input = input,
            Context = contextResult.Value,
            AuthorFullName = authorName,
            TargetFilePath = targetFilePath,
        };

        return await _warehouseIntegration.CreateMaterialRequisitionAsync(request, cancellationToken);
    }

    private static DataResult ValidateInput(MaterialRequisitionInput input, string targetFilePath)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
            return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_NoTargetPath"));

        var hasRequest = input.RequestId is Guid requestId && requestId != Guid.Empty;
        var hasSchedule = input.ScheduleId is Guid scheduleId && scheduleId != Guid.Empty;

        if (hasRequest == hasSchedule)
            return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_NoWorkOrder"));

        if (input.TechnicianId == Guid.Empty)
            return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_NoTechnician"));

        if (string.IsNullOrWhiteSpace(input.WarehouseName))
            return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_NoWarehouse"));

        if (input.Lines is null || input.Lines.Count == 0)
            return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_NoLines"));

        foreach (var line in input.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Name)
                || string.IsNullOrWhiteSpace(line.Unit)
                || line.Quantity <= 0)
            {
                return DataResult.Fail(DataError.Validation("MaterialRequisition_Error_InvalidQuantity"));
            }
        }

        return DataResult.Ok();
    }

    private async Task<DataResult<MaterialRequisitionDocumentContext>> BuildContextAsync(
        MaterialRequisitionInput input,
        TechnicianListItem technician,
        CancellationToken cancellationToken)
    {
        var profile = _authService.CurrentProfile;
        var departmentName = profile?.RepairDepartmentName
            ?? technician.RepairDepartmentName
            ?? "—";

        var requisitionId = Guid.NewGuid();
        var displayNumber = FormatDisplayRequisitionNumber(requisitionId);

        if (input.RequestId is Guid requestId)
        {
            var detail = await _requestService.GetRequestByIdAsync(requestId, cancellationToken);
            if (detail is null)
                return DataResult<MaterialRequisitionDocumentContext>.Fail(
                    DataError.Validation("MaterialRequisition_Error_WorkOrderNotFound"));

            if (!WorkOrderRequisitionPolicy.AllowsMaterialRequisition(detail.Status))
            {
                return DataResult<MaterialRequisitionDocumentContext>.Fail(
                    DataError.Validation("MaterialRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<MaterialRequisitionDocumentContext>.Ok(new MaterialRequisitionDocumentContext
            {
                RequisitionId = requisitionId,
                RequisitionNumber = displayNumber,
                RepairDepartmentName = departmentName,
                TechnicianFullName = technician.FullName,
                RequestNumber = detail.RequestNumber,
                AssetName = detail.AssetName ?? "—",
                AssetNumber = detail.AssetNumber ?? "—",
            });
        }

        if (input.ScheduleId is Guid scheduleId)
        {
            var schedule = (await _maintenanceService.GetScheduleAsync(cancellationToken))
                .FirstOrDefault(s => s.Id == scheduleId);

            if (schedule is null)
            {
                return DataResult<MaterialRequisitionDocumentContext>.Fail(
                    DataError.Validation("MaterialRequisition_Error_WorkOrderNotFound"));
            }

            if (!WorkOrderRequisitionPolicy.AllowsMaterialRequisitionSchedule(schedule.Status))
            {
                return DataResult<MaterialRequisitionDocumentContext>.Fail(
                    DataError.Validation("MaterialRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<MaterialRequisitionDocumentContext>.Ok(new MaterialRequisitionDocumentContext
            {
                RequisitionId = requisitionId,
                RequisitionNumber = displayNumber,
                RepairDepartmentName = departmentName,
                TechnicianFullName = technician.FullName,
                ScheduleId = schedule.Id,
                MaintenanceType = schedule.MaintenanceType,
                MaintenanceTypeLabel = FormatMaintenanceType(schedule.MaintenanceType),
                PlannedDate = schedule.PlannedDate,
                AssetName = schedule.AssetName,
                AssetNumber = schedule.AssetNumber,
            });
        }

        return DataResult<MaterialRequisitionDocumentContext>.Fail(
            DataError.Validation("MaterialRequisition_Error_NoWorkOrder"));
    }

    private static string FormatMaintenanceType(string type) => type switch
    {
        "to1" => "ТО-1",
        "to2" => "ТО-2",
        "kr" => "КР",
        _ => type,
    };

    private static string FormatDisplayRequisitionNumber(Guid requisitionId)
    {
        var seq = (BitConverter.ToUInt16(requisitionId.ToByteArray(), 0) % 999) + 1;
        return $"ТМЦ-{DateTime.Today:yyyyMMdd}-{seq:D3}";
    }
}
