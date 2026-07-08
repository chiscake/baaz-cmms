using BAAZ.CMMS.Core.Contracts.Integrations;
using BAAZ.CMMS.Core.Data;
using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Integrations.ToolTracker;
using BAAZ.CMMS.Core.Models.TmsIssuance;
using BAAZ.CMMS.Core.Repositories;

namespace BAAZ.CMMS.Core.Services.TmsIssuance;

public sealed class TmsToolRequisitionService(
    ITmsIssuanceClient issuanceClient,
    ITmsToolRequisitionLinkRepository linkRepository) : ITmsToolRequisitionService
{
    private readonly ITmsIssuanceClient _issuanceClient = issuanceClient;
    private readonly ITmsToolRequisitionLinkRepository _linkRepository = linkRepository;

    public async Task<DataResult<ToolRequisitionCreateResult>> CreateAndPersistAsync(
        ToolRequisitionInput input,
        Guid createdByProfileId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _linkRepository.GetByClientReferenceIdAsync(input.ClientReferenceId, cancellationToken);
        if (existing.IsSuccess && existing.Value is not null)
        {
            return DataResult<ToolRequisitionCreateResult>.Ok(MapExistingToCreateResult(existing.Value));
        }

        var duplicate = await _linkRepository.FindBlockingDuplicateAsync(
            new TmsWorkOrderRef { Kind = input.WorkOrder.Kind, Id = input.WorkOrder.Id },
            input.WarehouseId,
            cancellationToken);
        if (!duplicate.IsSuccess)
            return DataResult<ToolRequisitionCreateResult>.Fail(duplicate.Error ?? DataError.Unknown("Не удалось проверить существующие заявки TMS"));

        if (duplicate.Value is not null)
            return DataResult<ToolRequisitionCreateResult>.Fail(DataError.Validation("ToolRequisition_Error_DuplicateActiveLink"));

        var created = await _issuanceClient.CreateRequisitionAsync(input, cancellationToken);
        if (!created.IsSuccess || created.Value is null)
            return DataResult<ToolRequisitionCreateResult>.Fail(created.Error ?? DataError.Unknown("TMS не принял заявку"));

        if (TmsIntegrationSettingsSync.IsLive)
        {
            var verify = await _issuanceClient.GetRequisitionsByWorkOrderAsync(
                new TmsWorkOrderRef { Kind = input.WorkOrder.Kind, Id = input.WorkOrder.Id },
                TmsRequisitionFields.Summary,
                cancellationToken: cancellationToken);
            if (!verify.IsSuccess || verify.Value is null)
            {
                return DataResult<ToolRequisitionCreateResult>.Fail(
                    verify.Error ?? DataError.Unknown("ToolRequisition_Error_TmsNotConfirmed"));
            }

            var confirmed = verify.Value.Requisitions.Any(r => r.RequisitionId == created.Value.RequisitionId);
            if (!confirmed)
            {
                return DataResult<ToolRequisitionCreateResult>.Fail(
                    DataError.Unknown("ToolRequisition_Error_TmsNotConfirmed"));
            }
        }

        var link = new TmsToolRequisitionLinkModel
        {
            ClientReferenceId = created.Value.ClientReferenceId,
            TmsRequisitionId = created.Value.RequisitionId,
            WarehouseId = created.Value.WarehouseId,
            WarehouseName = created.Value.WarehouseName,
            WorkOrderKind = ToDbWorkOrderKind(input.WorkOrder.Kind),
            CmmsRequestId = input.WorkOrder.Kind == TmsWorkOrderKind.Request ? input.WorkOrder.Id : null,
            CmmsScheduleId = input.WorkOrder.Kind == TmsWorkOrderKind.Schedule ? input.WorkOrder.Id : null,
            LastKnownStatus = created.Value.Status,
            LastSyncedAt = DateTimeOffset.UtcNow,
            Notes = input.Notes,
            CreatedBy = createdByProfileId,
        };

        var persisted = await _linkRepository.InsertAsync(link, cancellationToken);
        if (!persisted.IsSuccess)
            return DataResult<ToolRequisitionCreateResult>.Fail(persisted.Error ?? DataError.Unknown("Не удалось сохранить ссылку на TMS"));

        return created;
    }

    public Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListLocalByWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        CancellationToken cancellationToken = default)
        => _linkRepository.ListByWorkOrderAsync(workOrder, cancellationToken);

    public Task<DataResult<IReadOnlyList<TmsToolRequisitionLinkModel>>> ListAllLocalAsync(
        int limit = 500,
        CancellationToken cancellationToken = default)
        => _linkRepository.ListAllAsync(limit, cancellationToken);

    public Task<DataResult<TmsToolRequisitionLinkModel?>> GetLocalByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _linkRepository.GetByIdAsync(id, cancellationToken);

    public async Task<DataResult<TmsRequisitionListResult>> RefreshWorkOrderStatusAsync(
        TmsWorkOrderRef workOrder,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
    {
        var remote = await _issuanceClient.GetRequisitionsByWorkOrderAsync(
            workOrder,
            TmsRequisitionFields.Summary,
            ifNoneMatch,
            cancellationToken);

        if (!remote.IsSuccess || remote.Value is null)
            return remote;

        if (remote.Value.NotModified)
            return remote;

        var syncedAt = DateTimeOffset.UtcNow;
        var localLinks = await _linkRepository.ListByWorkOrderAsync(workOrder, cancellationToken);
        if (!localLinks.IsSuccess || localLinks.Value is null)
            return DataResult<TmsRequisitionListResult>.Fail(localLinks.Error ?? DataError.Unknown("Не удалось загрузить локальные ссылки"));

        foreach (var item in remote.Value.Requisitions)
        {
            var link = localLinks.Value.FirstOrDefault(l => l.TmsRequisitionId == item.RequisitionId);
            if (link is null)
                continue;

            if (string.Equals(link.LastKnownStatus, item.Status, StringComparison.Ordinal)
                && string.Equals(link.SyncEtag, remote.Value.ETag, StringComparison.Ordinal))
            {
                continue;
            }

            var update = await _linkRepository.UpdateSyncStateAsync(
                link.Id,
                item.Status,
                remote.Value.ETag,
                syncedAt,
                cancellationToken);

            if (!update.IsSuccess)
            {
                return DataResult<TmsRequisitionListResult>.Fail(
                    update.Error ?? DataError.Unknown("Не удалось обновить локальный статус TMS"));
            }
        }

        return remote;
    }

    public async Task<DataResult> RefreshAllLocalAsync(
        int limit = 500,
        CancellationToken cancellationToken = default)
    {
        var local = await _linkRepository.ListAllAsync(limit, cancellationToken);
        if (!local.IsSuccess || local.Value is null)
            return DataResult.Fail(local.Error ?? DataError.Unknown("Не удалось загрузить локальные ссылки"));

        var workOrders = local.Value
            .Select(link => new TmsWorkOrderRef
            {
                Kind = link.WorkOrderKind == "schedule" ? TmsWorkOrderKind.Schedule : TmsWorkOrderKind.Request,
                Id = link.CmmsRequestId ?? link.CmmsScheduleId ?? Guid.Empty,
            })
            .Where(wo => wo.Id != Guid.Empty)
            .GroupBy(wo => (wo.Kind, wo.Id))
            .Select(g => g.First())
            .ToList();

        foreach (var workOrder in workOrders)
        {
            var refresh = await RefreshWorkOrderStatusAsync(workOrder, cancellationToken: cancellationToken);
            if (!refresh.IsSuccess)
                return DataResult.Fail(refresh.Error ?? DataError.Unknown("Не удалось обновить статусы TMS"));
        }

        return DataResult.Ok();
    }

    public async Task<DataResult<TmsCancelRequisitionsResult>> CancelForWorkOrderAsync(
        TmsWorkOrderRef workOrder,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var input = new TmsCancelRequisitionsInput
        {
            Reason = reason ?? "work_order_cancelled",
            CmmsRequestId = workOrder.Kind == TmsWorkOrderKind.Request ? workOrder.Id : null,
            CmmsScheduleId = workOrder.Kind == TmsWorkOrderKind.Schedule ? workOrder.Id : null,
        };

        var result = await _issuanceClient.CancelRequisitionsAsync(input, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return result;

        await _linkRepository.UpdateStatusByWorkOrderAsync(workOrder, TmsRequisitionStatuses.Cancelled, cancellationToken);
        return result;
    }

    public async Task<DataResult<TmsCancelRequisitionsResult>> CancelRequisitionAsync(
        Guid linkId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var linkResult = await _linkRepository.GetByIdAsync(linkId, cancellationToken);
        if (!linkResult.IsSuccess || linkResult.Value is null)
            return DataResult<TmsCancelRequisitionsResult>.Fail(linkResult.Error ?? DataError.Unknown("Заявка не найдена"));

        var link = linkResult.Value;
        if (!TmsRequisitionStatuses.IsCancellable(link.LastKnownStatus))
        {
            return DataResult<TmsCancelRequisitionsResult>.Fail(
                DataError.Validation("ToolRequisitionHistory_Cancel_NotAllowed"));
        }

        var input = new TmsCancelRequisitionsInput
        {
            RequisitionIds = [link.TmsRequisitionId],
            Reason = reason ?? "dispatcher_cancelled",
        };

        var result = await _issuanceClient.CancelRequisitionsAsync(input, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return result;

        var outcome = result.Value;
        var wasCancelled = outcome.Cancelled.Any(c => c.RequisitionId == link.TmsRequisitionId);
        var wasAlreadyCancelled = outcome.Skipped.Any(s =>
            s.RequisitionId == link.TmsRequisitionId
            && string.Equals(s.SkipReason, "already_cancelled", StringComparison.Ordinal));
        var wasAlreadyIssued = outcome.Skipped.Any(s =>
            s.RequisitionId == link.TmsRequisitionId
            && string.Equals(s.SkipReason, "already_issued", StringComparison.Ordinal));

        if (wasAlreadyIssued)
        {
            return DataResult<TmsCancelRequisitionsResult>.Fail(
                DataError.Validation("ToolRequisitionHistory_Cancel_NotAllowed"));
        }

        if (!wasCancelled && !wasAlreadyCancelled)
        {
            return DataResult<TmsCancelRequisitionsResult>.Fail(
                DataError.Validation("ToolRequisitionHistory_Cancel_NotApplied"));
        }

        var update = await _linkRepository.UpdateSyncStateAsync(
            link.Id,
            TmsRequisitionStatuses.Cancelled,
            link.SyncEtag,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!update.IsSuccess)
        {
            return DataResult<TmsCancelRequisitionsResult>.Fail(
                update.Error ?? DataError.Unknown("Не удалось обновить локальный статус TMS"));
        }

        return result;
    }

    private static ToolRequisitionCreateResult MapExistingToCreateResult(TmsToolRequisitionLinkModel link)
        => new()
        {
            RequisitionId = link.TmsRequisitionId,
            RequisitionNumber = TmsRequisitionDisplayNumber.Format(link.TmsRequisitionId),
            ClientReferenceId = link.ClientReferenceId,
            WarehouseId = link.WarehouseId,
            WarehouseName = link.WarehouseName,
            Status = link.LastKnownStatus,
            CreatedAt = link.CreatedAt ?? DateTimeOffset.UtcNow,
            Lines = Array.Empty<ToolRequisitionLineResult>(),
        };

    private static string ToDbWorkOrderKind(TmsWorkOrderKind kind)
        => kind switch
        {
            TmsWorkOrderKind.Request => "request",
            TmsWorkOrderKind.Schedule => "schedule",
            _ => "request",
        };
}
