using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

[Table("category_maintenance_norms_departments")]
public class CategoryMaintenanceNormDepartmentModel : BaseModel
{
    [PrimaryKey("category_norm_id", false)]
    [Column("category_norm_id")]
    public Guid CategoryNormId { get; set; }

    [PrimaryKey("repair_department_id", false)]
    [Column("repair_department_id")]
    public Guid RepairDepartmentId { get; set; }
}
