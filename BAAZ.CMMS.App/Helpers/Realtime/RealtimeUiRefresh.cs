using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Запускает перезагрузку страницы из Realtime-callback (фоновый поток WebSocket).</summary>
public static class RealtimeUiRefresh
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, DebounceSlot> Debouncers = new(StringComparer.Ordinal);

    public static void Enqueue(Func<Task> reloadAsync)
    {
        var queue = App.MainWindow?.DispatcherQueue;
        if (queue is not null && queue.TryEnqueue(() => _ = reloadAsync()))
            return;

        _ = reloadAsync();
    }

    /// <summary>
    /// Откладывает перезагрузку: серия Realtime-событий (пакет INSERT) → один вызов после паузы.
    /// </summary>
    public static void EnqueueDebounced(string key, Func<Task> reloadAsync, int delayMs = 400)
    {
        var queue = App.MainWindow?.DispatcherQueue;
        if (queue is null)
        {
            _ = RunFallbackDebouncedAsync(reloadAsync, delayMs);
            return;
        }

        if (!queue.TryEnqueue(() => ScheduleDebounced(key, reloadAsync, delayMs, queue)))
            _ = RunFallbackDebouncedAsync(reloadAsync, delayMs);
    }

    private static void ScheduleDebounced(string key, Func<Task> reloadAsync, int delayMs, DispatcherQueue queue)
    {
        lock (Gate)
        {
            if (!Debouncers.TryGetValue(key, out var slot))
            {
                slot = new DebounceSlot(queue);
                Debouncers[key] = slot;
            }

            slot.Schedule(reloadAsync, delayMs);
        }
    }

    private static async Task RunFallbackDebouncedAsync(Func<Task> reloadAsync, int delayMs)
    {
        await Task.Delay(delayMs).ConfigureAwait(false);
        await EnqueueOrRunAsync(reloadAsync).ConfigureAwait(false);
    }

    private static Task EnqueueOrRunAsync(Func<Task> reloadAsync)
    {
        var queue = App.MainWindow?.DispatcherQueue;
        if (queue is not null && queue.TryEnqueue(() => _ = reloadAsync()))
            return Task.CompletedTask;

        return reloadAsync();
    }

    private sealed class DebounceSlot
    {
        private readonly DispatcherQueue _queue;
        private readonly DispatcherQueueTimer _timer;
        private Func<Task>? _pending;

        public DebounceSlot(DispatcherQueue queue)
        {
            _queue = queue;
            _timer = _queue.CreateTimer();
            _timer.Tick += (_, _) => OnTick();
        }

        public void Schedule(Func<Task> reloadAsync, int delayMs)
        {
            _pending = reloadAsync;
            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(delayMs);
            _timer.IsRepeating = false;
            _timer.Start();
        }

        private void OnTick()
        {
            _timer.Stop();
            var action = _pending;
            _pending = null;
            if (action is not null)
                _ = action();
        }
    }
}
