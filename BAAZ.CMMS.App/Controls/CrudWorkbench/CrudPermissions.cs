namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Разрешения для текущего пользователя на странице CrudWorkbench.</summary>
public sealed class CrudPermissions
{
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanUpdate { get; init; }
    public bool CanArchive { get; init; }
    public bool CanBulkArchive { get; init; }
    public bool CanHardDelete { get; init; }
    public bool CanInlineEdit { get; init; }
    public bool CanChangeDepartment { get; init; }
}
