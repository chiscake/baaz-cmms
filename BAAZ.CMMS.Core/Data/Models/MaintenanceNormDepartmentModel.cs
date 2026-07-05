using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

[Table("maintenance_norms_departments")]
public class MaintenanceNormDepartmentModel : BaseModel
{
    [PrimaryKey("norm_id", false)]
    [Column("norm_id")]
    public Guid NormId { get; set; }

    [PrimaryKey("repair_department_id", false)]
    [Column("repair_department_id")]
    public Guid RepairDepartmentId { get; set; }
}
