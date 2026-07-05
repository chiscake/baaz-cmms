using BAAZ.CMMS.Core.Data.Attributes;

using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BAAZ.CMMS.Core.Data.Models;

/// <summary>PostgREST-модель таблицы public.requests (runtime, не EF scaffold).</summary>
[Table("requests")]
public sealed class RequestModel : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("request_number")]
    [Unique]
    public string RequestNumber { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("requester_id")]
    public Guid RequesterId { get; set; }

    [Column("asset_id")]
    public Guid? AssetId { get; set; }
}
