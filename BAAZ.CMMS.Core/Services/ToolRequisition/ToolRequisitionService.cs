using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Services.Catalog;
using BAAZ.CMMS.Core.Services.Requisitions;
using BAAZ.CMMS.Core.Services.TmsIssuance;

namespace BAAZ.CMMS.Core.Services.ToolRequisition;

public sealed class ToolRequisitionService(
    IToolRequisitionDocxIntegration docxIntegration,
    ITmsToolRequisitionService tmsToolRequisitionService,
    IRequestService requestService,
    IMaintenanceService maintenanceService,
    ITechnicianCatalogService technicianCatalogService,
    IAuthService authService) : IToolRequisitionService
{
    private readonly IToolRequisitionDocxIntegration _docxIntegration = docxIntegration;
    private readonly ITmsToolRequisitionService _tmsToolRequisitionService = tmsToolRequisitionService;
    private readonly IRequestService _requestService = requestService;
    private readonly IMaintenanceService _maintenanceService = maintenanceService;
    private readonly ITechnicianCatalogService _technicianCatalogService = technicianCatalogService;
    private readonly IAuthService _authService = authService;

    public async Task<DataResult<ToolRequisitionDocxResult>> SubmitDocxAsync(
        ToolRequisitionFormInput input,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateInput(input, requireWarehouseId: false, targetFilePath);
        if (!validation.IsSuccess)
            return DataResult<ToolRequisitionDocxResult>.Fail(validation.Error!);

        var build = await BuildDocumentRequestAsync(input, targetFilePath, cancellationToken);
        if (!build.IsSuccess || build.Value is null)
            return DataResult<ToolRequisitionDocxResult>.Fail(build.Error!);

        return await _docxIntegration.CreateToolRequisitionDocxAsync(build.Value, cancellationToken);
    }

    public async Task<DataResult<ToolRequisitionTmsResult>> SubmitToTmsAsync(
        ToolRequisitionFormInput input,
        Guid createdByProfileId,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateInput(input, requireWarehouseId: true, targetFilePath: null);
        if (!validation.IsSuccess)
            return DataResult<ToolRequisitionTmsResult>.Fail(validation.Error!);

        var technicianResult = await ResolveTechnicianAsync(input.TechnicianId, cancellationToken);
        if (!technicianResult.IsSuccess || technicianResult.Value is null)
            return DataResult<ToolRequisitionTmsResult>.Fail(technicianResult.Error!);

        var workOrderResult = await BuildWorkOrderSnapshotAsync(input, cancellationToken);
        if (!workOrderResult.IsSuccess || workOrderResult.Value is null)
            return DataResult<ToolRequisitionTmsResult>.Fail(workOrderResult.Error!);

        var tmsInput = new ToolRequisitionInput
        {
            ClientReferenceId = Guid.NewGuid(),
            WarehouseId = input.WarehouseId!.Value,
            WorkOrder = workOrderResult.Value,
            Technician = new ToolRequisitionTechnicianSnapshot
            {
                Id = technicianResult.Value.Id,
                FullName = technicianResult.Value.FullName,
            },
            Lines = input.Lines.Select(line => new ToolRequisitionLineInput
            {
                LineClientId = Guid.NewGuid(),
                Kind = line.ToolId is Guid toolId && toolId != Guid.Empty
                    ? ToolRequisitionLineKind.Catalog
                    : ToolRequisitionLineKind.FreeText,
                ToolId = line.ToolId,
                Description = line.Name,
                Quantity = line.Quantity > 0 ? line.Quantity : 1,
            }).ToList(),
            Notes = input.Notes,
        };

        var created = await _tmsToolRequisitionService.CreateAndPersistAsync(
            tmsInput, createdByProfileId, cancellationToken);

        if (!created.IsSuccess || created.Value is null)
            return DataResult<ToolRequisitionTmsResult>.Fail(created.Error ?? DataError.Unknown("ToolRequisition_Error_TmsFailed"));

        return DataResult<ToolRequisitionTmsResult>.Ok(new ToolRequisitionTmsResult
        {
            RequisitionId = created.Value.RequisitionId,
            RequisitionNumber = created.Value.RequisitionNumber
                ?? TmsRequisitionDisplayNumber.Format(created.Value.RequisitionId),
            ClientReferenceId = created.Value.ClientReferenceId,
            Status = created.Value.Status,
            WarehouseName = created.Value.WarehouseName ?? input.WarehouseName,
        });
    }

    private static DataResult ValidateInput(
        ToolRequisitionFormInput input,
        bool requireWarehouseId,
        string? targetFilePath)
    {
        if (!requireWarehouseId && string.IsNullOrWhiteSpace(targetFilePath))
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoTargetPath"));

        var hasRequest = input.RequestId is Guid requestId && requestId != Guid.Empty;
        var hasSchedule = input.ScheduleId is Guid scheduleId && scheduleId != Guid.Empty;

        if (hasRequest == hasSchedule)
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoWorkOrder"));

        if (input.TechnicianId == Guid.Empty)
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoTechnician"));

        if (string.IsNullOrWhiteSpace(input.WarehouseName))
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoWarehouse"));

        if (requireWarehouseId && (input.WarehouseId is null || input.WarehouseId == Guid.Empty))
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoWarehouseId"));

        if (input.Lines is null || input.Lines.Count == 0)
            return DataResult.Fail(DataError.Validation("ToolRequisition_Error_NoLines"));

        foreach (var line in input.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Name) || line.Quantity <= 0)
                return DataResult.Fail(DataError.Validation("ToolRequisition_Error_InvalidQuantity"));
        }

        return DataResult.Ok();
    }

    private async Task<DataResult<ToolRequisitionDocumentRequest>> BuildDocumentRequestAsync(
        ToolRequisitionFormInput input,
        string targetFilePath,
        CancellationToken cancellationToken)
    {
        var profile = _authService.CurrentProfile;
        if (profile is null)
            return DataResult<ToolRequisitionDocumentRequest>.Fail(DataError.Unauthorized());

        var technicianResult = await ResolveTechnicianAsync(input.TechnicianId, cancellationToken);
        if (!technicianResult.IsSuccess || technicianResult.Value is null)
            return DataResult<ToolRequisitionDocumentRequest>.Fail(technicianResult.Error!);

        var contextResult = await BuildDocumentContextAsync(input, technicianResult.Value, cancellationToken);
        if (!contextResult.IsSuccess || contextResult.Value is null)
            return DataResult<ToolRequisitionDocumentRequest>.Fail(contextResult.Error!);

        var authorName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.Id.ToString()
            : profile.FullName;

        return DataResult<ToolRequisitionDocumentRequest>.Ok(new ToolRequisitionDocumentRequest
        {
            Input = input,
            Context = contextResult.Value,
            AuthorFullName = authorName,
            TargetFilePath = targetFilePath,
        });
    }

    private async Task<DataResult<TechnicianListItem>> ResolveTechnicianAsync(
        Guid technicianId,
        CancellationToken cancellationToken)
    {
        var techniciansResult = await _technicianCatalogService.GetTechniciansAsync(cancellationToken);
        if (!techniciansResult.IsSuccess || techniciansResult.Value is null)
        {
            return DataResult<TechnicianListItem>.Fail(
                techniciansResult.Error ?? DataError.Unknown("ToolRequisition_Error_LoadTechnicians"));
        }

        var technician = techniciansResult.Value.FirstOrDefault(t => t.Id == technicianId);
        if (technician is null || !technician.IsActive)
            return DataResult<TechnicianListItem>.Fail(DataError.Validation("ToolRequisition_Error_NoTechnician"));

        return DataResult<TechnicianListItem>.Ok(technician);
    }

    private async Task<DataResult<ToolRequisitionDocumentContext>> BuildDocumentContextAsync(
        ToolRequisitionFormInput input,
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
                return DataResult<ToolRequisitionDocumentContext>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderNotFound"));

            if (!WorkOrderRequisitionPolicy.AllowsToolRequisition(detail.Status))
            {
                return DataResult<ToolRequisitionDocumentContext>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<ToolRequisitionDocumentContext>.Ok(new ToolRequisitionDocumentContext
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
                return DataResult<ToolRequisitionDocumentContext>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderNotFound"));
            }

            if (!WorkOrderRequisitionPolicy.AllowsToolRequisitionSchedule(schedule.Status))
            {
                return DataResult<ToolRequisitionDocumentContext>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<ToolRequisitionDocumentContext>.Ok(new ToolRequisitionDocumentContext
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

        return DataResult<ToolRequisitionDocumentContext>.Fail(
            DataError.Validation("ToolRequisition_Error_NoWorkOrder"));
    }

    private async Task<DataResult<ToolRequisitionWorkOrderSnapshot>> BuildWorkOrderSnapshotAsync(
        ToolRequisitionFormInput input,
        CancellationToken cancellationToken)
    {
        if (input.RequestId is Guid requestId)
        {
            var detail = await _requestService.GetRequestByIdAsync(requestId, cancellationToken);
            if (detail is null)
                return DataResult<ToolRequisitionWorkOrderSnapshot>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderNotFound"));

            if (!WorkOrderRequisitionPolicy.AllowsToolRequisition(detail.Status))
            {
                return DataResult<ToolRequisitionWorkOrderSnapshot>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<ToolRequisitionWorkOrderSnapshot>.Ok(new ToolRequisitionWorkOrderSnapshot
            {
                Kind = TmsWorkOrderKind.Request,
                Id = requestId,
                Number = detail.RequestNumber,
                Status = WorkOrderRequisitionPolicy.MapToTmsWorkOrderStatus(detail.Status),
                Title = detail.Title,
                AssetName = detail.AssetName,
                LocationName = detail.LocationDescription,
            });
        }

        if (input.ScheduleId is Guid scheduleId)
        {
            var schedule = (await _maintenanceService.GetScheduleAsync(cancellationToken))
                .FirstOrDefault(s => s.Id == scheduleId);

            if (schedule is null)
            {
                return DataResult<ToolRequisitionWorkOrderSnapshot>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderNotFound"));
            }

            if (!WorkOrderRequisitionPolicy.AllowsToolRequisitionSchedule(schedule.Status))
            {
                return DataResult<ToolRequisitionWorkOrderSnapshot>.Fail(
                    DataError.Validation("ToolRequisition_Error_WorkOrderStatus"));
            }

            return DataResult<ToolRequisitionWorkOrderSnapshot>.Ok(new ToolRequisitionWorkOrderSnapshot
            {
                Kind = TmsWorkOrderKind.Schedule,
                Id = scheduleId,
                Number = schedule.Id.ToString(),
                Status = WorkOrderRequisitionPolicy.MapToTmsWorkOrderStatus(schedule.Status),
                Title = $"{schedule.AssetNumber} — {schedule.AssetName}",
                AssetName = schedule.AssetName,
            });
        }

        return DataResult<ToolRequisitionWorkOrderSnapshot>.Fail(
            DataError.Validation("ToolRequisition_Error_NoWorkOrder"));
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
        return $"ИНС-{DateTime.Today:yyyyMMdd}-{seq:D3}";
    }
}
