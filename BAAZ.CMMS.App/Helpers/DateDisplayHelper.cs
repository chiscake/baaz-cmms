using System;
using System.Globalization;

namespace BAAZ.CMMS.App.Helpers;

public static class DateDisplayHelper
{
    public const string WireFormat = "yyyy-MM-dd";

    public static string Format(DateOnly? value) =>
        value?.ToString("d", CultureInfo.CurrentCulture) ?? "—";

    public static string? ToWireFormat(DateOnly? value) =>
        value?.ToString(WireFormat, CultureInfo.InvariantCulture);

    public static DateOnly? ParseWireFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(
            value,
            WireFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    public static DateTimeOffset? ToDateTimeOffset(DateOnly? value) =>
        value.HasValue
            ? new DateTimeOffset(value.Value.ToDateTime(TimeOnly.MinValue))
            : null;
}
