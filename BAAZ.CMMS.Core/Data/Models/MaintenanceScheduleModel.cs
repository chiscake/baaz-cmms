using System;

using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.maintenance_schedule.</summary>
[Table("maintenance_schedule")]
public class MaintenanceScheduleModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("maintenance_type")]
    public string MaintenanceType { get; set; } = string.Empty;

    [Column("planned_date")]
    public DateOnly PlannedDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "scheduled";

    [Column("notify_dispatchers")]
    public bool NotifyDispatchers { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
