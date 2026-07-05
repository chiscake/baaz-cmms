using Supabase.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Realtime;

namespace BAAZ.CMMS.Core.Data;

/// <summary>Единая точка доступа к Supabase PostgREST SDK.</summary>
public interface ISupabaseGateway
{
    /// <summary>Возвращает построитель запросов PostgREST для модели T.</summary>
    ISupabaseTable<T, RealtimeChannel> From<T>() where T : BaseModel, new();

    bool HasSession { get; }
}
