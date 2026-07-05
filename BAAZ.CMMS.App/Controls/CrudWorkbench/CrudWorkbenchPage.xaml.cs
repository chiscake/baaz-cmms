using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

public sealed partial class CrudWorkbenchPage : UserControl
{
    private INotifyPropertyChanged? _vmNotifier;
    private ICrudWorkbenchViewModel? _workbenchVm;

    public CrudWorkbenchPage()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModelNotifier();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    public static readonly DependencyProperty EditorContentProperty =
        DependencyProperty.Register(nameof(EditorContent), typeof(object),
            typeof(CrudWorkbenchPage), new PropertyMetadata(null));
    public object? EditorContent { get => GetValue(EditorContentProperty); set => SetValue(EditorContentProperty, value); }

    public static readonly DependencyProperty HasShowInactiveProperty =
        DependencyProperty.Register(nameof(HasShowInactive), typeof(bool),
            typeof(CrudWorkbenchPage), new PropertyMetadata(true));
    public bool HasShowInactive { get => (bool)GetValue(HasShowInactiveProperty); set => SetValue(HasShowInactiveProperty, value); }

    public event EventHandler<CrudCellContextEventArgs>? CellContextRequested;
    public event EventHandler<CrudHeaderContextEventArgs>? HeaderContextRequested;
    public event EventHandler<CrudRowEventArgs>? RowDoubleTapped;
    public event EventHandler<CrudCellEditEventArgs>? CellEditCommitted;
    public event EventHandler? HardDeleteClicked;
    public event EventHandler? ArchiveClicked;

    private void AttachViewModelNotifier()
    {
        if (_vmNotifier is not null)
            _vmNotifier.PropertyChanged -= OnViewModelPropertyChanged;

        if (_workbenchVm is not null)
            _workbenchVm.RowDataSaved -= OnRowDataSaved;

        _vmNotifier = DataContext as INotifyPropertyChanged;
        if (_vmNotifier is not null)
            _vmNotifier.PropertyChanged += OnViewModelPropertyChanged;

        _workbenchVm = DataContext as ICrudWorkbenchViewModel;
        if (_workbenchVm is not null)
            _workbenchVm.RowDataSaved += OnRowDataSaved;

        UpdateEditorColumnLayout();
        SyncGridSelectionState();
    }

    private void OnRowDataSaved(object? sender, EventArgs e)
        => DataGrid.RefreshVisibleRowData();

