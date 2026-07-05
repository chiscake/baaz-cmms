using System;
using System.Collections.Generic;
using System.Linq;

namespace BAAZ.CMMS.App.Helpers.LocationHelpers;

/// <summary>Сериализация набора id зон заявок для inline-flyout CrudGrid.</summary>
public static class LocationScopeIdsWireFormat
{
    private const char Separator = ';';

    public static string Serialize(IEnumerable<Guid> ids) =>
        string.Join(Separator, ids.OrderBy(id => id));

    public static HashSet<Guid> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var result = new HashSet<Guid>();
        foreach (var part in value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
                result.Add(id);
        }

        return result;
    }

    public static bool AreEqual(string? left, string? right) =>
        string.Equals(Serialize(Parse(left)), Serialize(Parse(right)), StringComparison.Ordinal);
}
