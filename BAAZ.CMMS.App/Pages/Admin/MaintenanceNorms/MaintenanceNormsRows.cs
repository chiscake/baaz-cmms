using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using BAAZ.CMMS.App.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Pages.Admin.MaintenanceNorms;

/// <summary>Строка master-списка категорий (вкладка «Категории»).</summary>
public sealed class CategoryRow
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public override string ToString() => Name;
}

/// <summary>Строка master-списка оборудования (вкладка «По оборудованию»).</summary>
public sealed class AssetPickerRow
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }

    public override string ToString() => DisplayName;
}

/// <summary>Редактируемый слот пресета категории (ТО-1 / ТО-2 / КР) — вкладка «Категории».</summary>
public sealed partial class CategorySlotEditor : ObservableObject
{
    public required string MaintenanceType { get; init; }

    public required string Header { get; init; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    /// <summary>Развёрнут ли Expander — независимо от <see cref="IsEnabled"/>.</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial string IntervalDaysText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public ObservableCollection<CheckableItem> Departments { get; } = [];

    public double IntervalDays
    {
        get => int.TryParse(IntervalDaysText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : double.NaN;
        set
        {
            var text = double.IsNaN(value) ? string.Empty : ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(IntervalDaysText, text, StringComparison.Ordinal))
                IntervalDaysText = text;
        }
    }

    partial void OnIntervalDaysTextChanged(string value) => OnPropertyChanged(nameof(IntervalDays));
}

/// <summary>
/// Редактируемый слот-override норматива объекта (ТО-1 / ТО-2 / КР) — вкладка «По оборудованию».
/// Показывает read-only пресет категории и редактируемый индивидуальный override.
/// </summary>
public sealed partial class AssetSlotEditor : ObservableObject
{
    public required string MaintenanceType { get; init; }

    public required string Header { get; init; }

    // Пресет категории — read-only.
    public string PresetSummary { get; set; } = string.Empty;

    // Индивидуальный override.
    [ObservableProperty]
    public partial bool HasOverride { get; set; }

    /// <summary>Развёрнут ли Expander — независимо от <see cref="HasOverride"/>.</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial string IntervalDaysText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    public double IntervalDays
    {
        get => int.TryParse(IntervalDaysText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) ? days : double.NaN;
        set
        {
            var text = double.IsNaN(value) ? string.Empty : ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(IntervalDaysText, text, StringComparison.Ordinal))
                IntervalDaysText = text;
        }
    }

    partial void OnIntervalDaysTextChanged(string value) => OnPropertyChanged(nameof(IntervalDays));

    public HashSet<Guid> PresetDepartmentIds { get; set; } = [];

    public ObservableCollection<CheckableItem> Departments { get; } = [];

    public IReadOnlyList<Guid> SelectedDepartmentIds =>
        Departments.Where(d => d.IsChecked).Select(d => d.Id).ToList();

    public bool HasDepartmentOverride =>
        !SelectedDepartmentIds.ToHashSet().SetEquals(PresetDepartmentIds);

    public void ResetDepartmentsToPreset()
    {
        foreach (var dept in Departments)
            dept.IsChecked = PresetDepartmentIds.Contains(dept.Id);
    }

    // Статус цикла + pending schedule.
    public string StatusSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPendingScheduleVisible { get; set; }

    public string PendingScheduleTooltip { get; set; } = string.Empty;

    // Исходное значение интервала на момент загрузки — для решения о показе диалога политики.
    public int? OriginalIntervalDays { get; set; }
}

/// <summary>Строка вкладки «Все нормативы» (аудит).</summary>
public sealed class AuditNormRow
{
    public required string MaintenanceTypeLabel { get; init; }

    public required string IntervalSummary { get; init; }

    public string? NextMaintenanceText { get; init; }
}

/// <summary>Группа строк аудита по одному объекту.</summary>
public sealed class AuditAssetGroup
{
    public Guid AssetId { get; init; }

    public required string Header { get; init; }

    public required ObservableCollection<AuditNormRow> Items { get; init; }
}
