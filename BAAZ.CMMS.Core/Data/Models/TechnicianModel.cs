using System;

using Newtonsoft.Json;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.technicians (runtime, не EF scaffold).</summary>
[Table("technicians")]
public class TechnicianModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("specialty")]
    public string Specialty { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("repair_department_id")]
    public Guid? RepairDepartmentId { get; set; }

    /// <summary>Заполняется БД (default/trigger); не отправлять при insert/update из клиента.</summary>
    [Column("created_at", NullValueHandling.Ignore, true, true)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at", NullValueHandling.Ignore, true, false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
