using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Helpers.Settings;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Сохранение видимости колонок CrudDataGrid в <see cref="SettingsHelper.CrudGridColumnVisibilityJson"/>.
/// Формат: { "Personnel": { "FullName": true, "Specialty": false }, ... }
/// </summary>
internal static class CrudColumnVisibilityStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static IReadOnlyDictionary<string, bool>? Load(string gridKey)
    {
        if (string.IsNullOrEmpty(gridKey)) return null;

        var all = Deserialize(SettingsHelper.Current.CrudGridColumnVisibilityJson);
        return all.TryGetValue(gridKey, out var cols) ? cols : null;
    }

    public static void Save(string gridKey, IReadOnlyDictionary<string, bool> columnVisibility)
    {
        if (string.IsNullOrEmpty(gridKey)) return;

        var all = Deserialize(SettingsHelper.Current.CrudGridColumnVisibilityJson);
        all[gridKey] = columnVisibility.ToDictionary(static p => p.Key, static p => p.Value);
        SettingsHelper.Current.CrudGridColumnVisibilityJson = Serialize(all);
    }

    public static void Remove(string gridKey)
    {
        if (string.IsNullOrEmpty(gridKey)) return;

        var all = Deserialize(SettingsHelper.Current.CrudGridColumnVisibilityJson);
        if (!all.Remove(gridKey)) return;
        SettingsHelper.Current.CrudGridColumnVisibilityJson = Serialize(all);
    }

    private static Dictionary<string, Dictionary<string, bool>> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(json, JsonOptions)
                ?? new Dictionary<string, Dictionary<string, bool>>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, Dictionary<string, bool>>();
        }
    }

    private static string Serialize(Dictionary<string, Dictionary<string, bool>> data)
        => JsonSerializer.Serialize(data, JsonOptions);
}
