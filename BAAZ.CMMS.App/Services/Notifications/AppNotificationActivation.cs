using System;

namespace BAAZ.CMMS.App.Services.Notifications;

/// <summary>Отложенная навигация после cold-start по клику на toast (до готовности shell).</summary>
public static class AppNotificationActivation
{
    private static readonly object Gate = new();
    private static string? _pendingPageKey;
    private static Guid? _pendingRequestId;

    public static void SetPending(string? pageKey, Guid? requestId)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
            return;

        lock (Gate)
        {
            _pendingPageKey = pageKey;
            _pendingRequestId = requestId;
        }
    }

    public static bool TryConsume(out string? pageKey, out Guid? requestId)
    {
        lock (Gate)
        {
            pageKey = _pendingPageKey;
            requestId = _pendingRequestId;
            _pendingPageKey = null;
            _pendingRequestId = null;
            return pageKey is not null;
        }
    }

    public static void ParseArguments(string? argument, out string? pageKey, out Guid? requestId)
    {
        pageKey = null;
        requestId = null;

        if (string.IsNullOrWhiteSpace(argument))
            return;

        foreach (var segment in argument.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (key.Equals("page", StringComparison.OrdinalIgnoreCase))
                pageKey = value;
            else if (key.Equals("requestId", StringComparison.OrdinalIgnoreCase)
                     && Guid.TryParse(value, out var id))
                requestId = id;
        }
    }
}
