namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public enum CrudColumnEditKind
{
    ReadOnly,
    Text,
    EnumList,
    /// <summary>Иерархия локаций через <see cref="LocationPicker.LocationPicker"/>.</summary>
    LocationTree,
    /// <summary>Мультивыбор зон заявок через <see cref="LocationScopePicker.LocationScopePicker"/>.</summary>
    LocationScopeTree,
    /// <summary>Дата (без времени) через <see cref="CalendarView"/> в flyout.</summary>
    Date,
}
