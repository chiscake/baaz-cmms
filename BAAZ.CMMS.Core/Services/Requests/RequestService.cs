using System.Diagnostics;

using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories;
using BAAZ.CMMS.Core.Repositories.Dtos;
using BAAZ.CMMS.Core.Services.Integrations;

namespace BAAZ.CMMS.Core.Services;

public sealed class RequestService(
    IRequestRepository requestRepository,
    IAuthService authService,
    IRequestIntegrationHooks integrationHooks) : IRequestService
{
    private static readonly string[] TypeValues = ["breakdown", "service", "inspection"];
    private static readonly string[] PriorityValues = ["low", "normal", "high", "critical"];
    private static readonly string[] RepairZoneValues = ["on_site", "workshop", "external"];
    private static readonly HashSet<string> CancellableStatuses = new(StringComparer.Ordinal)
    {
        "new", "accepted",
    };

    private readonly IRequestRepository _requestRepository = requestRepository;
    private readonly IAuthService _authService = authService;
    private readonly IRequestIntegrationHooks _integrationHooks = integrationHooks;

    public async Task<CreateRequestResult?> CreateRequestAsync(
        CreateRequestInput input,
        CancellationToken cancellationToken = default)
    {
        if (!TypeValues.Contains(input.Type)
            || !PriorityValues.Contains(input.Priority)
            || input.TargetRepairDepartmentId == Guid.Empty)
        {
            Debug.WriteLine(
                $"[RequestService] CreateRequestAsync validation failed: " +
                $"type={input.Type}, priority={input.Priority}, dept={input.TargetRepairDepartmentId}");
            return null;
        }

        var isAdmin = _authService.CurrentProfile?.Role == UserRole.Admin;
        var repairZone = "on_site";
        string? contractorName = null;

        if (isAdmin && !string.IsNullOrWhiteSpace(input.RepairZone))
        {
            repairZone = input.RepairZone.Trim();
            if (!RepairZoneValues.Contains(repairZone))
            {
                Debug.WriteLine($"[RequestService] CreateRequestAsync invalid repair zone: {repairZone}");
                return null;
            }

            if (repairZone == "external")
            {
                contractorName = string.IsNullOrWhiteSpace(input.ContractorName)
                    ? null
                    : input.ContractorName.Trim();
                if (string.IsNullOrWhiteSpace(contractorName))
                {
                    Debug.WriteLine("[RequestService] CreateRequestAsync contractor required for external zone");
                    return null;
                }
            }
        }

        var requestNumber = GenerateRequestNumber();
        var row = new RequestInsertDto
        {
            RequestNumber = requestNumber,
            Type = input.Type,
            Priority = input.Priority,
            RepairZone = repairZone,
            ContractorName = contractorName,
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            LocationDescription = input.LocationDescription.Trim(),
            RequesterId = input.RequesterId,
            AssetId = input.AssetId,
        };

        var created = await _requestRepository.CreateViaRpcAsync(
            row, input.TargetRepairDepartmentId, cancellationToken);
        if (!created.IsSuccess)
        {
            Debug.WriteLine(
                $"[RequestService] CreateRequestAsync CreateViaRpcAsync failed: " +
                $"code={created.Error?.Code}, key={created.Error?.MessageKey}, detail={created.Error?.Detail}");
            return null;
        }

        return new CreateRequestResult
        {
            Id = created.Value!.Id,
            RequestNumber = created.Value.RequestNumber ?? requestNumber,
        };
    }

    public async Task<IReadOnlyList<RequestListItem>> GetAllRequestsAsync(
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListAllAsync(limit, cancellationToken);
        return result.IsSuccess
            ? result.Value!.Select(MapListItemFromDetail).ToList()
            : [];
    }

    public async Task<DataResult> UpdateRequestFieldsAsync(
        Guid requestId,
        RequestEditInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.RequestNumber))
            return DataResult.Fail(DataError.Validation("AllRequests_Validation_RequestNumberRequired", ""));

        if (string.IsNullOrWhiteSpace(input.Title))
            return DataResult.Fail(DataError.Validation("AllRequests_Validation_TitleRequired", ""));

        if (!TypeValues.Contains(input.Type) || !PriorityValues.Contains(input.Priority))
            return DataResult.Fail(DataError.Validation("AllRequests_Error_Save", ""));

        var patch = new RequestPatchDto
        {
            RequestNumber = input.RequestNumber.Trim(),
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Type = input.Type,
            Priority = input.Priority,
        };

        return await _requestRepository.UpdateFieldsAsync(requestId, patch, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestListItem>> GetMyRequestsAsync(
        Guid requesterId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListByRequesterAsync(requesterId, limit, cancellationToken);
        return result.IsSuccess
            ? result.Value!.Select(MapListItem).ToList()
            : [];
    }

    public async Task<RequestDetailItem?> GetRequestByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.GetDetailByIdAsync(requestId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return null;

        return MapDetailItem(result.Value);
    }

    public async Task<bool> CancelRequestAsync(
        Guid requestId,
        Guid actorId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var current = await _requestRepository.GetStatusForRequesterAsync(requestId, actorId, cancellationToken);
        if (!current.IsSuccess || current.Value is null || !CancellableStatuses.Contains(current.Value.Status))
            return false;

        var patched = await _requestRepository.UpdateStatusAsync(requestId, "cancelled", cancellationToken);
        if (!patched.IsSuccess)
            return false;

        var history = await _requestRepository.InsertStatusHistoryAsync(
            new StatusHistoryInsertDto
            {
                RequestId = requestId,
                ChangedBy = actorId,
                OldStatus = current.Value.Status,
                NewStatus = "cancelled",
                Comment = comment,
            },
            cancellationToken);

        if (!history.IsSuccess)
            return false;

        await _integrationHooks.AfterRequestStatusChangedAsync(
            requestId, current.Value.Status, "cancelled", cancellationToken);
        return true;
    }

    public async Task<bool> CloseRequestAsync(
        Guid requestId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var current = await _requestRepository.GetStatusForRequesterAsync(requestId, actorId, cancellationToken);
        if (!current.IsSuccess
            || current.Value is null
            || !string.Equals(current.Value.Status, "completed", StringComparison.Ordinal))
        {
            return false;
        }

        var patched = await _requestRepository.UpdateStatusAsync(requestId, "closed", cancellationToken);
        if (!patched.IsSuccess)
            return false;

        var history = await _requestRepository.InsertStatusHistoryAsync(
            new StatusHistoryInsertDto
            {
                RequestId = requestId,
                ChangedBy = actorId,
                OldStatus = current.Value.Status,
                NewStatus = "closed",
                Comment = null,
            },
            cancellationToken);

        if (!history.IsSuccess)
            return false;

        await _integrationHooks.AfterRequestStatusChangedAsync(
            requestId, current.Value.Status, "closed", cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RequestListItem>> GetIncomingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListIncomingAsync(cancellationToken);
        return result.IsSuccess
            ? result.Value!.Select(MapListItemFromDetail).ToList()
            : [];
    }

    public async Task<IReadOnlyList<RequestListItem>> GetRequestsByStatusesAsync(
        IReadOnlyCollection<string> statuses,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListByStatusesAsync(statuses, cancellationToken);
        return result.IsSuccess
            ? result.Value!.Select(MapListItemFromDetail).ToList()
            : [];
    }

    public async Task<DataResult> AcceptRequestAsync(
        Guid requestId,
        Guid? technicianId = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.CallWorkflowRpcAsync(
            "accept_request",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_assignee_id"] = technicianId,
                ["p_comment"] = comment,
            },
            cancellationToken);

        return result;
    }

    public async Task<DataResult> RejectRequestAsync(
        Guid requestId,
        Guid actorId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var before = await _requestRepository.GetDetailByIdAsync(requestId, cancellationToken);
        var previousStatus = before.IsSuccess ? before.Value?.Status ?? string.Empty : string.Empty;

        var result = await _requestRepository.CallWorkflowRpcAsync(
            "reject_request",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_comment"] = comment,
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _integrationHooks.AfterRequestStatusChangedAsync(
                requestId, previousStatus, "rejected", cancellationToken);
        }

        return result;
    }

    public async Task<DataResult> AssignRequestAsync(
        Guid requestId,
        Guid technicianId,
        Guid actorId,
        Guid? repairDepartmentId = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["p_request_id"] = requestId,
            ["p_technician_id"] = technicianId,
        };

        if (repairDepartmentId is not null)
            parameters["p_repair_department_id"] = repairDepartmentId.Value;

        return await _requestRepository.CallWorkflowRpcAsync(
            "assign_request_technician",
            parameters,
            cancellationToken);
    }

    public async Task<DataResult> TransferDepartmentAsync(
        Guid requestId,
        Guid newDepartmentId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        return await _requestRepository.CallWorkflowRpcAsync(
            "transfer_request_department",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_new_department_id"] = newDepartmentId,
                ["p_comment"] = comment,
            },
            cancellationToken);
    }

    public async Task<DataResult> AddDepartmentAsync(
        Guid requestId,
        Guid departmentId,
        Guid? technicianId = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        return await _requestRepository.CallWorkflowRpcAsync(
            "add_request_department",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_department_id"] = departmentId,
                ["p_assignee_id"] = technicianId,
                ["p_comment"] = comment,
            },
            cancellationToken);
    }

    public async Task<DataResult> UpdateRepairZoneAsync(
        Guid requestId,
        string repairZone,
        string? contractorName = null,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        if (!RepairZoneValues.Contains(repairZone))
            return DataResult.Fail(DataError.Validation("RequestDetail_Error_RepairZoneRequired", repairZone));

        return await _requestRepository.CallWorkflowRpcAsync(
            "update_request_repair_zone",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_repair_zone"] = repairZone,
                ["p_contractor_name"] = contractorName,
                ["p_comment"] = comment,
            },
            cancellationToken);
    }

    public async Task<DataResult> StartWorkAsync(
        Guid requestId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        return await _requestRepository.CallWorkflowRpcAsync(
            "start_request_work",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_comment"] = comment,
            },
            cancellationToken);
    }

    public async Task<DataResult> ConfirmInventoryReceivedAsync(
        Guid requestId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var before = await _requestRepository.GetDetailByIdAsync(requestId, cancellationToken);
        var previousStatus = before.IsSuccess ? before.Value?.Status ?? string.Empty : string.Empty;

        var result = await _requestRepository.CallWorkflowRpcAsync(
            "confirm_inventory_received",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_comment"] = comment,
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _integrationHooks.AfterRequestStatusChangedAsync(
                requestId, previousStatus, "in_progress", cancellationToken);
        }

        return result;
    }

    public async Task<DataResult> CloseRequestAsStaffAsync(
        Guid requestId,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var before = await _requestRepository.GetDetailByIdAsync(requestId, cancellationToken);
        var previousStatus = before.IsSuccess ? before.Value?.Status ?? string.Empty : string.Empty;

        var result = await _requestRepository.CallWorkflowRpcAsync(
            "close_request_as_staff",
            new Dictionary<string, object?>
            {
                ["p_request_id"] = requestId,
                ["p_comment"] = comment,
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _integrationHooks.AfterRequestStatusChangedAsync(
                requestId, previousStatus, "closed", cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<RequestStatusHistoryItem>> GetStatusHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListStatusHistoryAsync(requestId, cancellationToken);
        if (!result.IsSuccess)
            return [];

        return result.Value!.Select(r => new RequestStatusHistoryItem
        {
            Id = r.Id,
            OldStatus = r.OldStatus ?? string.Empty,
            NewStatus = r.NewStatus ?? string.Empty,
            ChangedByName = r.Profiles?.FullName ?? string.Empty,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
        }).ToList();
    }

    public async Task<bool> CreateWorkReportAsync(
        Guid requestId,
        Guid repairDepartmentId,
        Guid authorId,
        WorkReportInput input,
        CancellationToken cancellationToken = default)
    {
        var maintenanceTypes = input.MaintenanceTypes?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var row = new WorkReportInsertDto
        {
            RequestId = requestId,
            ScheduleId = input.ScheduleId,
            RepairDepartmentId = repairDepartmentId,
            MaintenanceType = maintenanceTypes is { Count: > 0 } ? maintenanceTypes[0] : input.MaintenanceType,
            MaintenanceTypes = maintenanceTypes is { Count: > 0 } ? maintenanceTypes : null,
            AuthorId = authorId,
            TechnicianId = input.TechnicianId,
            WorkPerformed = input.WorkPerformed.Trim(),
            ActualDurationHours = input.ActualDurationHours,
            PartsUsed = input.PartsUsed,
            DefectsFound = string.IsNullOrWhiteSpace(input.DefectsFound) ? null : input.DefectsFound.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };

        var result = await _requestRepository.InsertWorkReportAsync(row, cancellationToken);
        return result.IsSuccess;
    }

    public async Task<IReadOnlyList<WorkReportItem>> GetWorkReportsForRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _requestRepository.ListWorkReportsByRequestAsync(requestId, cancellationToken);
        if (!result.IsSuccess)
            return [];

        return result.Value!.Select(r => new WorkReportItem
        {
            Id = r.Id,
            RequestId = r.RequestId,
            ScheduleId = r.ScheduleId,
            RepairDepartmentId = r.RepairDepartmentId,
            RepairDepartmentName = r.RepairDepartments?.Name,
            TechnicianName = r.Technicians?.FullName ?? string.Empty,
            WorkPerformed = r.WorkPerformed ?? string.Empty,
            ActualDurationHours = r.ActualDurationHours,
            DefectsFound = r.DefectsFound,
            Notes = r.Notes,
            MaintenanceType = r.MaintenanceType,
            MaintenanceTypes = r.MaintenanceTypes,
            CreatedAt = r.CreatedAt,
            PartsUsed = WorkReportPartsUsedFormatter.Format(r.PartsUsed),
        }).ToList();
    }

    private static RequestListItem MapListItem(RequestListRowDto row) => new()
    {
        Id = row.Id,
        RequestNumber = row.RequestNumber ?? string.Empty,
        Title = row.Title ?? string.Empty,
        Status = row.Status ?? string.Empty,
        Priority = row.Priority ?? string.Empty,
        Type = row.Type ?? string.Empty,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        AssigneeName = FormatAssigneeNames(row.RequestRepairDepartments),
    };

    private static RequestListItem MapListItemFromDetail(RequestDetailRowDto row) => new()
    {
        Id = row.Id,
        RequestNumber = row.RequestNumber ?? string.Empty,
        Title = row.Title ?? string.Empty,
        Description = row.Description ?? string.Empty,
        Status = row.Status ?? string.Empty,
        Priority = row.Priority ?? string.Empty,
        Type = row.Type ?? string.Empty,
        LocationDescription = row.LocationDescription ?? string.Empty,
        AssetNumber = row.Assets?.AssetNumber,
        AssetName = row.Assets?.Name,
        RepairZone = row.RepairZone ?? string.Empty,
        ContractorName = row.ContractorName,
        TargetDepartmentName = row.TargetRepairDepartment?.Name,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        AssigneeName = FormatAssigneeNames(row.RequestRepairDepartments),
        RequesterName = row.Profiles?.FullName,
        Departments = MapDepartments(row.RequestRepairDepartments),
    };

    private static RequestDetailItem MapDetailItem(RequestDetailRowDto row) => new()
    {
        Id = row.Id,
        RequestNumber = row.RequestNumber ?? string.Empty,
        Title = row.Title ?? string.Empty,
        Description = row.Description ?? string.Empty,
        Type = row.Type ?? string.Empty,
        Priority = row.Priority ?? string.Empty,
        RepairZone = row.RepairZone ?? string.Empty,
        ContractorName = row.ContractorName,
        Status = row.Status ?? string.Empty,
        LocationDescription = row.LocationDescription ?? string.Empty,
        AssetId = row.AssetId,
        AssetNumber = row.Assets?.AssetNumber,
        AssetName = row.Assets?.Name,
        InventoryId = row.InventoryId,
        InventoryKind = row.InventoryKind,
        InventoryName = row.InventoryName,
        InventorySerial = row.InventorySerial,
        InventoryTypeName = row.InventoryTypeName,
        InventoryHandoffMode = row.InventoryHandoffMode,
        InventoryWarehouseName = row.InventoryWarehouseName,
        InventoryReceivedAt = row.InventoryReceivedAt,
        RequesterName = row.Profiles?.FullName,
        Departments = MapDepartments(row.RequestRepairDepartments),
        AssigneeName = FormatAssigneeNames(row.RequestRepairDepartments),
        TargetRepairDepartmentId = row.TargetRepairDepartmentId,
        TargetRepairDepartmentName = row.TargetRepairDepartment?.Name,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
    };

    private static IReadOnlyList<RequestDepartmentItem> MapDepartments(
        List<RequestDepartmentEmbedDto>? departments)
    {
        if (departments is null || departments.Count == 0)
            return [];

        return departments.Select(d => new RequestDepartmentItem
        {
            RepairDepartmentId = d.RepairDepartmentId,
            DepartmentName = d.RepairDepartments?.Name ?? string.Empty,
            AssigneeId = d.AssigneeId,
            AssigneeName = d.Technicians?.FullName,
            AddedAt = d.AddedAt,
        }).ToList();
    }

    private static string? FormatAssigneeNames(List<RequestDepartmentEmbedDto>? departments)
    {
        if (departments is null || departments.Count == 0)
            return null;

        var names = departments
            .Select(d => d.Technicians?.FullName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToArray();

        return names.Length == 0 ? null : string.Join(", ", names);
    }

    private static string GenerateRequestNumber()
    {
        var suffix = Random.Shared.Next(1000, 9999);
        return $"З-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
    }
}
