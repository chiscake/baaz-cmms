using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель public.profiles (runtime).</summary>
[Table("profiles")]
public class ProfileModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("role")]
    public string Role { get; set; } = "requester";

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("location_id")]
    public Guid? LocationId { get; set; }

    [Column("repair_department_id")]
    public Guid? RepairDepartmentId { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
