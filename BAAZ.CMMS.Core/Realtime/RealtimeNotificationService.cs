using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Services;

using Supabase.Gotrue;
using Supabase.Postgrest.Models;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;

using GotrueConstants = Supabase.Gotrue.Constants;
using RealtimeConstants = Supabase.Realtime.Constants;

namespace BAAZ.CMMS.Core.Realtime;

/// <summary>
/// Сессионный сервис Supabase Realtime: один канал, несколько таблиц.
/// JWT обязателен (SetAuth) — иначе RLS на локальном Docker отфильтрует все события.
/// </summary>
public sealed class RealtimeNotificationService : IRealtimeNotificationService, IDisposable
{
    private static readonly string[] WatchedTables =
    [
        "requests",
        "maintenance_schedule",
        "work_reports",
        "request_repair_departments",
    ];

    private readonly ISupabaseClientProvider _clientProvider;
    private readonly IConnectionService _connectionService;
    private readonly HashSet<string> _handledPageKeys = [];

    private RealtimeChannel? _channel;
    private bool _started;
    private bool _wantsSubscription;
    private bool _authHooked;

    public event EventHandler<RealtimeEvent>? EventReceived;
    public event EventHandler<bool>? ConnectionStateChanged;

    public RealtimeNotificationService(
        ISupabaseClientProvider clientProvider,
        IConnectionService connectionService)
    {
        _clientProvider = clientProvider;
        _connectionService = connectionService;
        _connectionService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _wantsSubscription = true;

        if (_started)
        {
            Debug.WriteLine("[Realtime] StartAsync skipped — already started");
            return;
        }

        try
        {
            var client = _clientProvider.Client;
            EnsureAuthHook(client);

            var accessToken = client.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Debug.WriteLine("[Realtime] StartAsync deferred — no JWT (RLS requires authenticated Realtime)");
                return;
            }

            ApplyRealtimeAuth(accessToken);

            if (client.Realtime.Socket?.IsConnected != true)
            {
                Debug.WriteLine("[Realtime] Connecting WebSocket…");
                await client.Realtime.ConnectAsync();
                Debug.WriteLine($"[Realtime] WebSocket connected: {client.Realtime.Socket?.IsConnected}");
            }

            Debug.WriteLine("[Realtime] Creating channel public-cmms…");
            _channel = client.Realtime.Channel("public-cmms");

            foreach (var table in WatchedTables)
                _channel.Register(new PostgresChangesOptions("public", table));

            _channel.AddPostgresChangeHandler(
                PostgresChangesOptions.ListenType.All,
                HandleChange);

            Debug.WriteLine($"[Realtime] Subscribe at {DateTime.UtcNow:HH:mm:ss.fff}Z");
            await _channel.Subscribe();

            _started = true;
            ConnectionStateChanged?.Invoke(this, true);
            Debug.WriteLine($"[Realtime] Subscribed to {string.Join(", ", WatchedTables)}");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"[Realtime] StartAsync IO error (Realtime optional): {ex.Message}");
            await TeardownChannelAsync();
            ConnectionStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] StartAsync failed ({ex.GetType().Name}): {ex.Message}");
            await TeardownChannelAsync();
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public async Task StopAsync()
    {
        _wantsSubscription = false;
        _handledPageKeys.Clear();
        await TeardownChannelAsync();
        Debug.WriteLine("[Realtime] Stopped");
    }

    private void EnsureAuthHook(Supabase.Client client)
    {
        if (_authHooked)
            return;

        client.Auth.AddStateChangedListener(OnAuthStateChanged);
        _authHooked = true;
    }

    private async void OnAuthStateChanged(object? _, GotrueConstants.AuthState newState)
    {
        if (newState is not (GotrueConstants.AuthState.SignedIn or GotrueConstants.AuthState.TokenRefreshed))
            return;

        var token = _clientProvider.Client.Auth.CurrentSession?.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            ApplyRealtimeAuth(token);

            if (_wantsSubscription && !_started)
                await StartAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] OnAuthStateChanged error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    private void ApplyRealtimeAuth(string accessToken)
    {
        _clientProvider.Client.Realtime.SetAuth(accessToken);
        Debug.WriteLine("[Realtime] SetAuth applied (JWT for RLS)");
    }

    private async void OnConnectionStateChanged(object? sender, bool connected)
    {
        if (!_wantsSubscription)
            return;

        if (connected)
        {
            if (!_started)
                await StartAsync();
        }
        else if (_started)
        {
            await TeardownChannelAsync();
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    private async Task TeardownChannelAsync()
    {
        if (!_started && _channel is null)
            return;

        _started = false;

        try
        {
            _channel?.Unsubscribe();
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"[Realtime] Teardown IO (socket closed): {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] Teardown error ({ex.GetType().Name}): {ex.Message}");
        }
        finally
        {
            _channel = null;
        }

        await Task.CompletedTask;
    }

    private void HandleChange(IRealtimeChannel channel, PostgresChangesResponse change)
    {
        try
        {
            var table = ResolveTableName(change);
            if (string.IsNullOrEmpty(table) || !IsWatchedTable(table))
            {
                Debug.WriteLine($"[Realtime] HandleChange ignored — table unresolved or not watched");
                return;
            }

            var eventType = ResolveEventType(change);
            if (eventType is null)
                return;

            var (recordId, payload) = ResolveRecord(table, change);

            var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
            var key = $"{table}|{eventType}|{recordId}|{bucket}";

            lock (_handledPageKeys)
            {
                if (!_handledPageKeys.Add(key))
                    return;

                if (_handledPageKeys.Count > 500)
                    _handledPageKeys.Clear();
            }

            var ev = new RealtimeEvent
            {
                Table = table,
                EventType = eventType.Value,
                RecordId = recordId,
                Payload = payload,
            };

            Debug.WriteLine($"[Realtime] {ev.Table} {ev.EventType} id={ev.RecordId}");
            EventReceived?.Invoke(this, ev);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] HandleChange error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    private static RealtimeEventType? ResolveEventType(PostgresChangesResponse change)
    {
        var mapped = change.Event switch
        {
            RealtimeConstants.EventType.Insert => (RealtimeEventType?)RealtimeEventType.Insert,
            RealtimeConstants.EventType.Update => RealtimeEventType.Update,
            RealtimeConstants.EventType.Delete => RealtimeEventType.Delete,
            _ => null,
        };

        if (mapped is not null)
            return mapped;

        var type = change.Payload?.Data?._type;
        return type switch
        {
            "INSERT" => RealtimeEventType.Insert,
            "UPDATE" => RealtimeEventType.Update,
            "DELETE" => RealtimeEventType.Delete,
            _ => null,
        };
    }

    private static string? ResolveTableName(PostgresChangesResponse change)
    {
        var table = change.Payload?.Data?.Table;
        if (!string.IsNullOrEmpty(table))
            return table;

        if (TryModel<RequestModel>(change))
            return "requests";
        if (TryModel<MaintenanceScheduleModel>(change))
            return "maintenance_schedule";
        if (TryModel<WorkReportModel>(change))
            return "work_reports";
        if (TryModel<RequestRepairDepartmentModel>(change))
            return "request_repair_departments";

        return null;
    }

    private static bool TryModel<TModel>(PostgresChangesResponse change)
        where TModel : BaseModel, new()
    {
        try
        {
            return change.Model<TModel>() is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsWatchedTable(string table)
    {
        foreach (var watched in WatchedTables)
        {
            if (string.Equals(watched, table, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static (Guid? RecordId, object? Payload) ResolveRecord(
        string table,
        PostgresChangesResponse change)
    {
        try
        {
            return table switch
            {
                "requests" => FromModel(change.Model<RequestModel>(), m => m.Id),
                "maintenance_schedule" => FromModel(change.Model<MaintenanceScheduleModel>(), m => m.Id),
                "work_reports" => FromModel(change.Model<WorkReportModel>(), m => m.Id),
                "request_repair_departments" => FromJunction(change.Model<RequestRepairDepartmentModel>()),
                _ => (null, null),
            };
        }
        catch
        {
            return (null, null);
        }
    }

    private static (Guid? RecordId, object? Payload) FromModel<TModel>(
        TModel? model,
        Func<TModel, Guid> idSelector)
        where TModel : class
    {
        if (model is null)
            return (null, null);

        var id = idSelector(model);
        return id == Guid.Empty ? (null, model) : (id, model);
    }

    private static (Guid? RecordId, object? Payload) FromJunction(RequestRepairDepartmentModel? model)
    {
        if (model is null || model.RequestId == Guid.Empty)
            return (null, null);

        return (model.RequestId, model);
    }

    public void Dispose()
    {
        _connectionService.ConnectionStateChanged -= OnConnectionStateChanged;
        _ = StopAsync();
    }
}
