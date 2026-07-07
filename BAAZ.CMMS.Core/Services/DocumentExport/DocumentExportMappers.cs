using System;
using BAAZ.CMMS.Core.Integrations.Documents;
using BAAZ.CMMS.Core.Models;
using BAAZ.CMMS.Core.Models.DocumentExport;
using BAAZ.CMMS.Core.Repositories.Dtos;

namespace BAAZ.CMMS.Core.Services.DocumentExport;

internal static class DocumentExportMappers
{
    public static WorkReportDocumentRequest MapWorkReport(WorkReportRowDto row)
    {
        DateOnly? plannedDate = null;
        if (!string.IsNullOrWhiteSpace(row.MaintenanceSchedule?.PlannedDate)
            && DateOnly.TryParse(row.MaintenanceSchedule.PlannedDate, out var parsed))
        {
            plannedDate = parsed;
        }

        return new WorkReportDocumentRequest
        {
            ReportId = row.Id,
            RepairDepartmentName = row.RepairDepartments?.Name ?? string.Empty,
            TechnicianName = row.Technicians?.FullName ?? string.Empty,
            AuthorName = row.Profiles?.FullName ?? "—",
            WorkPerformed = row.WorkPerformed ?? string.Empty,
            ActualDurationHours = row.ActualDurationHours,
            DefectsFound = row.DefectsFound,
            Notes = row.Notes,
            PartsUsed = WorkReportPartsUsedFormatter.Format(row.PartsUsed),
            MaintenanceTypesText = DocumentExportLabels.FormatMaintenanceTypes(
                row.MaintenanceTypes,
                row.MaintenanceType),
            CreatedAt = row.CreatedAt,
            RequestNumber = row.Requests?.RequestNumber,
            RequestTitle = row.Requests?.Title,
            AssetName = row.Requests?.Assets?.Name ?? row.MaintenanceSchedule?.Assets?.Name,
            AssetNumber = row.Requests?.Assets?.AssetNumber ?? row.MaintenanceSchedule?.Assets?.AssetNumber,
            ScheduleMaintenanceType = row.MaintenanceSchedule?.MaintenanceType is { } mt
                ? DocumentExportLabels.MaintenanceType(mt)
                : null,
            SchedulePlannedDate = plannedDate,
        };
    }

    public static RepairRequestDocumentRequest MapRepairRequest(
        RequestDetailItem detail,
        string locationDescription,
        string authorFullName,
        DateTimeOffset generatedAt)
    {
        return new RepairRequestDocumentRequest
        {
            RequestId = detail.Id,
            RequestNumber = detail.RequestNumber,
            Title = detail.Title,
            Description = detail.Description,
            TypeLabel = DocumentExportLabels.RequestType(detail.Type),
            PriorityLabel = DocumentExportLabels.RequestPriority(detail.Priority),
            RepairZoneLabel = DocumentExportLabels.RepairZone(detail.RepairZone),
            StatusLabel = DocumentExportLabels.RequestStatus(detail.Status),
            LocationDescription = locationDescription,
            AssetDisplay = FormatAsset(detail),
            InventoryDisplay = FormatInventory(detail),
            RequesterName = detail.RequesterName,
            ContractorName = detail.ContractorName,
            TargetDepartmentName = detail.TargetRepairDepartmentName,
            Departments = detail.Departments.Select(d => new RepairRequestDepartmentLine
            {
                DepartmentName = d.DepartmentName,
                AssigneeName = d.AssigneeName,
            }).ToList(),
            AuthorFullName = authorFullName,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt,
            GeneratedAt = generatedAt,
        };
    }

