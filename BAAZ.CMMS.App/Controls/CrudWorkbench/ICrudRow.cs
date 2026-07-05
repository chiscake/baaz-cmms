using System;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Интерфейс строки для CrudWorkbench — идентификатор, выделение, архивный статус.</summary>
public interface ICrudRow
{
    Guid Id { get; }
    bool IsSelected { get; set; }
    bool IsActive { get; }
}
