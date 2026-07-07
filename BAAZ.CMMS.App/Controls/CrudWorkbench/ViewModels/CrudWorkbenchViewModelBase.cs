using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using BAAZ.CMMS.App.Localization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using WinUI.UtilsLibrary.Controls.PageHeader;
using WinUI.UtilsLibrary.ViewModels;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Базовый VM для всех страниц CrudWorkbench.
/// Содержит общую логику: коллекции, фильтр, выделение, команды toolbar, editor state.
/// </summary>
public abstract partial class CrudWorkbenchViewModelBase<TRow> : PageViewModelBase, ICrudWorkbenchViewModel, ICrudPaginationHost
    where TRow : class, ICrudRow
{
    private bool _suppressPaginationRefresh;

    public const int DefaultPageSize = 100;
    // --- Данные ---

    protected readonly ObservableCollection<TRow> _allRows = [];

    /// <summary>Отфильтрованные строки для отображения в таблице.</summary>
    public ObservableCollection<TRow> FilteredRows { get; } = [];

    /// <summary>Имя для Debug.WriteLine в наследниках.</summary>
    protected virtual string DebugTag => GetType().Name;

    // --- Выделение ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ToolbarDeleteLabel))]
    [NotifyPropertyChangedFor(nameof(ToolbarHardDeleteLabel))]
    [NotifyPropertyChangedFor(nameof(ShowHardDeleteToolbarButton))]
    [NotifyPropertyChangedFor(nameof(ShowArchiveToolbarButton))]
    [NotifyCanExecuteChangedFor(nameof(BulkArchiveCommand))]
    [NotifyCanExecuteChangedFor(nameof(BulkDeleteCommand))]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial bool? IsAllSelected { get; set; }

    public bool HasSelection => SelectedCount > 0;

    /// <summary>Текст кнопки архивации с количеством выбранных строк.</summary>
    public virtual string ToolbarDeleteLabel => string.Empty;

    /// <summary>Текст кнопки безвозвратного удаления с количеством выбранных строк.</summary>
    public virtual string ToolbarHardDeleteLabel => string.Empty;

    /// <summary>Показывать кнопку безвозвратного удаления в тулбаре.</summary>
    public bool ShowHardDeleteToolbarButton =>
        HasSelectableSelection && Permissions?.CanHardDelete == true;

    /// <summary>Показывать кнопку архивации / бана в тулбаре.</summary>
    public virtual bool ShowArchiveToolbarButton =>
        HasSelectableSelection && Permissions?.CanArchive == true;

    /// <summary>Есть выделенные строки, доступные для bulk-действий.</summary>
    public bool HasSelectableSelection => GetSelectableSelectedRows().Count > 0;

    // --- Фильтр ---

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowInactive { get; set; }

    public virtual string ShowInactiveLabel => string.Empty;

    /// <summary>Префикс ключей ресурсов тулбара, напр. <c>RepairDepartments</c> → <c>RepairDepartments_Toolbar_Add</c>.</summary>
    protected virtual string ToolbarResourcePrefix => string.Empty;

    public virtual string ToolbarAdd =>
        string.IsNullOrEmpty(ToolbarResourcePrefix)
            ? string.Empty
            : ResourceStrings.Get($"{ToolbarResourcePrefix}_Toolbar_Add");

    public virtual string ToolbarRefresh =>
        string.IsNullOrEmpty(ToolbarResourcePrefix)
            ? string.Empty
            : ResourceStrings.Get($"{ToolbarResourcePrefix}_Toolbar_Refresh");

    public virtual string ToolbarColumns =>
        string.IsNullOrEmpty(ToolbarResourcePrefix)
            ? string.Empty
            : ResourceStrings.Get($"{ToolbarResourcePrefix}_Toolbar_Columns");

    public virtual string FilterPlaceholder =>
        string.IsNullOrEmpty(ToolbarResourcePrefix)
            ? string.Empty
            : ResourceStrings.Get($"{ToolbarResourcePrefix}_Filter_Placeholder");

    public virtual string EditorSave => ResourceStrings.Get("CrudGrid_Save");

    public virtual string EditorCancel => ResourceStrings.Get("CrudGrid_Cancel");

    public virtual string FilterSearchLabel =>
        ResourceStrings.Get("CrudGrid_Search");

    /// <summary>Активные фильтры по колонкам (бейджи Supabase-style).</summary>
    public ObservableCollection<CrudColumnFilter> ColumnFilters { get; } = [];

    public bool HasColumnFilters => Columns.Any(c => c.IsFilterable);

    // --- Пагинация ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(CanGoToPreviousPage))]
    [NotifyPropertyChangedFor(nameof(CanGoToNextPage))]
    [NotifyPropertyChangedFor(nameof(PaginationOfText))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(CanGoToNextPage))]
    [NotifyPropertyChangedFor(nameof(PaginationOfText))]
    [NotifyPropertyChangedFor(nameof(PaginationRecordsText))]
    public partial int TotalRecords { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(CanGoToNextPage))]
    [NotifyPropertyChangedFor(nameof(PaginationOfText))]
    [NotifyPropertyChangedFor(nameof(PaginationPageSizeText))]
    public partial int PageSize { get; set; } = DefaultPageSize;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));

    public bool CanGoToPreviousPage => CurrentPage > 1;

    public bool CanGoToNextPage => CurrentPage < TotalPages;

    public string PaginationPageLabel => ResourceStrings.Get("CrudGrid_Pagination_Page");

    public string PaginationOfText =>
        string.Format(ResourceStrings.Get("CrudGrid_Pagination_Of"), TotalPages);

    public string PaginationRecordsText =>
        string.Format(ResourceStrings.Get("CrudGrid_Pagination_Records"), TotalRecords);

    public string PaginationPageSizeText =>
        string.Format(ResourceStrings.Get("CrudGrid_Pagination_RowsFormat"), PageSize);

    public ICommand GoToPreviousPageCommand { get; private set; } = null!;
    public ICommand GoToNextPageCommand { get; private set; } = null!;
    public ICommand SetPageSizeCommand { get; private set; } = null!;

    // --- Сортировка ---

    public string? SortColumnKey { get; private set; }
    public SortDirection SortDirection { get; private set; }

    /// <summary>
    /// Устанавливает колонку сортировки. Если columnKey начинается с '~' — Descending.
    /// Пустая строка = сброс сортировки.
    /// </summary>
    public void SetSort(string columnKey)
    {
        if (string.IsNullOrEmpty(columnKey))
        {
            SortColumnKey = null;
            SortDirection = SortDirection.Ascending;
        }
        else if (columnKey.StartsWith("~", StringComparison.Ordinal))
        {
            SortColumnKey = columnKey[1..];
            SortDirection = SortDirection.Descending;
        }
        else
        {
            // Toggle direction if same key
            if (SortColumnKey == columnKey && SortDirection == SortDirection.Ascending)
                SortDirection = SortDirection.Descending;
            else
            {
                SortColumnKey = columnKey;
                SortDirection = SortDirection.Ascending;
            }
        }

        OnPropertyChanged(nameof(SortColumnKey));
        OnPropertyChanged(nameof(SortDirection));
        ResetPaginationPage();
        RefreshFilteredRows();
    }

    /// <summary>Фильтрация по значению конкретной ячейки.</summary>
    public virtual void FilterByCellValue(string columnKey, string? value)
    {
        var col = Columns.FirstOrDefault(c => c.Key == columnKey);
        if (col is null || !col.IsFilterable)
        {
            FilterText = value ?? string.Empty;
            return;
        }

        if (col.FilterKind == CrudColumnFilterKind.Bool
            && CrudBoolCellHelper.TryParseDisplay(value, out var boolValue))
        {
            SetColumnFilter(
                col,
                CrudBoolCellHelper.ToFilterValue(boolValue),
                CrudBoolCellHelper.ToFilterDisplayValue(boolValue));
            return;
        }

        SetColumnFilter(col, value ?? string.Empty, value ?? string.Empty);
    }

    /// <summary>Добавить или обновить фильтр по колонке.</summary>
    protected void SetColumnFilter(CrudColumnDefinition col, string value, string displayValue)
    {
        var existing = ColumnFilters.FirstOrDefault(f => f.ColumnKey == col.Key);
        if (existing is not null)
        {
            existing.Value = value;
            existing.DisplayValue = displayValue;
            existing.ColumnHeader = col.Header;
        }
        else
        {
            ColumnFilters.Add(new CrudColumnFilter
            {
                ColumnKey = col.Key,
                ColumnHeader = col.Header,
                Value = value,
                DisplayValue = displayValue,
            });
        }

        ResetPaginationPage();
        RefreshFilteredRows();
    }

    /// <summary>Сохранить inline-изменение одной ячейки. По умолчанию не поддерживается.</summary>
    public async Task<bool> SaveInlineCellAsync(ICrudRow row, string columnKey, string? newValue, CancellationToken ct)
    {
        var ok = await SaveInlineCellCoreAsync(row, columnKey, newValue, ct);
        if (ok)
            OnInlineCellSaved();
        return ok;
    }

    /// <summary>Реализация inline-save в наследниках. После успеха вызовите <see cref="CommitRowUpdate"/>.</summary>
    protected virtual Task<bool> SaveInlineCellCoreAsync(
        ICrudRow row, string columnKey, string? newValue, CancellationToken ct)
        => Task.FromResult(false);

    /// <summary>Обновить FilteredRows и перерисовать видимые строки грида.</summary>
    protected void NotifyRowDataChanged()
    {
        RefreshFilteredRows();
        OnRowDataSaved();
    }

    /// <summary>Хук после успешного inline-save (обновление FilteredRows).</summary>
    protected virtual void OnInlineCellSaved() => NotifyRowDataChanged();

    event EventHandler? ICrudWorkbenchViewModel.RowDataSaved
    {
        add => RowDataSaved += value;
        remove => RowDataSaved -= value;
    }

    private event EventHandler? RowDataSaved;

    /// <summary>Хук после сохранения данных строки (inline или редактор).</summary>
    protected virtual void OnRowDataSaved() => RowDataSaved?.Invoke(this, EventArgs.Empty);

    /// <summary>Заменить строку в <see cref="_allRows"/>.</summary>
    protected void CommitRowUpdate(Guid id, TRow newRow)
    {
        var idx = FindRowIndex(id);
        if (idx >= 0)
            _allRows[idx] = newRow;
    }

    protected int FindRowIndex(Guid id)
    {
        for (int i = 0; i < _allRows.Count; i++)
        {
            if (_allRows[i].Id == id)
                return i;
        }

        return -1;
    }

    /// <inheritdoc />
    public void RefreshGrid() => RefreshFilteredRowsPublic();

    /// <inheritdoc />
    public virtual bool CanInlineEditCell(ICrudRow row, string columnKey)
    {
        if (Permissions?.CanInlineEdit != true)
            return false;

        var col = Columns.FirstOrDefault(c => c.Key == columnKey);
        return col is { IsInlineEditable: true, IsComputed: false };
    }

    /// <inheritdoc />
    public virtual void PrepareInlineCellEdit(ICrudRow row, string columnKey) { }

    /// <inheritdoc />
    public virtual string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value) => null;

    // --- Состояние загрузки ---

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Загрузка данных таблицы (начальная и Refresh).</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    // --- Редактор ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    public partial bool IsEditorOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(IsEditingExistingRecord))]
    public partial bool IsNewRecord { get; set; }

    public string EditorTitle =>
        IsNewRecord ? GetNewRecordTitle() : GetEditRecordTitle();

    /// <summary>Режим редактирования существующей записи (инверсия <see cref="IsNewRecord"/>).</summary>
    public bool IsEditingExistingRecord => !IsNewRecord;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditorError))]
    public partial string? EditorError { get; set; }

    public bool HasEditorError => !string.IsNullOrEmpty(EditorError);

    // --- Текущая строка в редакторе ---

    [ObservableProperty]
    public partial TRow? EditingRow { get; set; }

    // --- Realtime hooks ---

    /// <summary>
    /// True если пользователь внёс изменения в редакторе, но ещё не нажал Save.
    /// Конкретный VM обязан устанавливать это значение при изменении любого editor-поля.
    /// </summary>
    [ObservableProperty]
    public partial bool EditorIsDirty { get; set; }

    // --- Колонки и права ---

    public ObservableCollection<CrudColumnDefinition> Columns { get; } = [];

    [ObservableProperty]
    public partial CrudPermissions Permissions { get; set; } = new();

    // --- Команды ---

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// В режиме <see cref="CrudRowOpenMode.Editor"/>: «Добавить» вызывает <see cref="OnAddRequested"/>
    /// вместо панели вставки (напр. переход на страницу создания).
    /// </summary>
    protected virtual bool AddNavigatesExternally => false;

    [RelayCommand]
    private void OpenInsert()
    {
        if (RowOpenMode == CrudRowOpenMode.Pick || AddNavigatesExternally)
        {
            OnAddRequested();
            return;
        }

        EditingRow = null;
        EditorError = null;
        IsNewRecord = true;
        IsEditorOpen = true;
        OnNewRecordOpened();
    }

    [RelayCommand]
    private void CancelEditor()
    {
        IsEditorOpen = false;
        EditingRow = null;
        EditorError = null;
    }

    public IAsyncRelayCommand SaveEditorCommand { get; }

    // Защита от двойного/быстрого открытия — предотвращает краш при множественных кликах
    private int _openRowVersion;

    [RelayCommand]
    private void OpenRow(TRow row)
    {
        if (!CanOpenRow(row))
            return;

        if (RowOpenMode == CrudRowOpenMode.Pick)
        {
            OnRecordPicked(row);
            return;
        }

        // Если уже открываем эту же запись — пропустить
        if (ReferenceEquals(EditingRow, row) && IsEditorOpen)
            return;

        var version = Interlocked.Increment(ref _openRowVersion);

        EditingRow = row;
        EditorError = null;
        IsNewRecord = false;
        Debug.WriteLine($"[{DebugTag}] OpenRow: IsEditorOpen=true, rowId={row.Id}");
        IsEditorOpen = true;

        if (version == _openRowVersion)
        {
            Debug.WriteLine($"[{DebugTag}] OpenRow: calling OnRowOpened");
            OnRowOpened(row);
            Debug.WriteLine($"[{DebugTag}] OpenRow: OnRowOpened done");
        }
    }

    public IAsyncRelayCommand BulkArchiveCommand { get; }
    public IAsyncRelayCommand BulkDeleteCommand { get; }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        bool target = IsAllSelected != true;
        foreach (var row in FilteredRows)
        {
            if (!IsRowSelectable(row))
            {
                row.IsSelected = false;
                continue;
            }

            row.IsSelected = target;
        }
        RefreshSelectionCount();
    }

    // --- Конструктор ---

    protected CrudWorkbenchViewModelBase()
    {
        ColumnFilters.CollectionChanged += OnColumnFiltersCollectionChanged;

        LoadCommand = new AsyncRelayCommand(ct => LoadInternalAsync(ct));
        RefreshCommand = new AsyncRelayCommand(ct => LoadInternalAsync(ct));
        SaveEditorCommand = new AsyncRelayCommand(ct => SaveInternalAsync(ct));
        BulkArchiveCommand = new AsyncRelayCommand(
            ct => BulkArchiveInternalAsync(ct),
            () => HasSelectableSelection && Permissions.CanArchive);
        BulkDeleteCommand = new AsyncRelayCommand(
            ct => BulkDeleteInternalAsync(ct),
            () => HasSelectableSelection && Permissions.CanHardDelete);

        GoToPreviousPageCommand = new RelayCommand(
            () => CurrentPage--,
            () => CanGoToPreviousPage);
        GoToNextPageCommand = new RelayCommand(
            () => CurrentPage++,
            () => CanGoToNextPage);
        SetPageSizeCommand = new RelayCommand<int>(size =>
        {
            if (size is not (100 or 500 or 1000)) return;
            PageSize = size;
        });
    }

    partial void OnCurrentPageChanged(int value)
    {
        if (_suppressPaginationRefresh) return;

        var clamped = Math.Clamp(value, 1, TotalPages);
        if (clamped != value)
        {
            _suppressPaginationRefresh = true;
            CurrentPage = clamped;
            _suppressPaginationRefresh = false;
        }

        ClearRowSelection();
        RefreshFilteredRows();
        if (GoToPreviousPageCommand is RelayCommand prev)
            prev.NotifyCanExecuteChanged();
        if (GoToNextPageCommand is RelayCommand next)
            next.NotifyCanExecuteChanged();
    }

    partial void OnPageSizeChanged(int value)
    {
        if (_suppressPaginationRefresh) return;

        ResetPaginationPage();
        ClearRowSelection();
        RefreshFilteredRows();
    }

    private void ResetPaginationPage()
    {
        if (CurrentPage == 1) return;
        _suppressPaginationRefresh = true;
        CurrentPage = 1;
        _suppressPaginationRefresh = false;
    }

    private void ClearRowSelection()
    {
        foreach (var row in _allRows)
            row.IsSelected = false;
    }

    partial void OnPermissionsChanged(CrudPermissions value)
    {
        OnPropertyChanged(nameof(ShowHardDeleteToolbarButton));
        OnPropertyChanged(nameof(ShowArchiveToolbarButton));
        BulkDeleteCommand.NotifyCanExecuteChanged();
        BulkArchiveCommand.NotifyCanExecuteChanged();
    }

    protected static bool IsRowSelectable(ICrudRow row) =>
        row is not ICrudSelectableRow selectable || selectable.IsSelectable;

    protected List<TRow> GetSelectableSelectedRows() =>
        FilteredRows.Where(r => r.IsSelected && IsRowSelectable(r)).ToList();

    /// <summary>Выбранные строки, доступные для bulk-действий (для code-behind страницы).</summary>
    public IReadOnlyList<TRow> GetSelectableSelectedRowsPublic() => GetSelectableSelectedRows();

    // --- Overridable hooks ---

    protected abstract Task LoadDataAsync(CancellationToken ct);
    protected abstract Task<bool> SaveAsync(bool isNew, CancellationToken ct);
    protected abstract Task<bool> ArchiveAsync(IReadOnlyList<TRow> rows, CancellationToken ct);

    /// <summary>Безвозвратное удаление выбранных строк (подтверждение — на странице).</summary>
    protected virtual Task<bool> DeleteAsync(IReadOnlyList<TRow> rows, CancellationToken ct)
        => Task.FromResult(false);
    protected abstract void InitColumns();

    /// <summary>PostgREST-модель для автоматических бейджей PK/Unique в заголовках.</summary>
    protected virtual Type? CrudSchemaModelType => null;

    /// <summary>Ручные бейджи для полей вне модели (например email из auth.users).</summary>
    protected virtual void ApplyManualColumnSemantics(IList<CrudColumnDefinition> columns) { }

    private void FinalizeColumnSemantics()
    {
        if (CrudSchemaModelType is not null)
            CrudColumnSemantics.ApplyFromModel(Columns, CrudSchemaModelType);
        CrudColumnSemantics.ApplyIdConvention(Columns);
        ApplyManualColumnSemantics(Columns);
    }
    protected abstract void InitPermissions();

    /// <summary>Ключ для <see cref="CrudColumnVisibilityStore"/> (например «Personnel»).</summary>
    protected abstract string ColumnSettingsKey { get; }

    string ICrudWorkbenchViewModel.ColumnSettingsKey => ColumnSettingsKey;

    /// <summary>Применить сохранённую или дефолтную видимость после <see cref="InitColumns"/>.</summary>
    protected void ApplyColumnVisibilityFromSettings()
    {
        var saved = CrudColumnVisibilityStore.Load(ColumnSettingsKey);

        foreach (var col in Columns)
        {
            if (col.IsHidden)
            {
                col.IsVisible = false;
                continue;
            }

            if (saved is not null && saved.TryGetValue(col.Key, out bool visible))
                col.IsVisible = visible;
            else
                col.IsVisible = col.IsVisibleByDefault;
        }
    }

    /// <summary>Сохранить текущую видимость переключаемых столбцов.</summary>
    public void PersistColumnVisibility()
    {
        var visibility = Columns
            .Where(c => !c.IsHidden)
            .ToDictionary(c => c.Key, c => c.IsVisible);
        CrudColumnVisibilityStore.Save(ColumnSettingsKey, visibility);
    }

    /// <summary>Сбросить видимость к <see cref="CrudColumnDefinition.IsVisibleByDefault"/> и очистить сохранение.</summary>
    public void ResetColumnVisibilityToDefault()
    {
        CrudColumnVisibilityStore.Remove(ColumnSettingsKey);

        foreach (var col in Columns)
            col.IsVisible = col.IsHidden ? false : col.IsVisibleByDefault;

        OnPropertyChanged(nameof(HasColumnFilters));
    }

    protected abstract string GetNewRecordTitle();
    protected abstract string GetEditRecordTitle();
    protected virtual string GetActiveStatusColumnKey() => "Active";

    private bool _showInactiveForcedByActiveFalseFilter;

    /// <summary>
    /// Фильтр по колонке статуса требует включить «Показывать неактивных»,
    /// иначе строки с противоположным статусом отсекаются до column-filters.
    /// </summary>
    protected virtual void SyncShowInactiveFromColumnFilters()
    {
        var statusFilter = ColumnFilters.FirstOrDefault(f =>
            string.Equals(f.ColumnKey, GetActiveStatusColumnKey(), StringComparison.Ordinal));

        var forceShow = false;
        if (statusFilter is not null)
        {
            var col = Columns.FirstOrDefault(c => c.Key == statusFilter.ColumnKey);
            forceShow = col is not null && ShouldForceShowInactiveForColumnFilter(col, statusFilter);
        }

        if (forceShow)
        {
            if (!ShowInactive)
            {
                ShowInactive = true;
                _showInactiveForcedByActiveFalseFilter = true;
            }
        }
        else if (_showInactiveForcedByActiveFalseFilter)
        {
            ShowInactive = false;
            _showInactiveForcedByActiveFalseFilter = false;
        }
    }

    /// <summary>
    /// Для колонки <see cref="GetActiveStatusColumnKey"/> — нужно ли принудительно показать неактивные строки.
    /// По умолчанию: фильтр «false» (как «Активен = нет»).
    /// </summary>
    protected virtual bool ShouldForceShowInactiveForColumnFilter(
        CrudColumnDefinition col, CrudColumnFilter filter)
        => col.FilterKind == CrudColumnFilterKind.Bool
            && string.Equals(filter.Value, "false", StringComparison.OrdinalIgnoreCase);

    private void OnColumnFiltersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CrudColumnFilter filter in e.NewItems)
                filter.PropertyChanged += OnColumnFilterPropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (CrudColumnFilter filter in e.OldItems)
                filter.PropertyChanged -= OnColumnFilterPropertyChanged;
        }

        ResetPaginationPage();
        RefreshFilteredRows();
    }

    private void OnColumnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CrudColumnFilter.Value) or nameof(CrudColumnFilter.ColumnKey))
        {
            ResetPaginationPage();
            RefreshFilteredRows();
        }
    }

    protected virtual IEnumerable<TRow> ApplyFilter(IEnumerable<TRow> source) => source;

    protected virtual bool MatchesColumnFilters(TRow row)
    {
        if (row is not ICrudGridRow gridRow) return true;

        foreach (var filter in ColumnFilters)
        {
            var col = Columns.FirstOrDefault(c => c.Key == filter.ColumnKey);
            if (col is null) continue;
            if (!MatchesColumnFilter(gridRow, col, filter))
                return false;
        }

        return true;
    }

    protected virtual bool MatchesColumnFilter(ICrudGridRow row, CrudColumnDefinition col, CrudColumnFilter filter)
    {
        var raw = GetFilterRawValue(row, col) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filter.Value))
            return true;

        if (col.FilterKind == CrudColumnFilterKind.Bool)
            return string.Equals(raw, filter.Value, StringComparison.OrdinalIgnoreCase);

        return raw.Contains(filter.Value, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>Значение ячейки для сравнения с фильтром (не display-текст).</summary>
    protected virtual string? GetFilterRawValue(ICrudGridRow row, CrudColumnDefinition col)
    {
        if (col.FilterKind == CrudColumnFilterKind.Bool
            && CrudBoolCellHelper.TryParseDisplay(row.GetCellText(col.Key), out var boolValue))
            return CrudBoolCellHelper.ToFilterValue(boolValue);

        return row.GetCellText(col.Key);
    }
    protected virtual bool CanOpenRow(TRow row) => true;
    protected virtual CrudRowOpenMode RowOpenMode => CrudRowOpenMode.Editor;

    public bool UsesPickMode => RowOpenMode == CrudRowOpenMode.Pick;

    public string ExpandRowToolTip => UsesPickMode
        ? ResourceStrings.Get("CrudGrid_PickRow")
        : ResourceStrings.Get("CrudGrid_ExpandRow");

    protected virtual void OnNewRecordOpened() { }
    protected virtual void OnRowOpened(TRow row) { }
    /// <summary>Pick-режим: выбор строки без редактора (master-detail, picker).</summary>
    protected virtual void OnRecordPicked(TRow row) { }

    /// <summary>Pick-режим: «Добавить» без панели редактора (напр. переход на другую страницу).</summary>
    protected virtual void OnAddRequested() { }

    public async Task OnPageLoadedAsync()
    {
        InitColumns();
        FinalizeColumnSemantics();
        InitPermissions();
        ApplyColumnVisibilityFromSettings();
        OnPropertyChanged(nameof(HasColumnFilters));
        await LoadCommand.ExecuteAsync(null);
    }

    // --- Внутренние методы ---

    private async Task LoadInternalAsync(CancellationToken ct)
    {
        IsBusy = true;
        IsLoading = true;
        InfoBanner.Report(string.Empty);
        try
        {
            await LoadDataAsync(ct);
            Debug.WriteLine($"[{DebugTag}] LoadInternalAsync: _allRows={_allRows.Count}");
            ResetPaginationPage();
            RefreshFilteredRows();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
            IsBusy = false;
        }
    }

    private async Task SaveInternalAsync(CancellationToken ct)
    {
        IsBusy = true;
        EditorError = null;
        try
        {
            bool success = await SaveAsync(IsNewRecord, ct);
            if (success)
            {
                IsEditorOpen = false;
                EditingRow = null;
                NotifyRowDataChanged();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            EditorError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BulkArchiveInternalAsync(CancellationToken ct)
    {
        var selected = GetSelectableSelectedRows();
        if (selected.Count == 0) return;

        IsBusy = true;
        try
        {
            bool success = await ArchiveAsync(selected, ct);
            if (success)
            {
                foreach (var row in selected)
                    row.IsSelected = false;

                if (IsEditorOpen && EditingRow is not null &&
                    selected.Any(r => r.Id == EditingRow.Id))
                {
                    IsEditorOpen = false;
                    EditingRow = null;
                }

                NotifyRowDataChanged();
                RefreshSelectionCount();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BulkDeleteInternalAsync(CancellationToken ct)
    {
        var selected = GetSelectableSelectedRows();
        if (selected.Count == 0) return;

        IsBusy = true;
        try
        {
            bool success = await DeleteAsync(selected, ct);
            if (success)
            {
                foreach (var row in selected)
                    row.IsSelected = false;

                if (IsEditorOpen && EditingRow is not null &&
                    selected.Any(r => r.Id == EditingRow.Id))
                {
                    IsEditorOpen = false;
                    EditingRow = null;
                }

                RefreshFilteredRows();
                RefreshSelectionCount();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            InfoBanner.Report(ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Обновить FilteredRows из _allRows с учётом фильтра, сортировки и пагинации.</summary>
    protected void RefreshFilteredRows()
    {
        SyncShowInactiveFromColumnFilters();

        var allVisible = BuildVisibleRows();
        UpdatePaginationTotals(allVisible.Count);

        var pageRows = allVisible
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        SyncFilteredRowsCollection(pageRows);
        OnPropertyChanged(nameof(FilteredRows));

        Debug.WriteLine(
            $"[{DebugTag}] RefreshFilteredRows: page={CurrentPage}/{TotalPages}, visible={FilteredRows.Count}, " +
            $"total={TotalRecords}, all={_allRows.Count}, filter='{FilterText}', showInactive={ShowInactive}");

        RefreshSelectionCount();
        if (GoToPreviousPageCommand is RelayCommand prev)
            prev.NotifyCanExecuteChanged();
        if (GoToNextPageCommand is RelayCommand next)
            next.NotifyCanExecuteChanged();
    }

    private void UpdatePaginationTotals(int totalRecords)
    {
        _suppressPaginationRefresh = true;
        TotalRecords = totalRecords;

        var totalPages = TotalPages;
        if (CurrentPage > totalPages)
            CurrentPage = totalPages;
        if (CurrentPage < 1)
            CurrentPage = 1;

        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(PaginationOfText));
        OnPropertyChanged(nameof(PaginationRecordsText));
        OnPropertyChanged(nameof(PaginationPageSizeText));
        _suppressPaginationRefresh = false;
    }

    private List<TRow> BuildVisibleRows()
    {
        var source = _allRows.AsEnumerable();

        if (!ShowInactive)
            source = source.Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(FilterText))
            source = ApplyFilter(source);

        if (ColumnFilters.Count > 0)
            source = source.Where(r => MatchesColumnFilters(r));

        if (!string.IsNullOrEmpty(SortColumnKey))
            source = ApplySort(source);

        return source.ToList();
    }

    /// <summary>
    /// Синхронизирует FilteredRows с <paramref name="next"/> с сохранением порядка
    /// (сортировка, фильтры) и подменой экземпляров с тем же Id после archive/restore.
    /// </summary>
    private void SyncFilteredRowsCollection(IReadOnlyList<TRow> next)
    {
        if (IsSameRowSequence(FilteredRows, next))
        {
            var anyDiff = false;
            for (int i = 0; i < next.Count; i++)
            {
                if (!ReferenceEquals(FilteredRows[i], next[i]))
                {
                    anyDiff = true;
                    break;
                }
            }

            if (!anyDiff)
                return;

            for (int i = 0; i < next.Count; i++)
            {
                if (!ReferenceEquals(FilteredRows[i], next[i]))
                    FilteredRows[i] = next[i];
            }

            return;
        }

        FilteredRows.Clear();
        foreach (var row in next)
            FilteredRows.Add(row);
    }

    private static bool IsSameRowSequence(IReadOnlyList<TRow> current, IReadOnlyList<TRow> next)
    {
        if (current.Count != next.Count) return false;
        for (int i = 0; i < current.Count; i++)
        {
            if (current[i].Id != next[i].Id) return false;
        }

        return true;
    }

    protected void RefreshSelectionCount()
    {
        var selectableRows = FilteredRows.Where(IsRowSelectable).ToList();
        SelectedCount = selectableRows.Count(r => r.IsSelected);

        IsAllSelected = selectableRows.Count switch
        {
            0 => false,
            _ when SelectedCount == 0 => false,
            _ when SelectedCount == selectableRows.Count => true,
            _ => null,
        };

        OnPropertyChanged(nameof(HasSelectableSelection));
        OnPropertyChanged(nameof(ShowArchiveToolbarButton));
        OnPropertyChanged(nameof(ShowHardDeleteToolbarButton));
        OnPropertyChanged(nameof(ToolbarDeleteLabel));
        OnPropertyChanged(nameof(ToolbarHardDeleteLabel));
    }

    /// <summary>Вызывается из code-behind при изменении checkbox строки.</summary>
    public void RefreshSelectionCountPublic() => RefreshSelectionCount();

    /// <summary>Пересчитать отфильтрованные строки (например, после скрытия колонки).</summary>
    public void RefreshFilteredRowsPublic() => RefreshFilteredRows();

    [RelayCommand]
    private void ApplyFilter()
    {
        ResetPaginationPage();
        RefreshFilteredRows();
    }

    partial void OnFilterTextChanged(string value)
    {
        ResetPaginationPage();
        RefreshFilteredRows();
    }

    partial void OnShowInactiveChanged(bool value)
    {
        ResetPaginationPage();
        RefreshFilteredRows();
    }

    // --- Realtime update hooks (extensible by subclass) ---

    /// <summary>
    /// Применить удалённое изменение строки к локальной коллекции.
    /// По умолчанию: если редактор открыт на этой строке и dirty → показать предупреждение,
    /// иначе — тихая замена строки в коллекции.
    /// Конкретный VM может переопределить для нестандартной логики.
    /// </summary>
    public virtual async Task ApplyRemoteRowChangeAsync(Guid rowId, bool isDeleted = false)
    {
        if (isDeleted)
        {
            // Убрать строку из коллекции
            var toRemove = _allRows.FirstOrDefault(r => r.Id == rowId);
            if (toRemove is not null)
            {
                _allRows.Remove(toRemove);
                if (IsEditorOpen && EditingRow?.Id == rowId)
                {
                    IsEditorOpen = false;
                    EditingRow = null;
                    InfoBanner.Report(GetDeletedRemotelyMessage(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
                }
                RefreshFilteredRows();
            }
            return;
        }

        if (IsEditorOpen && EditingRow?.Id == rowId && EditorIsDirty)
        {
            // Редактор открыт с изменениями — предупредить, но не перезаписывать
            InfoBanner.Report(GetRemoteConflictMessage(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
        }
        else
        {
            // Тихое обновление: перезагрузить строку из БД
            await RefreshRowAsync(rowId);
        }
    }

    /// <summary>
    /// Сигнализировать о доступности обновлений.
    /// Если пользователь не в редакторе — тихий refresh. Иначе — InfoBar с кнопкой.
    /// </summary>
    public virtual async Task HandleRemoteRefreshAvailableAsync()
    {
        if (!IsEditorOpen || !EditorIsDirty)
        {
            // Тихий refresh (merge)
            await RefreshCommand.ExecuteAsync(null);
        }
        else
        {
            // Есть несохранённые изменения — предложить обновление
            InfoBanner.Report(GetRefreshAvailableMessage(), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
        }
    }

    /// <summary>Перезагрузить одну строку по id. Переопределяется в конкретном VM.</summary>
    protected virtual Task RefreshRowAsync(Guid rowId) => Task.CompletedTask;

    protected virtual string GetRemoteConflictMessage() =>
        BAAZ.CMMS.App.Localization.ResourceStrings.Get("CrudWorkbench_RemoteConflictWarning");

    protected virtual string GetRefreshAvailableMessage() =>
        BAAZ.CMMS.App.Localization.ResourceStrings.Get("CrudWorkbench_RefreshAvailable");

    protected virtual string GetDeletedRemotelyMessage() =>
        BAAZ.CMMS.App.Localization.ResourceStrings.Get("CrudWorkbench_DeletedRemotely");

    /// <summary>
    /// Сортирует строки по <see cref="SortColumnKey"/> и <see cref="SortDirection"/>.
    /// По умолчанию сортирует через <see cref="ICrudGridRow.GetCellText"/>.
    /// </summary>
    protected virtual IEnumerable<TRow> ApplySort(IEnumerable<TRow> source)
    {
        if (string.IsNullOrEmpty(SortColumnKey)) return source;

        string key = SortColumnKey;
        return SortDirection == SortDirection.Ascending
            ? source.OrderBy(r => (r as ICrudGridRow)?.GetCellText(key) ?? string.Empty,
                StringComparer.CurrentCultureIgnoreCase)
            : source.OrderByDescending(r => (r as ICrudGridRow)?.GetCellText(key) ?? string.Empty,
                StringComparer.CurrentCultureIgnoreCase);
    }

    /// <summary>Текст кнопки archive/restore/ban в тулбаре по выделению.</summary>
    protected string BuildToggleArchiveToolbarLabel(
        string idleKey,
        string archiveFormatKey,
        string restoreFormatKey)
    {
        var selected = GetSelectableSelectedRows();
        if (selected.Count == 0)
            return ResourceStrings.Get(idleKey);

        var anyActive = selected.Any(r => r.IsActive);
        var anyInactive = selected.Any(r => !r.IsActive);
        if (anyActive && !anyInactive)
            return string.Format(ResourceStrings.Get(archiveFormatKey), selected.Count);
        if (anyInactive && !anyActive)
            return string.Format(ResourceStrings.Get(restoreFormatKey), selected.Count);

        return string.Format(ResourceStrings.Get(archiveFormatKey), selected.Count);
    }

    /// <summary>Сортировка по <see cref="DateTimeOffset"/>-колонке (CreatedAt, UpdatedAt).</summary>
    protected IEnumerable<TRow> ApplyDateTimeColumnSort(
        IEnumerable<TRow> source,
        string columnKey,
        Func<TRow, DateTimeOffset?> selector)
    {
        if (!string.Equals(SortColumnKey, columnKey, StringComparison.Ordinal))
            return source;

        return SortDirection == SortDirection.Ascending
            ? source.OrderBy(r => selector(r) ?? DateTimeOffset.MinValue)
            : source.OrderByDescending(r => selector(r) ?? DateTimeOffset.MinValue);
    }

    IReadOnlyList<CrudColumnDefinition> ICrudWorkbenchViewModel.Columns => Columns;

    ICommand ICrudWorkbenchViewModel.OpenInsertCommand => OpenInsertCommand;

    ICommand ICrudWorkbenchViewModel.OpenRowCommand => OpenRowCommand;

    ICommand ICrudWorkbenchViewModel.CancelEditorCommand => CancelEditorCommand;

    ICommand ICrudWorkbenchViewModel.ToggleSelectAllCommand => ToggleSelectAllCommand;

    ICommand ICrudWorkbenchViewModel.ApplyFilterCommand => ApplyFilterCommand;
}
