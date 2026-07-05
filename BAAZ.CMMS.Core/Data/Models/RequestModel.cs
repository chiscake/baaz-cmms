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

    [Column("inventory_id")]
    public Guid? InventoryId { get; set; }

    [Column("inventory_kind")]
    public string? InventoryKind { get; set; }

    [Column("inventory_name")]
    public string? InventoryName { get; set; }

    [Column("inventory_serial")]
    public string? InventorySerial { get; set; }

    [Column("inventory_type_name")]
    public string? InventoryTypeName { get; set; }

    [Column("inventory_source")]
    public string InventorySource { get; set; } = "tms";

    [Column("inventory_handoff_mode")]
    public string? InventoryHandoffMode { get; set; }

    [Column("inventory_warehouse_name")]
    public string? InventoryWarehouseName { get; set; }

    [Column("inventory_received_at")]
    public DateTimeOffset? InventoryReceivedAt { get; set; }
}
