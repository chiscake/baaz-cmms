using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Helpers.Settings;

namespace BAAZ.CMMS.App.Pages.Dispatcher.MaintenanceSchedule;

internal static class MaintenanceSchedulePrefsStore
{
    private const string DefaultJson = "{}";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static MaintenanceSchedulePrefs Load()
    {
        var json = SettingsHelper.Current.MaintenanceSchedulePrefsJson;
        if (string.IsNullOrWhiteSpace(json))
            return new MaintenanceSchedulePrefs();

        try
        {
            return JsonSerializer.Deserialize<MaintenanceSchedulePrefs>(json, JsonOptions)
                ?? new MaintenanceSchedulePrefs();
        }
        catch (JsonException)
        {
            return new MaintenanceSchedulePrefs();
        }
    }

    public static void Save(MaintenanceSchedulePrefs prefs)
    {
        SettingsHelper.Current.MaintenanceSchedulePrefsJson =
            JsonSerializer.Serialize(prefs, JsonOptions);
    }

    public static void Update(Action<MaintenanceSchedulePrefs> mutate)
    {
        var prefs = Load();
        mutate(prefs);
        Save(prefs);
    }
}

internal sealed class MaintenanceSchedulePrefs
{
    public int SelectedViewIndex { get; set; }

    public int ZoomPreset { get; set; } = (int)ScheduleZoomPreset.Month;

    public double SplitPaneStarWeight { get; set; } = 1.5;

    public List<Guid> CollapsedLocationIds { get; set; } = [];
}

public enum ScheduleZoomPreset
{
    Week = 0,
    Month = 1,
    Quarter = 2,
}
