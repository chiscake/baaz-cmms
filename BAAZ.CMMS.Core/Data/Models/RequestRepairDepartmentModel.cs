using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель junction public.request_repair_departments (Realtime payload).</summary>
[Table("request_repair_departments")]
public sealed class RequestRepairDepartmentModel : BaseModel
{
    [Column("request_id")]
    public Guid RequestId { get; set; }

    [Column("repair_department_id")]
    public Guid RepairDepartmentId { get; set; }

    [Column("assignee_id")]
    public Guid? AssigneeId { get; set; }
}
