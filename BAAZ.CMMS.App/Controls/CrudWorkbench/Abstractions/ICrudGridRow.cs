namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Строка с текстовым представлением ячеек для <see cref="CrudDataGrid"/>.
/// </summary>
public interface ICrudGridRow : ICrudRow
{
    /// <summary>Текст ячейки по ключу колонки (<see cref="CrudColumnDefinition.Key"/>).</summary>
    string? GetCellText(string columnKey);

    /// <summary>
    /// Значение для inline-редактора (id для FK/enum, иначе как <see cref="GetCellText"/>).
    /// </summary>
    string? GetCellEditValue(string columnKey) => GetCellText(columnKey);
}
