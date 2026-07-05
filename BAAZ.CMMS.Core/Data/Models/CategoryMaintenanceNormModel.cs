using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.category_maintenance_norms (пресеты, UC-A5).</summary>
[Table("category_maintenance_norms")]
public class CategoryMaintenanceNormModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("maintenance_type")]
    public string MaintenanceType { get; set; } = string.Empty;

    [Column("interval_days")]
    public int IntervalDays { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
