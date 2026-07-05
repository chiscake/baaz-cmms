using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.work_reports (Realtime payload).</summary>
[Table("work_reports")]
public sealed class WorkReportModel : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("request_id")]
    public Guid? RequestId { get; set; }

    [Column("schedule_id")]
    public Guid? ScheduleId { get; set; }

    [Column("repair_department_id")]
    public Guid RepairDepartmentId { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
