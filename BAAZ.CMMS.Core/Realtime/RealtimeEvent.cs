using System;

using Supabase.Realtime.PostgresChanges;

namespace BAAZ.CMMS.Core.Realtime;

public enum RealtimeEventType { Insert, Update, Delete }

/// <summary>
/// Единое событие, публикуемое IRealtimeNotificationService для всех подписчиков.
/// </summary>
public sealed class RealtimeEvent
{
    public required string Table { get; init; }
    public required RealtimeEventType EventType { get; init; }
    public Guid? RecordId { get; init; }

    /// <summary>Snapshot новой строки (если доступен). Тип — строго-типизированная модель или null.</summary>
    public object? Payload { get; init; }
}
