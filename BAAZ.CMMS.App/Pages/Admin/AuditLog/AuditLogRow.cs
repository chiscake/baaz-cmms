using System;

using BAAZ.CMMS.App.Controls.CrudWorkbench;
using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Pages.Admin.AuditLog;

public sealed class AuditLogRow : ICrudGridRow
{
    public required Guid Id { get; init; }

    public required DateTimeOffset ChangedAt { get; init; }

    public required string ActorName { get; init; }

    public required string Operation { get; init; }

    public required string TableName { get; init; }

    public required string RecordKey { get; init; }

    public string? OldDataJson { get; init; }

    public string? NewDataJson { get; init; }

    public bool IsActive => true;

    public bool IsSelected { get; set; }

    public string GetCellText(string columnKey) => columnKey switch
    {
        "ChangedAt" => DateTimeDisplayHelper.Format(ChangedAt),
        "ActorName" => ActorName,
        "Operation" => AuditLogOperationHelper.GetLabel(Operation),
        "TableName" => TableName,
        "RecordKey" => RecordKey,
        "Id" => Id.ToString(),
        _ => string.Empty,
    };

    public static AuditLogRow FromListItem(AuditLogListItem item, string actorDisplay)
        => new()
        {
            Id = item.Id,
            ChangedAt = item.ChangedAt,
            ActorName = actorDisplay,
            Operation = item.Operation,
            TableName = item.TableName,
            RecordKey = item.RecordKey,
            OldDataJson = item.OldDataJson,
            NewDataJson = item.NewDataJson,
        };
}
