using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель представления public.asset_maintenance_status.</summary>
[Table("asset_maintenance_status")]
public class AssetMaintenanceStatusModel : BaseModel
{
    [PrimaryKey("norm_id", false)]
    [Column("norm_id")]
    public Guid NormId { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("maintenance_type")]
    public string MaintenanceType { get; set; } = string.Empty;

    [Column("interval_days")]
    public int IntervalDays { get; set; }

    [Column("last_maintenance_date")]
    public DateOnly? LastMaintenanceDate { get; set; }

    [Column("next_maintenance_date")]
    public DateOnly? NextMaintenanceDate { get; set; }
}
