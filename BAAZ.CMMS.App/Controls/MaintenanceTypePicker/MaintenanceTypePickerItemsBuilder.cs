using System.Collections.Generic;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using BAAZ.CMMS.App.Localization;
using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.App.Controls.MaintenanceTypePicker;

/// <summary>Сборка элементов <see cref="MaintenanceTypeCardPicker"/> из нормативов или дефолтных подписей.</summary>
public static class MaintenanceTypePickerItemsBuilder
{
    public static IList<MaintenanceTypePickerItem> BuildDefault()
        => BuildFromSlots(null);

    public static IList<MaintenanceTypePickerItem> BuildFromSlots(IReadOnlyList<EffectiveNormSlot>? slots)
    {
        var slotsByType = slots?.ToDictionary(s => s.MaintenanceType)
            ?? new Dictionary<string, EffectiveNormSlot>();

        return MaintenanceTypeLabels.All
            .Select(type => CreateItem(type, slotsByType.GetValueOrDefault(type)))
            .ToList();
    }

    private static MaintenanceTypePickerItem CreateItem(string type, EffectiveNormSlot? slot)
    {
        var description = !string.IsNullOrWhiteSpace(slot?.EffectiveDescription)
            ? slot!.EffectiveDescription!
            : ResourceStrings.Get("MaintenanceSchedule_Details_Description_Empty");

        return new MaintenanceTypePickerItem
        {
            Key = type,
            Title = MaintenanceTypeLabels.Get(type),
            Description = description,
        };
    }
}
