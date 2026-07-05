using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using System.Windows.Input;

namespace BAAZ.CMMS.App.Controls.CrudWorkbench;

/// <summary>
/// Универсальный контракт ViewModel для CrudWorkbenchPage.
/// Позволяет странице вызывать общие действия без рефлексии.
/// </summary>
public interface ICrudWorkbenchViewModel
{
    bool? IsAllSelected { get; }

    bool IsEditorOpen { get; }

    bool UsesPickMode { get; }

    IReadOnlyList<CrudColumnDefinition> Columns { get; }

    ICommand OpenInsertCommand { get; }

    ICommand OpenRowCommand { get; }

    ICommand CancelEditorCommand { get; }

    ICommand ToggleSelectAllCommand { get; }

    ICommand ApplyFilterCommand { get; }

    IAsyncRelayCommand RefreshCommand { get; }

    IAsyncRelayCommand SaveEditorCommand { get; }

    IAsyncRelayCommand BulkArchiveCommand { get; }

    IAsyncRelayCommand BulkDeleteCommand { get; }

    void RefreshSelectionCountPublic();

    /// <summary>Ключ страницы для сохранения видимости столбцов в settings.json.</summary>
    string ColumnSettingsKey { get; }

    void PersistColumnVisibility();

    void ResetColumnVisibilityToDefault();

    string? SortColumnKey { get; }
    SortDirection SortDirection { get; }

    void SetSort(string columnKey);
    void FilterByCellValue(string columnKey, string? value);

    Task<bool> SaveInlineCellAsync(ICrudRow row, string columnKey, string? newValue, CancellationToken ct);

    /// <summary>Пересчитать FilteredRows после inline-save.</summary>
    void RefreshGrid();

    /// <summary>Сигнал после сохранения данных строки (inline или редактор).</summary>
    event EventHandler? RowDataSaved;

    /// <summary>Можно ли открыть inline-редактор для ячейки.</summary>
    bool CanInlineEditCell(ICrudRow row, string columnKey);

    /// <summary>Подготовить метаданные колонки перед открытием inline-flyout (напр. disabled-узлы дерева).</summary>
    void PrepareInlineCellEdit(ICrudRow row, string columnKey);

    /// <summary>Клиентская валидация значения inline-редактора; null = ok, иначе текст ошибки.</summary>
    string? ValidateInlineCellValue(ICrudRow row, string columnKey, string? value);
}

public enum SortDirection { Ascending, Descending }
