# MEMORY.md

Проектная память BAAZ CMMS — уроки, баги, паттерны. Для агентов и разработчиков.

---

## WinUI: `ExecutionEngineException` — модальный `LocationPicker` в Asset Registry

**Статус:** исправлено, подтверждено пользователем.

### Симптом

`System.ExecutionEngineException` в `Microsoft.WinUI.dll` при открытии модального выбора локации (`ContentDialog.ShowAsync()`) со страницы **Asset Registry** — из редактора или inline flyout. Падение на первом layout pass после `dialog.SizeChanged`. Managed stack trace нет.

### Что работало (изоляция)

| Сценарий | Результат |
|----------|-----------|
| Тот же `LocationPicker` modal на **Users** | OK |
| `ContentDialog` + `TextBlock` | OK |
| `ContentDialog` + голый `TreeView` (TreeViewNode, ~85 узлов) | OK |
| Полный `LocationPicker` UserControl в `ContentDialog` на **Asset Registry** | **Crash** |

### Не root cause

- Рекурсия дерева / stack overflow — `RebuildTree` / `ApplySelectionToTree` завершаются до crash.
- `x:Bind` на `ShowInlineTree` visibility — откатили, crash остался.

### Контекст Asset Registry (contributing factors)

- Редактор открыт → второй экземпляр `LocationPicker` (inline tree свёрнут, режим кнопки диалога).
- При inline edit — открытый Flyout с ещё одним `LocationPicker`.
- Отдельный баг: `ComboBox` категории с `SelectedItem` на class objects → `MeasureOverride` / `E_INVALIDARG`; fix: `SelectedValue` / `EditorCategoryId`.

### Fix (работает)

1. **`LocationPickerDialogHelper.ShowSingleAsync`** — в `ContentDialog` класть **`LocationTreePanel`**, не полный `LocationPicker` (без nested UserControl + chrome x:Bind в modal).
2. **`PopupDismissHelper.CloseAncestorPopups`** перед modal при открытии из flyout.
3. Общая логика выбора — **`LocationTreeSelectionHelper`**.
4. Default hierarchical `DataTemplate` — в **`LocationTreePanel`** (`EnsureDefaultItemTemplate`).

Файлы: `Helpers/LocationHelpers/LocationPickerDialogHelper.cs`, `PopupDismissHelper.cs`, `LocationTreeSelectionHelper.cs`, `Controls/LocationTree/LocationTreePanel.*`.

### Урок для WinUI

Modal picker → **тонкий контент диалога** (`LocationTreePanel` или dedicated dialog surface). **Не** re-host полного picker `UserControl`, который уже на странице.

WinUI может кинуть native `ExecutionEngineException` на layout при: сложные nested controls + несколько экземпляров picker + открытый editor/flyout.

**Диагностика:** progressive `ContentDialog` isolation — `TextBlock` → bare `TreeView` → full control; сравнить страницы с одним vs несколькими picker на экране.
