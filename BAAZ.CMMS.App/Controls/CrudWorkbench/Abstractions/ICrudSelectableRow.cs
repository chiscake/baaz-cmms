using System;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Строка, для которой можно отключить выделение (например admin-учётки).</summary>
public interface ICrudSelectableRow
{
    bool IsSelectable { get; }
}
