namespace BAAZ.CMMS.App.Controls.MaintenanceTypePicker;

/// <summary>Элемент выбора вида ТО: ключ, заголовок и описание.</summary>
public sealed class MaintenanceTypePickerItem
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
