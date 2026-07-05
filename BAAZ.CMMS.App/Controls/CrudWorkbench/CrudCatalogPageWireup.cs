using System;
using System.Linq;
using System.Threading.Tasks;

using BAAZ.CMMS.App.Localization;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>Настройки общих обработчиков catalog-страницы CrudWorkbench.</summary>
public sealed class CrudCatalogPageOptions<TRow>
    where TRow : class, ICrudGridRow
{
    public required string ResourcePrefix { get; init; }

    public required Func<TRow, Task> ArchiveRowAsync { get; init; }

    public required Func<TRow, Task> DeleteRowAsync { get; init; }

    public CrudBulkArchiveConfirmMode BulkArchiveConfirmMode { get; init; } =
        CrudBulkArchiveConfirmMode.Always;

    public bool ConfirmArchiveRow { get; init; } = true;

    public Func<TRow, string>? GetRowDisplayName { get; init; }

    public Func<TRow, string?>? GetArchiveContextMenuLabel { get; init; }

    public Func<TRow, bool>? CanEditRow { get; init; }

    public Func<TRow, bool>? CanMutateRow { get; init; }

    public Func<TRow, string, bool, string>? GetArchiveRowTitleKey { get; init; }

    public Func<TRow, string, bool, string>? GetArchiveRowMessageKey { get; init; }

    public Func<bool, bool, string>? GetArchiveBulkTitleKey { get; init; }

    public Func<bool, bool, string>? GetArchiveBulkMessageKey { get; init; }

    /// <summary>Замена стандартного bulk archive confirm (напр. Users ban/unban).</summary>
    public Func<Task>? BulkArchiveAsync { get; init; }
}

