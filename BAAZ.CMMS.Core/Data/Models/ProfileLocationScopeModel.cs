using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

[Table("profile_location_scopes")]
public class ProfileLocationScopeModel : BaseModel
{
    [PrimaryKey("profile_id", false)]
    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [PrimaryKey("location_id", false)]
    [Column("location_id")]
    public Guid LocationId { get; set; }
}
