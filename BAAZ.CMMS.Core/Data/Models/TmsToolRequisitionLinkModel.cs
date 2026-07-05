using System;

using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>Локальная ссылка CMMS на заявку выдачи в TMS (public.tms_tool_requisition_links).</summary>
[Table("tms_tool_requisition_links")]
public sealed class TmsToolRequisitionLinkModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("client_reference_id")]
    public Guid ClientReferenceId { get; set; }

    [Column("tms_requisition_id")]
    public Guid TmsRequisitionId { get; set; }

    [Column("warehouse_id")]
    public Guid WarehouseId { get; set; }

    [Column("warehouse_name")]
    public string? WarehouseName { get; set; }

    [Column("work_order_kind")]
    public string WorkOrderKind { get; set; } = "request";

    [Column("cmms_request_id")]
    public Guid? CmmsRequestId { get; set; }

    [Column("cmms_schedule_id")]
    public Guid? CmmsScheduleId { get; set; }

    [Column("last_known_status")]
    public string LastKnownStatus { get; set; } = "new";

    [Column("last_synced_at")]
    public DateTimeOffset? LastSyncedAt { get; set; }

    [Column("sync_etag")]
    public string? SyncEtag { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