/// <summary>
/// Подключает общие обработчики CrudWorkbench к catalog-странице (без наследования от <see cref="Microsoft.UI.Xaml.Controls.Page"/>).
/// </summary>
public sealed class CrudCatalogPageWireup<TViewModel, TRow>
    where TViewModel : CrudWorkbenchViewModelBase<TRow>
    where TRow : class, ICrudGridRow
{
    private readonly TViewModel _viewModel;
    private readonly CrudWorkbenchPage _workbench;
    private readonly CrudCatalogPageOptions<TRow> _options;

    public CrudCatalogPageWireup(
        TViewModel viewModel,
        CrudWorkbenchPage workbench,
        CrudCatalogPageOptions<TRow> options)
    {
        _viewModel = viewModel;
        _workbench = workbench;
        _options = options;
    }

    public void Wire()
    {
        _workbench.CellContextRequested += OnCellContextRequested;
        _workbench.HardDeleteClicked += OnHardDeleteClicked;

        if (_options.BulkArchiveConfirmMode == CrudBulkArchiveConfirmMode.Always)
            _workbench.ArchiveClicked += OnArchiveClicked;
    }

    public Task LoadAsync() => _viewModel.OnPageLoadedAsync();

    private string GetRowDisplayName(TRow row) =>
        _options.GetRowDisplayName?.Invoke(row)
        ?? row.GetCellText("Name")
        ?? row.GetCellText("FullName")
        ?? row.GetCellText("FullPath")
        ?? row.GetCellText("Email")
        ?? row.Id.ToString();

    private string? GetArchiveContextMenuLabel(TRow row) =>
        _options.GetArchiveContextMenuLabel?.Invoke(row)
        ?? (row.IsActive
            ? ResourceStrings.Get($"{_options.ResourcePrefix}_Context_Archive")
            : ResourceStrings.Get($"{_options.ResourcePrefix}_Context_Restore"));

    private string GetArchiveRowTitleKey(TRow row, bool archiving) =>
        _options.GetArchiveRowTitleKey?.Invoke(row, _options.ResourcePrefix, archiving)
        ?? (archiving
            ? $"{_options.ResourcePrefix}_Archive_Title"
            : $"{_options.ResourcePrefix}_Restore_Title");

    private string GetArchiveRowMessageKey(TRow row, bool archiving) =>
        _options.GetArchiveRowMessageKey?.Invoke(row, _options.ResourcePrefix, archiving)
        ?? (archiving
            ? $"{_options.ResourcePrefix}_Archive_Message"
            : $"{_options.ResourcePrefix}_Restore_Message");

    private string GetArchiveBulkTitleKey(bool anyActive, bool anyInactive) =>
        _options.GetArchiveBulkTitleKey?.Invoke(anyActive, anyInactive)
        ?? (anyInactive && !anyActive
            ? $"{_options.ResourcePrefix}_RestoreBulk_Title"
            : $"{_options.ResourcePrefix}_ArchiveBulk_Title");

    private string GetArchiveBulkMessageKey(bool anyActive, bool anyInactive) =>
        _options.GetArchiveBulkMessageKey?.Invoke(anyActive, anyInactive)
        ?? (anyInactive && !anyActive
            ? $"{_options.ResourcePrefix}_RestoreBulk_Message"
            : $"{_options.ResourcePrefix}_ArchiveBulk_Message");

    private void OnCellContextRequested(object? sender, CrudCellContextEventArgs e)
    {
        if (e.Row is not TRow row)
            return;

        var col = _viewModel.Columns.FirstOrDefault(c => c.Key == e.ColumnKey);
        var canEdit = _options.CanEditRow?.Invoke(row) ?? true;
        var canMutate = _options.CanMutateRow?.Invoke(row) ?? true;

        var flyout = CrudGridContextMenuBuilder.BuildCellMenu(
            row,
            col,
            _viewModel.Columns,
            _viewModel,
            _viewModel.Permissions,
            onEditRow: canEdit ? () => _viewModel.OpenRowCommand.Execute(row) : null,
            onEditCell: col is not null && canEdit && _viewModel.Permissions.CanInlineEdit
                        && _viewModel.CanInlineEditCell(row, e.ColumnKey)
                ? () => _workbench.RequestInlineCellEdit(row, e.ColumnKey)
                : null,
            onArchiveRow: canMutate && _viewModel.Permissions.CanArchive
                ? () => _ = OnArchiveRowFromMenuAsync(row)
                : null,
            onDeleteRow: canMutate && _viewModel.Permissions.CanHardDelete
                ? () => _ = OnDeleteRowFromMenuAsync(row)
                : null,
            archiveRowLabel: canMutate && _viewModel.Permissions.CanArchive
                ? GetArchiveContextMenuLabel(row)
                : null);

        flyout.ShowAt(e.CellElement);
    }

    private async Task OnArchiveRowFromMenuAsync(TRow row)
    {
        if (_options.ConfirmArchiveRow)
        {
            var archiving = row.IsActive;
            var confirmed = await CrudPageConfirmHelper.ConfirmArchiveRowAsync(
                archiving,
                GetArchiveRowTitleKey(row, archiving: true),
                GetArchiveRowMessageKey(row, archiving: true),
                GetArchiveRowTitleKey(row, archiving: false),
                GetArchiveRowMessageKey(row, archiving: false),
                GetRowDisplayName(row));
            if (!confirmed)
                return;
        }

        await _options.ArchiveRowAsync(row);
    }

    private async Task OnDeleteRowFromMenuAsync(TRow row)
    {
        var confirmed = await CrudPageConfirmHelper.ConfirmDeleteRowAsync(
            $"{_options.ResourcePrefix}_Delete_Title",
            $"{_options.ResourcePrefix}_Delete_Message",
            GetRowDisplayName(row));
        if (!confirmed)
            return;

        await _options.DeleteRowAsync(row);
    }

    private async void OnHardDeleteClicked(object? sender, EventArgs e)
    {
        var count = _viewModel.SelectedCount;
        if (count == 0)
            return;

        var confirmed = await CrudPageConfirmHelper.ConfirmBulkDeleteAsync(_options.ResourcePrefix, count);
        if (!confirmed)
            return;

        if (_viewModel.BulkDeleteCommand.CanExecute(null))
            await _viewModel.BulkDeleteCommand.ExecuteAsync(null);
    }

    private async void OnArchiveClicked(object? sender, EventArgs e)
    {
        if (_options.BulkArchiveAsync is not null)
        {
            await _options.BulkArchiveAsync();
            return;
        }

        var selected = _viewModel.GetSelectableSelectedRowsPublic();
        if (selected.Count == 0)
            return;

        var anyActive = selected.Any(r => r.IsActive);
        var anyInactive = selected.Any(r => !r.IsActive);

        var confirmed = await CrudPageConfirmHelper.ConfirmBulkArchiveAsync(
            GetArchiveBulkTitleKey(anyActive, anyInactive),
            GetArchiveBulkMessageKey(anyActive, anyInactive),
            $"{_options.ResourcePrefix}_RestoreBulk_Title",
            $"{_options.ResourcePrefix}_RestoreBulk_Message",
            selected.Count,
            anyActive,
            anyInactive);

        if (!confirmed)
            return;

        if (_viewModel.BulkArchiveCommand.CanExecute(null))
            await _viewModel.BulkArchiveCommand.ExecuteAsync(null);
    }
}