    private void OnPageLoaded(object sender, RoutedEventArgs e)
        => AttachViewModelNotifier();

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        if (_workbenchVm is not null)
            _workbenchVm.RowDataSaved -= OnRowDataSaved;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudWorkbenchViewModel.IsAllSelected))
            SyncGridSelectionState();

        if (e.PropertyName is nameof(ICrudWorkbenchViewModel.IsEditorOpen)
            or nameof(ICrudWorkbenchViewModel.UsesPickMode))
            UpdateEditorColumnLayout();
    }

    private void SyncGridSelectionState()
    {
        if (_workbenchVm is not null)
            DataGrid.IsAllSelected = _workbenchVm.IsAllSelected;
    }

    private void UpdateEditorColumnLayout()
    {
        if (_workbenchVm is null)
            return;

        var isOpen = !_workbenchVm.UsesPickMode && _workbenchVm.IsEditorOpen;

        Debug.WriteLine($"[CrudWorkbenchPage] UpdateEditorColumnLayout isOpen={isOpen}");

        EditorColumnDefinition.Width = isOpen
            ? new GridLength(380, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
        EditorColumnDefinition.MinWidth = isOpen ? 280 : 0;

        EditorSplitter.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        EditorPanel.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;

        Debug.WriteLine(
            $"[CrudWorkbenchPage] UpdateEditorColumnLayout done: " +
            $"colWidth={EditorColumnDefinition.Width.Value}, editorVisible={EditorPanel.Visibility}");
    }

    private void Workbench_AddClicked(object sender, EventArgs e)
    {
        if (_workbenchVm?.OpenInsertCommand.CanExecute(null) == true)
            _workbenchVm.OpenInsertCommand.Execute(null);
    }

    private void Workbench_RefreshClicked(object sender, EventArgs e)
        => _ = _workbenchVm?.RefreshCommand.ExecuteAsync(null);

    private void Workbench_ArchiveClicked(object sender, EventArgs e)
    {
        if (ArchiveClicked is not null)
            ArchiveClicked.Invoke(this, EventArgs.Empty);
        else
            _ = _workbenchVm?.BulkArchiveCommand.ExecuteAsync(null);
    }

    private void Workbench_HardDeleteClicked(object sender, EventArgs e)
        => HardDeleteClicked?.Invoke(this, EventArgs.Empty);

    private void Workbench_ColumnsChanged(object sender, EventArgs e)
    {
        _workbenchVm?.PersistColumnVisibility();
        DataGrid.RebuildAfterColumnsChanged();
    }

    private void Workbench_ColumnsResetRequested(object sender, EventArgs e)
    {
        _workbenchVm?.ResetColumnVisibilityToDefault();
        DataGrid.RebuildAfterColumnsChanged();
    }

    private void Workbench_SearchClicked(object sender, EventArgs e)
    {
        if (_workbenchVm?.ApplyFilterCommand.CanExecute(null) == true)
            _workbenchVm.ApplyFilterCommand.Execute(null);
    }

    private void Workbench_FiltersChanged(object sender, EventArgs e)
    {
        if (_workbenchVm?.ApplyFilterCommand.CanExecute(null) == true)
            _workbenchVm.ApplyFilterCommand.Execute(null);
    }

    /// <summary>Inline-редактирование ячейки (dbl-click или контекстное меню).</summary>
    public void RequestInlineCellEdit(ICrudGridRow row, string columnKey)
    {
        if (_workbenchVm is not null)
        {
            _workbenchVm.PrepareInlineCellEdit(row, columnKey);
            DataGrid.BeginInlineEdit(
                row,
                columnKey,
                v => _workbenchVm.ValidateInlineCellValue(row, columnKey, v));
        }
        else
        {
            DataGrid.BeginInlineEdit(row, columnKey);
        }
    }

    private void DataGrid_SelectAllClicked(object sender, EventArgs e)
    {
        if (_workbenchVm?.ToggleSelectAllCommand.CanExecute(null) == true)
            _workbenchVm.ToggleSelectAllCommand.Execute(null);
        SyncGridSelectionState();
    }

    private void DataGrid_RowSelectionChanged(object sender, EventArgs e)
    {
        _workbenchVm?.RefreshSelectionCountPublic();
        SyncGridSelectionState();
    }

    private void DataGrid_RowExpandRequested(object sender, CrudRowEventArgs e)
        => ExecuteOpenRow(e.Row);

    private void DataGrid_RowDoubleTapped(object sender, CrudRowEventArgs e)
    {
        ExecuteOpenRow(e.Row);
        RowDoubleTapped?.Invoke(this, e);
    }

    private void DataGrid_CellInlineEditRequested(object sender, CrudCellContextEventArgs e)
    {
        if (_workbenchVm is not null && _workbenchVm.CanInlineEditCell(e.Row, e.ColumnKey))
            RequestInlineCellEdit(e.Row, e.ColumnKey);
        else
            ExecuteOpenRow(e.Row);
    }

    private void ExecuteOpenRow(ICrudRow row)
    {
        if (_workbenchVm?.OpenRowCommand.CanExecute(row) == true)
            _workbenchVm.OpenRowCommand.Execute(row);
    }

    private void DataGrid_CellContextRequested(object sender, CrudCellContextEventArgs e)
        => CellContextRequested?.Invoke(this, e);

    private void DataGrid_HeaderContextRequested(object sender, CrudHeaderContextEventArgs e)
    {
        if (_workbenchVm is null) return;

        var col = _workbenchVm.Columns.FirstOrDefault(c => c.Key == e.ColumnKey);
        if (col is null)
        {
            HeaderContextRequested?.Invoke(this, e);
            return;
        }

        var menu = CrudGridContextMenuBuilder.BuildHeaderMenu(col, _workbenchVm, () =>
        {
            _workbenchVm.PersistColumnVisibility();
            DataGrid.RebuildAfterColumnsChanged();
        });
        menu.ShowAt(e.HeaderElement);
        HeaderContextRequested?.Invoke(this, e);
    }

    private void DataGrid_SortRequested(object sender, CrudSortRequestedEventArgs e)
        => _workbenchVm?.SetSort(e.ColumnKey);

    private async void DataGrid_CellEditCommitted(object sender, CrudCellEditEventArgs e)
    {
        CellEditCommitted?.Invoke(this, e);
        if (_workbenchVm is not null)
            await _workbenchVm.SaveInlineCellAsync(e.Row, e.ColumnKey, e.NewValue, default);
    }

    private void EditorPanel_SaveClicked(object sender, EventArgs e)
        => _ = _workbenchVm?.SaveEditorCommand.ExecuteAsync(null);

    private void EditorPanel_CancelClicked(object sender, EventArgs e)
    {
        if (_workbenchVm?.CancelEditorCommand.CanExecute(null) == true)
            _workbenchVm.CancelEditorCommand.Execute(null);
    }
}
