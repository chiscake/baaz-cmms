using BAAZ.CMMS.Core.Data.Attributes;

using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.assets active (runtime, не EF scaffold).</summary>
[Table("assets")]
public class AssetModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("asset_number")]
    [Unique]
    public string AssetNumber { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("location_id")]
    public Guid LocationId { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("manufacturer")]
    public string? Manufacturer { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("serial_number")]
    public string? SerialNumber { get; set; }

    [Column("commissioning_date")]
    public DateOnly? CommissioningDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
