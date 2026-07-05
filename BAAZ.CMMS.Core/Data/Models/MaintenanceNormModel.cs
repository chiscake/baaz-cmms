using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>
/// PostgREST-модель таблицы public.maintenance_norms — sparse override (UC-A5):
/// строка существует только когда хотя бы одно поле переопределено относительно
/// пресета категории, либо это standalone-норматив asset без category_id.
/// </summary>
[Table("maintenance_norms")]
public class MaintenanceNormModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("maintenance_type")]
    public string MaintenanceType { get; set; } = string.Empty;

    [Column("interval_days")]
    public int? IntervalDays { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("override_departments")]
    public bool OverrideDepartments { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
