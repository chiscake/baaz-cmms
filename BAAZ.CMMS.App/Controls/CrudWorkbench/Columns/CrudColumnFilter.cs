using CommunityToolkit.Mvvm.ComponentModel;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Активный фильтр по колонке (бейдж «поле = значение»).</summary>
public sealed partial class CrudColumnFilter : ObservableObject
{
    [ObservableProperty]
    public partial string ColumnKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ColumnHeader { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayValue { get; set; } = string.Empty;

    public string BadgeText => $"{ColumnHeader} = {DisplayValue}";

    partial void OnColumnKeyChanged(string value) => OnPropertyChanged(nameof(BadgeText));

    partial void OnColumnHeaderChanged(string value) => OnPropertyChanged(nameof(BadgeText));

    partial void OnDisplayValueChanged(string value) => OnPropertyChanged(nameof(BadgeText));
}
