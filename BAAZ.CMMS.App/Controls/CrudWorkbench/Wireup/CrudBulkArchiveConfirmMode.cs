namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Когда показывать confirm перед bulk archive/ban в тулбаре.</summary>
public enum CrudBulkArchiveConfirmMode
{
    /// <summary>Диалог перед BulkArchiveCommand (по умолчанию).</summary>
    Always,

    /// <summary>Без диалога — CrudWorkbenchPage вызывает BulkArchiveCommand напрямую.</summary>
    Never,
}
