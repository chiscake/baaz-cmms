using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.audit_log.</summary>
[Table("audit_log")]
public class AuditLogEntryModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("table_name")]
    public string AuditedTableName { get; set; } = string.Empty;

    [Column("record_id")]
    public Guid? RecordId { get; set; }

    [Column("record_key")]
    public string RecordKey { get; set; } = string.Empty;

    [Column("operation")]
    public string Operation { get; set; } = string.Empty;

    [Column("changed_by")]
    public Guid? ChangedBy { get; set; }

    [Column("changed_at")]
    public DateTimeOffset ChangedAt { get; set; }

    [Column("old_data")]
    public JObject? OldData { get; set; }

    [Column("new_data")]
    public JObject? NewData { get; set; }
}
