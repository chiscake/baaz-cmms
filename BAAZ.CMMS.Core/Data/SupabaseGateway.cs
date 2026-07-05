using Supabase.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Realtime;

using BAAZ.CMMS.Core.Services;

namespace BAAZ.CMMS.Core.Data;

public sealed class SupabaseGateway : ISupabaseGateway
{
    private readonly ISupabaseClientProvider _clientProvider;

    public SupabaseGateway(ISupabaseClientProvider clientProvider)
    {
        _clientProvider = clientProvider;
    }

    public bool HasSession =>
        _clientProvider.Client.Auth.CurrentSession?.AccessToken is { Length: > 0 };

    public ISupabaseTable<T, RealtimeChannel> From<T>() where T : BaseModel, new()
        => _clientProvider.Client.From<T>();
}
