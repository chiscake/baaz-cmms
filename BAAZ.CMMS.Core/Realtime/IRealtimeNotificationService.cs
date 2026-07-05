using System;
using System.Threading;
using System.Threading.Tasks;

namespace BAAZ.CMMS.Core.Realtime;

/// <summary>
/// Сессионный сервис подписки на Supabase Realtime для таблиц requests и maintenance_schedule.
/// Запускается после логина, останавливается при выходе.
/// </summary>
public interface IRealtimeNotificationService
{
    /// <summary>Подписаться на postgres_changes. Вызывается после успешной аутентификации.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Отписаться и освободить ресурсы. Вызывается при выходе или смене клиента.</summary>
    Task StopAsync();

    /// <summary>Событие, публикуемое при получении INSERT/UPDATE из Realtime.</summary>
    event EventHandler<RealtimeEvent> EventReceived;

    /// <summary>true — канал подписан; false — отключён или недоступен.</summary>
    event EventHandler<bool>? ConnectionStateChanged;
}
