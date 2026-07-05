using System;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

[Table("maintenance_schedule_departments")]
public class MaintenanceScheduleDepartmentModel : BaseModel
{
    [PrimaryKey("schedule_id", false)]
    [Column("schedule_id")]
    public Guid ScheduleId { get; set; }

    [PrimaryKey("repair_department_id", false)]
    [Column("repair_department_id")]
    public Guid RepairDepartmentId { get; set; }
}
