namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Поведение кнопки expand / double-tap по строке CrudDataGrid.
/// </summary>
public enum CrudRowOpenMode
{
    /// <summary>Открыть панель редактора справа (по умолчанию).</summary>
    Editor,

    /// <summary>Вернуть выбранную запись без редактора (picker / overflow).</summary>
    Pick,
}
