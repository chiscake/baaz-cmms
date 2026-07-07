using System;
using System.Globalization;

namespace BAAZ.CMMS.App.Helpers;

/// <summary>Форматирование UTC/timestamptz для отображения в локальном часовом поясе ОС.</summary>
public static class DateTimeDisplayHelper
{
    public static string Format(DateTimeOffset? value)
    {
        if (!value.HasValue) return "—";
        return value.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }

    public static string Format(DateTime? value)
    {
        if (!value.HasValue) return "—";

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value.Value),
            DateTimeKind.Local => new DateTimeOffset(value.Value.ToUniversalTime(), TimeSpan.Zero),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)),
        };

        return Format(utc);
    }
}
