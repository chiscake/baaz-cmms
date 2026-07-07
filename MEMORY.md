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

---

## Release output: уменьшение размера `win-x64` (333 → ~123 MB)

**Статус:** применено в `BAAZ.CMMS.App.csproj`. Debug не трогали.

### Контекст

Папка `bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\` — self-contained unpackaged WinUI 3. Цель: меньше размер без поломки запуска exe.

### Аудит (до оптимизации)

| Категория | ~MB | Комментарий |
|-----------|-----|-------------|
| Весь output | **333** | 925 файлов |
| `.dll` суммарно | 321 | основная масса |
| Self-contained .NET (coreclr, `System.Private.CoreLib`, WinForms, WPF) | ~170 | `runtimeconfig.json`: `includedFrameworks` + `Microsoft.WindowsDesktop.App` |
| WinUI / Windows App SDK (self-contained) | ~63 | `Microsoft.ui.xaml.dll`, `Microsoft.WinUI.dll`, … — **нужны** |
| ML-бинарники WinAppSDK (`onnxruntime.dll`, `DirectML.dll`) | ~39 | приложение **не использует** ML |
| Satellite-языки (13 локалей NuGet) | ~20 | в UI только ru-RU + en-US |
| PDB | ~1.4 | не нужны в демо-сборке |
| OpenXml (`DocumentFormat.OpenXml`) | ~6.5 | нужен — DOCX в MaterialRequisition / ToolRequisition |

Крупнейшие файлы: `Microsoft.Windows.SDK.NET.dll` (~24 MB), `onnxruntime.dll` (~21 MB), `DirectML.dll` (~18 MB), `PresentationFramework.dll` (~15 MB), `System.Windows.Forms.dll` (~13 MB).

### Что сработало

1. **Release: framework-dependent .NET** (`SelfContained=false`) при сохранении **`WindowsAppSDKSelfContained=true`**.  
   - .NET 10 Desktop Runtime — с машины; WinAppSDK runtime — в output.  
   - Эффект: −~170 MB (нет coreclr, WinForms, WPF в output).  
   - **Важно:** `<SelfContained>false</SelfContained>` в `PropertyGroup` для Release **не применился** — MSBuild/SDK оставлял `SelfContained=true`. Сработал target **`SetFrameworkDependentRelease`** с `BeforeTargets="ProcessFrameworkReferences"`.

2. **`SatelliteResourceLanguages=ru-RU;en-US`** — только локали приложения.

3. **`DebugType=none`**, **`DebugSymbols=false`** + post-build удаление `**\*.pdb` (в т.ч. из `BAAZ.CMMS.Core`, `WinUI.UtilsLibrary`).

4. **Post-build delete** неиспользуемых ML DLL WinAppSDK: `onnxruntime.dll`, `DirectML.dll`, `Microsoft.ML.OnnxRuntime.dll`.  
   - Проверено: exe стартует без них (ручной тест — удалить файлы и запустить).

### Результат

| | До | После |
|---|-----|--------|
| Размер | 333 MB | **~123 MB** (−63%) |
| Файлов | 925 | ~453 |
| `coreclr.dll` в output | да | нет |
| `onnxruntime.dll` | да | нет |

Debug-сборка по-прежнему self-contained (удобно для F5 без отдельной установки .NET).

### Trade-off

Release exe **требует установленный .NET 10 Desktop Runtime** на целевой ПК. Без него — быстрый fail при старте. Для демо на машинах разработки (VS / SDK) — ок.

`PublishTrimmed` не включали — WinUI + reflection рискованно для демо-проекта.

### Где смотреть

`BAAZ.CMMS.App/BAAZ.CMMS.App.csproj`: блоки `PropertyGroup Condition=Release`, targets `SetFrameworkDependentRelease`, `RemoveUnusedWinAppSdkMlBinaries`.

### Урок

- Self-contained WinUI output раздувается **двумя** слоями: bundled .NET + bundled WinAppSDK; второй нужен для unpackaged без системного Windows App Runtime, первый для демо часто избыточен.  
- WinAppSDK тащит **onnx/DirectML** (~40 MB) даже без ML-фич — безопасно выкинуть post-build, если не используете.  
- `SelfContained` для WinUI лучше задавать через **MSBuild target до `ProcessFrameworkReferences`**, не только через PropertyGroup.  
- Размер output: `Get-ChildItem … -Recurse -File | Measure-Object Length -Sum`; группировать по префиксам (`onnx`, `PresentationFramework`, `Windows.Forms`, `Microsoft.ui.xaml`).