    public static RequestCardDocumentRequest MapRequestCard(
        RepairRequestDocumentRequest request,
        IReadOnlyList<RequestStatusHistoryItem> history,
        IReadOnlyList<WorkReportItem> workReports)
    {
        return new RequestCardDocumentRequest
        {
            Request = request,
            History = history.Select(h =>
            {
                var sameStatus = string.Equals(h.OldStatus, h.NewStatus, StringComparison.OrdinalIgnoreCase);
                return new RequestCardHistoryLine
                {
                    ChangedAtText = DocxDocumentBuilder.FormatDateTime(h.CreatedAt),
                    ChangedByName = h.ChangedByName,
                    OldStatusLabel = sameStatus
                        ? string.Empty
                        : DocumentExportLabels.RequestStatus(h.OldStatus),
                    NewStatusLabel = DocumentExportLabels.RequestStatus(h.NewStatus),
                    Comment = h.Comment,
                };
            }).ToList(),
            WorkReports = workReports.Select(r => new RequestCardWorkReportSummary
            {
                DepartmentName = r.RepairDepartmentName ?? string.Empty,
                TechnicianName = r.TechnicianName,
                CreatedAtText = DocxDocumentBuilder.FormatDateTime(r.CreatedAt),
            }).ToList(),
        };
    }

    public static PprWorkOrderDocumentRequest MapPprWorkOrder(
        MaintenanceScheduleItem item,
        string? workDescription,
        string authorFullName,
        DateTimeOffset generatedAt) => new()
    {
        ScheduleId = item.Id,
        AssetName = item.AssetName,
        AssetNumber = item.AssetNumber,
        MaintenanceTypeLabel = DocumentExportLabels.MaintenanceType(item.MaintenanceType),
        PlannedDate = item.PlannedDate,
        StatusLabel = DocumentExportLabels.ScheduleStatus(item.Status),
        DepartmentNames = string.Join(", ", item.DepartmentNames),
        WorkDescription = workDescription,
        LastMaintenanceDate = item.LastMaintenanceDate,
        NextMaintenanceDate = item.NextMaintenanceDate,
        AuthorFullName = authorFullName,
        GeneratedAt = generatedAt,
    };

    public static MaintenanceScheduleExcelRequest MapScheduleExcel(
        IReadOnlyList<MaintenanceScheduleItem> items,
        string periodLabel,
        string? filtersSummary,
        DateTime generatedAt) => new()
    {
        PeriodLabel = periodLabel,
        FiltersSummary = filtersSummary,
        GeneratedAt = generatedAt,
        Rows = items.Select(i => new MaintenanceScheduleExcelRow
        {
            AssetNumber = i.AssetNumber,
            AssetName = i.AssetName,
            MaintenanceTypeLabel = DocumentExportLabels.MaintenanceType(i.MaintenanceType),
            PlannedDate = i.PlannedDate,
            StatusLabel = DocumentExportLabels.ScheduleStatus(i.Status),
            Status = i.Status,
            DepartmentNames = string.Join(", ", i.DepartmentNames),
            LastMaintenanceDate = DocxDocumentBuilder.FormatDate(i.LastMaintenanceDate),
            NextMaintenanceDate = DocxDocumentBuilder.FormatDate(i.NextMaintenanceDate),
        }).ToList(),
    };

    private static string? FormatAsset(RequestDetailItem detail)
    {
        if (detail.AssetId is null)
            return null;

        if (!string.IsNullOrWhiteSpace(detail.AssetNumber) && !string.IsNullOrWhiteSpace(detail.AssetName))
            return $"{detail.AssetName} (инв. № {detail.AssetNumber})";

        return detail.AssetName ?? detail.AssetNumber;
    }

    private static string? FormatInventory(RequestDetailItem detail)
    {
        if (detail.InventoryId is null)
            return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(detail.InventoryName))
            parts.Add(detail.InventoryName);
        if (!string.IsNullOrWhiteSpace(detail.InventorySerial))
            parts.Add($"сер. № {detail.InventorySerial}");
        if (!string.IsNullOrWhiteSpace(detail.InventoryTypeName))
            parts.Add(detail.InventoryTypeName);

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }
}
