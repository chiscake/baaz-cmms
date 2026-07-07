# AGENTS.md

Guidance for AI coding agents in this repo. **For agents only** (English). Human product context may live elsewhere; keep focus on **how to work in repo**.

Update as project evolves.

## Project overview

**BAAZ CMMS** — desktop **demonstration** app for equipment maintenance + repair request tracking. University internship project for OAO «Baranovichsky auto-aggregate plant» (БААЗ). **No long-term maintenance** expectation; prioritize working functionality + clear structure over production hardening.

| Layer | Project | Responsibility |
|-------|---------|----------------|
| UI + app shell | `BAAZ.CMMS.App` | WinUI 3 pages, ViewModels, DI wiring, DTO/UI mapping, settings UI |
| Domain + data access | `BAAZ.CMMS.Core` | Business logic + **Supabase client** — no WinUI or Windows App SDK references |
| Database | `supabase/` | Local PostgreSQL via Supabase CLI (Docker) |
| Shared UI library | `libs/WinUI.UtilsLibrary` | Git **submodule** — navigation, controls, settings helpers |

**Stack:** .NET 10, WinUI 3 / Windows App SDK 2.2.x, CommunityToolkit.Mvvm, DevWinUI, Supabase NuGet package (formerly `supabase-csharp`).

**On launch:** user signs in; session tokens in **Windows Credential Manager** only (not `settings.json`).

**Roles** (on `profiles`, 1:1 with `auth.users`, RLS-enforced):

| Role | Permissions |
|------|-------------|
| `admin` | Full CRUD |
| `dispatcher` | Dispatch repair work; enter work reports for technicians |
| `requester` | Create requests; view own requests |

**Role labels (ru-RU UI):** DB/API stays `requester`; Russian label **«Заявитель»** — not «Заказчик». Nav group `Nav_Group_Requester` same term. Page `Locations` → **«Локации»** (`Nav_Locations`), not «Подразделения».

**Connectivity:** online-only. Supabase unreachable → dialog + error + **retry** — no offline cache or local DB fallback.

**Notifications:** **no** `notifications` table. **Supabase Realtime** — канал `public-cmms`, таблицы `requests`, `maintenance_schedule`, `work_reports`, `request_repair_departments` (publication `150_realtime_publication.sql`). `RealtimeNotificationService` (Core) — дедуп страниц `(table|type|id|minute)`; reconnect при `IConnectionService.ConnectionStateChanged`. App: `ShellNotificationPresenter` → Windows toasts (`AppNotificationManager.Register` в `Program.cs`, unpackaged) + `INavBadgeService` (`dispatcher.incomingRequests`, `requester.myRequests`); дедуп toast — `HashSet<Guid>` в presenter. Страницы: `EventReceived` + `UnsubscribeRealtime` при уходе. Без polling.

## Repository layout

```
BAAZ.CMMS.slnx
AGENTS.md
BAAZ.CMMS.App/                  # WinUI host app
  App.xaml.cs              # DI, global exception handling
  Program.cs               # Settings + language init before Application.Start
  Navigation/PageMap.cs    # Page key → Type map
  Pages/                   # Role / page_key / files (see § Pages layout)
  Helpers/SettingsHelper/
  Strings/{en-US,ru-RU}/Resources.resw
BAAZ.CMMS.Core/                 # UI-free logic (structure evolving)
  Data/                    # ISupabaseGateway, DataResult; PostgREST models in Data/Models/
  Models/                  # UI/service DTOs (ListItem, EditInput, …)
  Services/
    Auth/                  # IAuthService
    Connection/            # IConnectionService
    Supabase/              # ISupabaseClientProvider, SupabaseRestClient
    Catalog/               # ICatalogService (facade); подпапки Assets, Locations, …
    LocationTree/          # ILocationTreeCache
    Requests/              # IRequestService
    Maintenance/           # IMaintenanceService
    ProfileAdmin/          # IProfileAdminService, AdminUsersFunctionClient
  Repositories/
    Dtos/                  # REST embed DTOs (e.g. RequestRestDtos)
scripts/
supabase/
  migrations/              # Ordered SQL migrations (source of truth) — see § Database workflow
  seed.sql
libs/WinUI.UtilsLibrary/   # Submodule — see libs/WinUI.UtilsLibrary/AGENTS.md
```

**Submodule:** do **not** edit `libs/WinUI.UtilsLibrary` in this repo. Propose patches (separate workspace) or suggest `git submodule update`.

## Setup

**Prerequisites:** .NET 10 SDK, Docker Desktop (local Supabase), Supabase CLI, Git with submodules initialized.

```powershell
git submodule update --init --recursive
dotnet restore BAAZ.CMMS.slnx -p:Platform=x64
supabase start
```

After `supabase db reset`, run **test-user seed script** (JS) from database setup docs — creates demo accounts.

**Edge Functions (UC-A2):** после полного `supabase start` контейнер `supabase_edge_runtime_<project_id>` serves `supabase/functions/` via Kong (`http://127.0.0.1:54321/functions/v1/...`). В CLI 2.x может быть в **Stopped services** (контейнер не создан) — повторный `supabase start` при запущенном стеке контейнер не восстанавливает.

На **Windows** `supabase functions serve` (CLI 2.x) может падать с `ENAMETOOLONG: name too long, uv_spawn`. Используйте:

```powershell
pnpm fn:serve   # scripts/serve-edge-functions.ps1: stop+start при отсутствии контейнера, иначе restart + логи
```

Альтернатива без логов: после правок в `supabase/functions/` достаточно hot reload (`per_worker` в `config.toml`), если контейнер уже запущен. Не делайте `docker restart` только edge_runtime — Kong кэширует IP; при необходимости перезапустите весь стек (`supabase stop` / `supabase start`). Прямой CLI: `pnpm fn:serve:cli` (если заработает после обновления Supabase CLI).

Деплой: `pnpm fn:deploy`; remote логи: `pnpm fn:logs`. **Cloud:** Edge Functions (e.g. `admin-users`) are **not** deployed by `db push` / `db reset --linked` — run `pnpm fn:deploy` separately (see [`docs/CLOUD_DATABASE.md`](docs/CLOUD_DATABASE.md)).

**Supabase URL / keys:** overridable from **Settings** at runtime; persisted in `%LocalAppData%\BAAZ.CMMS.App\settings.json` (not app `.env`). Built-in defaults target local dev. **Cloud project ref:** `nuygawdgrzoiehefysfv` — see [`docs/CLOUD_DATABASE.md`](docs/CLOUD_DATABASE.md).

## Build and verification

Build **x64** only. Do **not** run app (F5 manual in Visual Studio).

```powershell
dotnet build BAAZ.CMMS.slnx -c Debug -p:Platform=x64
```

Single project:

```powershell
dotnet build BAAZ.CMMS.App/BAAZ.CMMS.App.csproj -c Debug -p:Platform=x64
dotnet build BAAZ.CMMS.Core/BAAZ.CMMS.Core.csproj -c Debug
```

- **No CI** / **no automated tests** unless user asks.
- **Verification gate:** successful x64 build only (no `supabase db reset` on every change).
- **No** enforced analyzers or `dotnet format`.
- **Parallel agents / unrelated build errors:** if `dotnet build` fails with errors in files **outside** the agent's current task, do **not** fix them — another chat agent is likely editing concurrently (parallel tasks usually touch different files). Still fix errors clearly caused by the agent's own edits. **Exception:** subagents spawned by an orchestrator — the parent orchestrator owns build verification and may fix or delegate subagent failures.

## Database workflow

Local dev: **Docker + Supabase CLI** only.

| Path | Purpose |
|------|---------|
| `supabase/migrations/` | Ordered thematic migrations — **source of truth** |
| `supabase/seed.sql` | Seed data on `supabase db reset` |

**Migration layout** (`NNN_<domain>_<kind>.sql`, step `010`):

| File | Contents |
|------|----------|
| `000_extensions_and_enums.sql` | `create type …` |
| `010_tables_catalog.sql` | `locations`, `repair_departments`, `profiles`, `technicians`, `assets`, … |
| `020_tables_maintenance.sql` | `maintenance_norms`, `maintenance_schedule`, … |
| `030_tables_requests.sql` | `requests`, `request_repair_departments`, `work_reports`, … |
| `040_tables_integrations.sql` | `tms_tool_requisition_links`, … |
| `050_functions_security_helpers.sql` | RLS helpers, location scope, `request_*` visibility |
| `060_functions_domain_rpc.sql` | `create_request`, `accept_request`, …, `create_schedule_entry` |
| `070_functions_triggers.sql` | trigger function bodies |
| `080_triggers.sql` | `create trigger …` |
| `090_views.sql` | `asset_maintenance_status` |
| `100_grants.sql` | `grant …` |
| `110_rls_catalog.sql` | RLS for catalog tables |
| `120_rls_maintenance.sql` | RLS for maintenance tables |
| `130_rls_requests.sql` | RLS for requests / work reports |
| `140_rls_integrations.sql` | RLS for integration tables |
| `150_realtime_publication.sql` | `alter publication supabase_realtime …` |

**No `schema.sql`:** agents do **not** maintain consolidated schema snapshot. Local dump after apply: `supabase db dump --local --schema public -f schema.sql` (user-driven, optional).

**Early development — edit migrations in place (default):**

Bootstrap phase; local DB disposable, not versioned like production.

- **Do not** add numbered migration for routine schema tweaks unless user asks.
- **Edit thematic file** owning change (e.g. `030_tables_requests.sql` columns, `130_rls_requests.sql` policies).
- **Do not** run `supabase db reset` or `supabase migration up` unless user asks — they apply/reset DB themselves.

**New** migration file only when user requests or change must apply incrementally to deployed DB that cannot reset.

**After migration changes** (when user applies):

1. Apply locally (`supabase db reset` or `supabase migration up`) — **user-driven**, not auto on every edit
2. Add/update PostgREST models in **`BAAZ.CMMS.Core/Data/Models/`** (+ repositories / `SupabaseRestClient` DTOs as needed)
3. Run test-user JS seed if accounts needed

**PostgREST models (runtime data access):** all DB I/O via Supabase client (PostgREST + RLS), not direct Postgres/EF. Table models in **`BAAZ.CMMS.Core/Data/Models/`** — `[Table]`, `[Column("snake_case")]`, inherit `BaseModel`; see `TechnicianModel`. Repositories: `ISupabaseGateway.From<T>()`. Joins, views, RPC: `SupabaseRestClient` + narrow JSON DTOs in **`Repositories/Dtos/`** (e.g. `RequestRestDtos`). UI shapes: `BAAZ.CMMS.Core/Models/` (`*ListItem`, `*EditInput`). After schema change, update matching `*Model` by hand; `auth.users` outside PostgREST models.

Built-in **`auth.users`**; app fields + roles in **`profiles`**.

**Last maintenance date (computed, not stored):** do **not** add `last_maintenance_date` to `assets`. Per type (`to1`, `to2`, `kr`), derive from `work_reports`: planned via `schedule_id` → `maintenance_schedule`, emergency via `request_id` when `work_reports.maintenance_type` set. Expose as SQL **view** so C# reads like table (`client.From<AssetMaintenanceStatusModel>().Get()` or REST) without app aggregation. Add view in migrations + hand-written PostgREST model or REST DTO. `security_invoker = true` on Postgres 15+ if RLS through view.

**No cyclic FK:** `maintenance_schedule` must **not** have `work_report_id`. One-way: `work_reports.schedule_id` only. Completion: trigger `work_reports_check_schedule_completion`.

**Multi-department reports:** each `work_reports` row scoped to one `repair_department_id`. Schedule entry with N departments in `maintenance_schedule_departments` → N `work_reports` rows. Trigger auto-completes schedule when all departments reported. Schedule entries via RPC `public.create_schedule_entry(asset_id, type, date, dept_ids[])` for atomicity.

**Dispatcher visibility (RLS):** dispatcher sees only their `repair_department_id` data — requests (`request_repair_departments`), schedule (`maintenance_schedule_departments`), work reports, technicians. Admin sees all. Exception: `new` (unrouted) queue visible to **all** dispatchers for accept (UC-D1).

**Request assignment:** `assignee_id` on `request_repair_departments`, **not** `requests` — request can route to multiple departments (each own technician). `requests.repair_zone` request-level (one physical location); change does not change `status`, recorded as comment-only row in `request_status_history`. RPCs: `supabase/migrations/060_functions_domain_rpc.sql`.

**`repair_zone` in UI:** `requester` and `dispatcher` do **not** set repair zone on `NewRequest` — default `on_site` at create; dispatcher sets/changes zone on `RequestDetail` (UC-D2). **Exception:** `admin` may set `repair_zone` when creating a request on `NewRequest` (not a gap for requester/dispatcher forms).

**Asset status sync (when implemented):** PostgreSQL trigger on `requests` — `status` → `in_progress` + `asset_id` set → `assets.status = maintenance`; `status` → `closed` → `assets.status = active`. Skip if `asset_id` null or asset `decommissioned`.

```sql
-- Planned: asset_maintenance_status (see docs/DATABASE_TABLES.md)
CREATE VIEW public.asset_maintenance_status
WITH (security_invoker = true) AS
WITH qualifying_reports AS (
    SELECT ms.asset_id, ms.maintenance_type, wr.created_at
    FROM work_reports wr
    INNER JOIN maintenance_schedule ms ON ms.id = wr.schedule_id

    UNION ALL

    SELECT r.asset_id, wr.maintenance_type, wr.created_at
    FROM work_reports wr
    INNER JOIN requests r ON r.id = wr.request_id
    WHERE wr.maintenance_type IS NOT NULL
      AND r.asset_id IS NOT NULL
)
SELECT
    mn.id AS norm_id,
    mn.asset_id,
    mn.maintenance_type,
    mn.interval_days,
    MAX(qr.created_at)::date AS last_maintenance_date,
    CASE
        WHEN MAX(qr.created_at) IS NOT NULL
        THEN (MAX(qr.created_at)::date + mn.interval_days)
        ELSE CURRENT_DATE
    END AS next_maintenance_date
FROM maintenance_norms mn
LEFT JOIN qualifying_reports qr
    ON qr.asset_id = mn.asset_id
   AND qr.maintenance_type = mn.maintenance_type
GROUP BY mn.id, mn.asset_id, mn.maintenance_type, mn.interval_days;
```

## Project boundaries

### BAAZ.CMMS.App

- WinUI pages, ViewModels (CommunityToolkit.Mvvm), navigation shell, localization, settings UI.
- Registers `BAAZ.CMMS.Core` services in DI (`App.xaml.cs`).
- **App-local controls** OK when not reusable enough for submodule.
- Remove `Pages/Demos/` + `PageMap` / DI / menu entries **as soon as unneeded** — don't wait for explicit cleanup if user moves to real features.

### BAAZ.CMMS.Core

- All Supabase access (NuGet **Supabase** client).
- Domain models, services, repositories — **no** `Microsoft.UI.Xaml`, WinUI, or Windows App SDK.
- Layout: `Models/`, `Services/`, `Repositories/` (names may evolve).

### WinUI.UtilsLibrary (submodule)

- Reusable WinUI: `INavigationService`, pickers, `PageHeader`, settings helpers, themes.
- MVVM, navigation, localization, control wiring — follow **`libs/WinUI.UtilsLibrary/AGENTS.md`**.
- Don't duplicate here; reference when touching shell/navigation/shared controls.

## Architecture patterns (app)

From submodule unless noted:

- **MVVM** CommunityToolkit.Mvvm; pages wire ViewModels; avoid `Frame.Navigate` outside `INavigationService`.
- **DI:** `App.xaml.cs` → `ConfigureServices` after `MainWindow` created.
- **Navigation:** register pages in `BAAZ.CMMS.App/Navigation/PageMap.cs`.
- **Localization:** `ru-RU` (default) + `en-US` via `.resw`; `ResourceStrings.Get("Key")` — no hard-coded user strings.
- **Repo language for humans:** comments, commits, user-facing copy in **Russian** (Conventional Commit prefix `feat`/`fix`/… may stay English).

### Color and theming

- **Палитра цветов (default):** `ThemeResource` / `StaticResource` — ключи кистей и темы WinUI/DevWinUI; не литералы `#RRGGBB`, `Color.FromArgb`, инлайн `SolidColorBrush` в XAML/code-behind, если нет веской причины.
- **Status badges:** map status → brush keys via `StatusBadgeFactory` (Fluent `ThemeResource` keys only — `AccentFillColorDefaultBrush`, `SystemFillColorSuccessBrush`, …); control `Controls/StatusBadge/`.
- **New semantic colors:** add Fluent theme brush keys in `StatusBadgeFactory` (or app `Styles.xaml`), not scattered hex across pages.

#### WinUI theming (code-behind)

WinUI platform limits — not domain logic. Reference: `MaintenanceScheduleTimelineControl`, `AppThemeHelper`, `ThemeBrushResolver` (`BAAZ.CMMS.App/Helpers/`).

- **Не `Application.Current.Resources` для theme-aware кистей:** lookup игнорирует `RequestedTheme` элемента и не обновляется при смене темы (microsoft-ui-xaml#7663, #9464).
- **Programmatic text:** XAML-стили с `{ThemeResource}` в `ResourceDictionary`; применять `Style` в code-behind (Microsoft workaround).
- **Programmatic fills/strokes:** `ThemeBrushResolver.Resolve(key, ActualTheme)` — обход merged `ThemeDictionaries` (`Light`/`Default` для light, `Dark` для dark); всегда новый `SolidColorBrush`, не shared brush из `Application.Current.Resources`.
- **Saved theme on launch:** `rootGrid.RequestedTheme = SettingsHelper.Current.SelectedAppTheme` в конструкторе каждого Window сразу после `InitializeComponent`; затем `AppThemeHelper.Apply` когда окно в `WindowHelper.ActiveWindows`. `ThemeHelper.RootTheme` в `OnLaunched` до создания Window — no-op.
- **Never** `Application.RequestedTheme` в `OnLaunched` — `COMException` (WinRT.Runtime).
- **`AppThemeHelper`:** `FrameworkElement.RequestedTheme` на root + caption buttons; `Application.RequestedTheme` не трогать.

### WinUI binding in ItemsControl DataTemplates

- **`ElementName` in `DataTemplate` broken:** `ElementName=PageRoot` (any `ElementName`) in `ItemsControl`/`ListView` `DataTemplate` — page not resolve; `Header`/`PlaceholderText`/`ComboBox.ItemsSource` stay empty (`MaterialRequisition` line items).
- **Fix:** (1) Row `UserControl` + row model `Owner` → page VM (`MaterialRequisitionLineRow.Owner`; bind `Row.Owner.LineNameHeader`, `Row.Owner.UnitOptions`). (2) `ItemsControl` `DataContext` = page VM + `{Binding}`. (3) Code-behind wireup — `IncomingRequestCard`.

## Pages layout

Top-level: `Home/`, `Settings/`, `Admin/`, `Dispatcher/`, `Requester/`.

**Pattern:** `{Role}/{page_key}/` — Page, ViewModel, Row, NavigationArgs. Namespace: `BAAZ.CMMS.App.Pages.{Role}.{page_key}`.

| Role | Examples |
|------|----------|
| `Home` | `Home/AdminHome/`, `Home/DispatcherHome/`, `Home/RequesterHome/` |
| `Settings` | flat — `Settings/SettingsPage.*` (single shell page) |
| `Admin` | `Admin/Users/`, `Admin/Locations/`, `Admin/AssetRegistry/`, `Admin/MaintenanceNorms/` |
| `Dispatcher` | `Dispatcher/PersonnelManagement/`, `Dispatcher/IncomingRequests/`, `Dispatcher/MaintenanceSchedule/` |
| `Requester` | `Requester/NewRequest/`, `Requester/MyRequests/`, `Requester/RequesterAssets/` |

`PageMap` keys unchanged. Admin opens dispatcher/requester pages via nav groups — files stay in owning role folder, no duplication.

## CrudWorkbench pages (catalog CRUD)

Reusable Supabase-style grid + filter + side editor: `BAAZ.CMMS.App/Controls/CrudWorkbench/`. **Reference implementations:**

| Page | UC | Folder | Core service | Data access |
|------|-----|--------|--------------|-------------|
| Personnel | UC-D9 | `Pages/Dispatcher/PersonnelManagement/` | `ITechnicianCatalogService` | Supabase REST / RLS (`technicians`) |
| Users | UC-A2 | `Pages/Admin/Users/` | `IProfileAdminService` | Edge Function `admin-users` (auth admin API) |

| RepairDepartments | UC-A6 | `Pages/Admin/RepairDepartments/` | `IRepairDepartmentCatalogService` | Supabase REST / RLS |
| Locations | UC-A1 | `Pages/Admin/Locations/` | `ILocationCatalogService` | Supabase REST / RLS |
| AssetRegistry | UC-A4 | `Pages/Admin/AssetRegistry/` | `IAssetCatalogService` | Supabase REST / RLS |

**RepairDepartments** = minimal catalog template; Personnel / Users for role-specific patterns.

### Shared catalog infrastructure (post-refactor)

| Component | Path | Purpose |
|-----------|------|---------|
| `CrudCatalogPageWireup` | `Controls/CrudWorkbench/CrudCatalogPageWireup.cs` | Context menu, archive/delete confirm, bulk actions — без дублирования в code-behind |
| `CrudCatalogPageOptions<TRow>` | same file | Callbacks: `ArchiveRowAsync`, `DeleteRowAsync`, custom bulk archive (Users) |
| `CrudPageConfirmHelper` | `Controls/CrudWorkbench/CrudPageConfirmHelper.cs` | Стандартные confirm-диалоги по `{Entity}_*` resw |
| `LocationPickerEditorSync` | `Helpers/LocationHelpers/LocationPickerEditorSync.cs` | `AttachTree` + `SetSelection` для `LocationPicker` в редакторе |
| `LocationScopePickerEditorSync` | `Helpers/LocationHelpers/LocationScopePickerEditorSync.cs` | `AttachTree` + `SetSelection` для `LocationScopePicker`; подавление обратной синхронизации при клике |
| `CrudColumnTemplates` | `Controls/CrudWorkbench/CrudColumnTemplates.cs` | `CreateActiveBoolColumn`, `AppendAuditColumns`, `CreateHiddenUuidColumn` |
| `ICrudWorkbenchViewModel` | `Controls/CrudWorkbench/ICrudWorkbenchViewModel.cs` | Typed contract для `CrudWorkbenchPage` (команды, editor state) — **без reflection** |

**VM toolbar strings:** override `protected override string ToolbarResourcePrefix => "EntityName"` — base подставляет `{Prefix}_Toolbar_Add`, `_Filter_Placeholder`, …; `EditorSave`/`EditorCancel` — `CrudGrid_Save` / `CrudGrid_Cancel`. Archive label — `BuildToggleArchiveToolbarLabel(...)` в base.

### When to use

Use CrudWorkbench for **admin/dispatcher catalog CRUD**: list + filter + optional inline edit + create/edit panel + archive/bulk. Do **not** for workflow pages (request detail, schedule calendar, requisitions) — bespoke pages.

### Layer split

| Layer | Responsibility |
|-------|----------------|
| `BAAZ.CMMS.Core` | Service interface, DTOs, Supabase/Edge Function calls — **no WinUI** |
| `BAAZ.CMMS.App` | `{Entity}Row`, `{Entity}ViewModel`, `{Entity}Page`, `.resw` strings, nav/DI |
| `Controls/CrudWorkbench/` | Shared grid, toolbar, paginator, editor shell — extend only when pattern needs new capability |

### Checklist for a new CRUD page

1. **Registry** — row in **App actions registry** (below) + `docs/use-cases/overview.md` (`action_id`, UC body).
2. **Core** — add/extend service in `BAAZ.CMMS.Core/Services/` (+ models). Register in `App.xaml.cs` if new interface.
   - Simple table + RLS → repository + catalog service (`AssetCatalogService` / `*Repository`).
   - Auth admin / service-role-only → Edge Function (`ProfileAdminService` + `supabase/functions/admin-users/`). Local dev: `pnpm fn:serve`.
3. **Row** — `{Entity}Row.cs` implementing `ICrudGridRow` (+ `ICrudSelectableRow` when some rows not selectable/bulk-mutated):
   - `Id`, `IsActive`, `IsSelected`, `GetCellText(columnKey)` — keys match `CrudColumnDefinition.Key`.
   - Immutable `init` fields + `ObservableObject` only for `IsSelected`.
   - Map domain → row in ViewModel (`MapToRow`); replace rows immutably (`WithActive`, `ReplaceRow`) — no in-place mutate except selection.
4. **ViewModel** — `{Entity}ViewModel : CrudWorkbenchViewModelBase<{Entity}Row>`:
   - Inject Core service(s) + `IAuthService` when role-dependent.
   - Override `ToolbarResourcePrefix` for toolbar/filter resw; column headers — отдельные свойства.
   - **Required overrides:** `ColumnSettingsKey`, `InitColumns`, `InitPermissions`, `LoadDataAsync`, `SaveAsync`, `ArchiveAsync`, `GetNewRecordTitle`, `GetEditRecordTitle`.
   - **Optional:** `DeleteAsync` (hard delete), `ApplyFilter`, `ApplySort` (или `ApplyDateTimeColumnSort`), `GetFilterRawValue`, `SaveInlineCellAsync`, `GetActiveStatusColumnKey` (if status column not `"Active"`), `ShouldForceShowInactiveForColumnFilter`, `CanOpenRow`, `ToolbarDeleteLabel` (если не toggle archive).
   - `InitColumns()` — `CrudColumnTemplates.AppendAuditColumns` + `CreateActiveBoolColumn` где применимо.
   - Do **not** call `InitColumns` yourself — `OnPageLoadedAsync()` in base: `InitColumns` → `InitPermissions` → `ApplyColumnVisibilityFromSettings` → load.
5. **Page XAML** — thin shell (обработчики **не** на `CrudWorkbenchPage` — wireup подключает):

```xml
<layout:PageLayout Header="{x:Bind ViewModel.PageTitle, Mode=OneWay}"
                   State="{x:Bind ViewModel.InfoBanner, Mode=OneWay}"
                   VerticalScrollMode="Disabled">
    <crud:CrudWorkbenchPage x:Name="Workbench"
                              VerticalAlignment="Stretch"
                              DataContext="{x:Bind ViewModel}">
        <crud:CrudWorkbenchPage.EditorContent>
            <!-- StackPanel with editor fields bound to ViewModel -->
        </crud:CrudWorkbenchPage.EditorContent>
    </crud:CrudWorkbenchPage>
</layout:PageLayout>
```

**Always** `VerticalScrollMode="Disabled"` on `PageLayout` for CrudWorkbench. Workbench constrains height to `ScrollView` viewport; vertical page scroll breaks table/paginator.

6. **Page code-behind** — `Page : Page` (WinUI не позволяет наследовать custom `Page` из XAML). Паттерн:

```csharp
private readonly CrudCatalogPageWireup<MyViewModel, MyRow> _crud;

public MyPage()
{
    ViewModel = App.Services.GetRequiredService<MyViewModel>();
    DataContext = ViewModel;
    InitializeComponent();
    _crud = new CrudCatalogPageWireup<MyViewModel, MyRow>(
        ViewModel, Workbench,
        new CrudCatalogPageOptions<MyRow>
        {
            ResourcePrefix = "MyEntity",
            ArchiveRowAsync = row => ViewModel.SetRowArchivedAsync(row, row.IsActive),
            DeleteRowAsync = row => ViewModel.DeleteRowAsync(row),
        });
    _crud.Wire();
}

protected override async void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    await _crud.LoadAsync();
}
```

   - `BulkArchiveConfirmMode.Never` — Personnel (bulk archive без confirm).
   - `ConfirmArchiveRow = false` — Personnel context menu, Users ban.
   - `BulkArchiveAsync` — custom bulk confirm (Users ban/unban).
   - `LocationPicker` в редакторе — `LocationPickerEditorSync` (Users, Locations, Assets).
   - Password / scope picker — page-specific code-behind (Users).
7. **Navigation** — `PageMap.cs` entry, `NavLeafCatalog` leaf (if menu item), role filter in nav builder.
8. **DI** — `services.AddTransient<{Entity}ViewModel>()` in `App.xaml.cs`.
9. **i18n** — keys in `Strings/ru-RU/Resources.resw` + `Strings/en-US/Resources.resw` (`{Entity}_Column_*`, `{Entity}_Toolbar_*`, `{Entity}_Editor_*`, delete confirm). Reuse `CrudGrid_*` / `CrudWorkbench_*` (в т.ч. `CrudGrid_Column_Id` для uuid — не дублировать).
10. **Build** — `dotnet build … -p:Platform=x64`.

### Column definitions (`InitColumns`)

Each `CrudColumnDefinition`:

| Property | Typical use |
|----------|-------------|
| `Key` | Stable id — must match `GetCellText` switch in Row |
| `Header` | Localized title |
| `DataTypeLabel` | Supabase-style hint (`text`, `bool`, `timestamptz`, `uuid`, `enum`, `fk`) |
| `IsPrimaryKey` / `IsUnique` | Бейджи в заголовке грида и flyout «Столбцы»; автозаполнение (см. ниже) |
| `DesiredWidth` | Initial width px; `double.NaN` → 150 default |
| `IsVisibleByDefault` | `false` for Id/audit columns hidden by default |
| `IsHidden` | Column in data but never shown (RLS-only fields) |
| `IsSortable` / `IsFilterable` | Header menu + filter bar |
| `FilterKind` | `Text`, `Bool`, … — bool: `CrudBoolCellHelper` + `FilterKind.Bool` в базовом VM |
| `IsInlineEditable` + `EditKind` | Context menu «edit cell»; implement `SaveInlineCellAsync` |
| `EnumOptions` | For `EditKind.EnumList` (FK pickers in inline/flyout editor) |

**Status column:** default inactive filter uses `GetActiveStatusColumnKey()` (`"Active"`). Inverted (Users: `Banned`, active = `!IsBanned`) → override `GetActiveStatusColumnKey`, map `IsActive` on row, override `ShouldForceShowInactiveForColumnFilter`.

**Role-dependent columns:** add/remove in `InitColumns()` by role (Personnel: `Department` admin only). `OnPropertyChanged(nameof(Columns))` if columns change after first load.

**UUID column (`Id`):** always **last** in `InitColumns()`, `IsVisibleByDefault = false`. Via `CrudColumnTemplates.CreateHiddenUuidColumn()` (`CrudGrid_Column_Id`: ru «Идентификатор», en «ID»). No separate `{Entity}_Column_Id` in `.resw`.

**PK / Unique badges:** after `InitColumns()` base VM calls `CrudColumnSemantics` — reflection on `CrudSchemaModelType` (`[PrimaryKey]` / `[Unique]` on PostgREST model in `BAAZ.CMMS.Core/Data/Models/`), convention `Key == "Id"`, optional `ApplyManualColumnSemantics` (Users: `Email`). UNIQUE in Core: `[Unique]` (`BAAZ.CMMS.Core/Data/Attributes/UniqueAttribute.cs`). Render: `CrudColumnHeaderBuilder` (grid header + picker «Столбцы»; filter bar + copy column name — только `Header`).

### What the base class already provides

Don't reimplement in page VM:

- Toolbar: add, refresh, bulk archive, bulk delete, column visibility picker
- Filter text, column filter badges, «show inactive», client-side filter + sort + **pagination** (default 100; slice in `FilteredRows`)
- Editor open/save/cancel, selection / select-all, `InfoBanner` errors, column width/visibility persistence (`CrudColumnVisibilityStore`, `CrudColumnWidthStore`)
- `RefreshFilteredRows()` after any `_allRows` mutation

### Personnel vs Users — pattern differences

| Concern | Personnel (`UC-D9`) | Users (`UC-A2`) |
|---------|---------------------|-----------------|
| Archive semantics | Deactivate technician (`SetTechnicianActiveAsync`) | Ban/unban via Edge Function |
| `ShowInactive` label | «Показывать неактивных» | «Показывать заблокированных» |
| Status column key | `Active` | `Banned` (`IsActive => !IsBanned`) |
| Selectable rows | All | `ICrudSelectableRow`: skip admin account + current user |
| Inline edit | FullName, Specialty, Department | None (editor panel only) |
| Hard delete | Admin only, `DeleteTechnicianAsync` | Admin only, Edge Function delete |
| Extra editor UI | Department ComboBox / dispatcher read-only | Password generate, role/location lookups |
| Bulk archive confirm | Wireup default (`CrudBulkArchiveConfirmMode.Never`) | `BulkArchiveAsync` в options — ban/unban dialog |

### CrudWorkbench internals (do not break)

- **`CrudDataGrid`** — header/body column widths in sync. After layout, `SyncHeaderWithBody()` (scrollbar padding + copy header `ActualWidth` to rows). Live resize: `ResolveHeaderColumnWidth` — body gets **header actual width**, not raw drag (header enforces content minimum).
- **`CrudWorkbenchPage`** — binds height to host `ScrollView` (minus padding). Required for paginator.
- **Editing `Controls/CrudWorkbench/`** — only cross-page grid/workbench behavior; page logic stays in VM/page.

### Common pitfalls

- Forgetting `VerticalScrollMode="Disabled"` → paginator clipped, table overflows status bar.
- Column `Key` / `GetCellText` mismatch → empty cells.
- In-place row DTO mutate after load → selection/filter bugs; replace in `_allRows` by index/Id.
- Edge Function calls (`AdminUsersFunctionClient`): pass **session JWT** as `Authorization`; do **not** set `Headers["apikey"]` in `InvokeFunctionOptions` — Supabase C# SDK 1.1.1 merges publishable key from `Client`; explicit `apikey` → `401 Invalid API key` on cloud (local Kong may tolerate duplicates).
- Bool column without `FilterKind.Bool` or display not via `CrudBoolCellHelper.Format` → фильтр / бейдж ломается.
- Hard delete without `DeleteAsync` override → bulk delete noop (`DeleteAsync` default returns false).

### `CrudRowOpenMode.Pick` (overflow / picker)

Сценарии «выбрать запись без редактора» (master-detail, read-only grid): `RowOpenMode => CrudRowOpenMode.Pick`, handle `OnRecordPicked(TRow)` / `OnAddRequested()`. Редактор справа не открывается (`UsesPickMode`).

**Семантика кнопок (отличается от catalog `Editor`):**

| UI | `Editor` (catalog) | `Pick` (напр. MyRequests table) |
|----|-------------------|--------------------------------|
| Toolbar «Добавить» | Боковая панель (`IsEditorOpen`) | `OnAddRequested()` — навигация / другое действие |
| Expand / dbl-click | Tooltip `CrudGrid_ExpandRow` → редактор | Tooltip `CrudGrid_PickRow` → `OnRecordPicked` |
| Боковая панель | Видна при редактировании | Скрыта |

Референс: `MyRequestsTableViewModel` + `CrudWorkbenchPage` в `MyRequestsPage`.

**Не мигрировать на CrudWorkbench без необходимости:**
- `RequesterAssets` — остаётся `ListView` (лёгкий список без column filters).
- `MaintenanceNorms` — **done (UC-A5):** bespoke master-detail (не CrudWorkbench): 3 вкладки «По оборудованию» / «Категории» / «Все нормативы»; Hierarchical Override (`equipment_categories`, `category_maintenance_norms*`, `maintenance_norms`, `sync_schedule_after_norm_change`, pending-schedule badge). Deep-link `MaintenanceNormsNavigationArgs(AssetId?, CategoryId?)` — приём на странице; исходящая навигация с `AssetRegistry` и др. — опционально, не блокирует `done`.

## Default agent behavior

1. **Do what user asked** — stay scoped; no drive-by refactors.
2. **Proceed without asking** for: NuGet add/update, DB schema/migration changes, removing demo pages when appropriate.
3. **No commit or push** unless user explicitly requests.
4. **Consult docs before editing** external APIs — see below.
5. Prefer **file references** over large inline code dumps in chat.

## Git workflow

- Primary branch: **`main`** (no multi-branch workflow).
- **Commit** only when user asks; **never push** unless requested.
- Respect `.gitignore` (`bin/`, `obj/`, `.env`, etc.).
- **Commit message format** (Russian body; English prefix optional):

```
feat: Краткое описание

- сделано A
- добавлено B
```

**Submodule updates:** suggest `git submodule update --init --recursive` when library has relevant fixes.

## Documentation before code changes

**Required** before changes involving **WinUI 3**, **Windows App SDK**, **Community Toolkit**, **DevWinUI**, **Supabase**, or other external APIs:

1. In-repo: this file, `libs/WinUI.UtilsLibrary/AGENTS.md`, existing code.
2. **Context7 MCP** when step 1 insufficient (`resolve-library-id` → `get-library-docs` / `query-docs`).
3. Web search only if Context7 has no match.

Match versions: `net10.0`, Windows App SDK **2.2.x**, DevWinUI **10.x**.

Exempt: rename-only refactors, comments, formatting with no API/behavior change.

After non-trivial API usage, verify with **x64 build**.

## Quick reference

| Task | Start here |
|------|------------|
| Register a page | `BAAZ.CMMS.App/Navigation/PageMap.cs`, `BAAZ.CMMS.App/App.xaml.cs` |
| Shell / navigation UI | `BAAZ.CMMS.App/MainWindow.xaml`, `MainWindow.xaml.cs` |
| App startup | `BAAZ.CMMS.App/Program.cs` |
| Global DI / exceptions | `BAAZ.CMMS.App/App.xaml.cs` |
| App settings model | `BAAZ.CMMS.App/Helpers/SettingsHelper/` |
| Localized strings | `BAAZ.CMMS.App/Strings/{en-US,ru-RU}/Resources.resw` |
| Add domain logic / DB | `BAAZ.CMMS.Core/` |
| **New CrudWorkbench catalog page** | **§ CrudWorkbench pages** — template `Pages/Admin/RepairDepartments/` (minimal), `PersonnelManagement` / `Users` for advanced |
| PostgREST table model | `BAAZ.CMMS.Core/Data/Models/*Model.cs` — pattern `TechnicianModel` |
| Schema change (default) | Edit existing file in `supabase/migrations/` → update `Data/Models` after user applies DB |
| New migration file | Only when user asks or DB cannot reset — then `NNN_description.sql` → update models |
| Shared WinUI control | Submodule — propose patch, do not edit in-tree |
| Submodule conventions | `libs/WinUI.UtilsLibrary/AGENTS.md` |

## Out of scope (unless user says otherwise)

- Long-term production support, CI pipelines, automated test suites
- Offline mode / local SQLite fallback
- Direct edits inside `libs/WinUI.UtilsLibrary`
- ERP integrations (1С, Galaktika, Intermech)
- Mobile clients, IoT / OPC-UA

---

## App actions registry

**Source of truth** for all navigable actions. Adding/changing action/page/UC → update this table first, then `docs/use-cases/overview.md` + `BAAZ.CMMS.App`.

**Sync rule:**
- `overview.md` ↔ `AGENTS.md`: `uc` column matches UC-* heading; `action_id` appears in the UC body as `**Action ID:** \`act.*\``.
- `AGENTS.md` ↔ App: `page_key` matches `PageMap.cs`; `nav_item_id` matches `NavItemIds.*`; `core_service` matches the injected interface.
- Status values: `stub` (TextBlock only), `partial` (some logic, known gaps shown in red), `done`.

| action_id | uc | roles | page_key | nav_item_id | core_service | status |
|---|---|---|---|---|---|---|
| `act.auth.signin` | UC-G1 | all | — (LoginWindow) | — | `IAuthService` | done |
| `act.auth.signout` | UC-G1 | all | — (TitleBar button) | — | `IAuthService` | done |
| `act.settings.view` | UC-G2 | all | `Settings` | Settings | `ISupabaseClientProvider` | done |
| `act.connection.check` | UC-G3 | all | — (status bar) | — | `IConnectionService` | done |
| `act.home.view` | UC-G4 | all | `HomeAdmin` / `HomeDispatcher` / `HomeRequester` | `admin.home` / `dispatcher.home` / `requester.home` | (App section VMs + Core services) | done |
| `act.requests.create` | UC-R1 | requester, dispatcher, admin | `NewRequest` | `requester.newRequest` | `IRequestService` | done |
| `act.requests.my_list` | UC-R2 | requester, dispatcher, admin | `MyRequests` | `requester.myRequests` | `IRequestService` | done |
| `act.requester.assets` | UC-R1 | requester, dispatcher, admin | `RequesterAssets` | `requester.assets` | `IRequesterAssetCatalog` | done |
| `act.requests.cancel` | UC-R3 | requester, dispatcher, admin | `MyRequests` | `requester.myRequests` | `IRequestService` | done |
| `act.requests.accept` | UC-R4 | requester, dispatcher, admin | `MyRequests` | `requester.myRequests` | `IRequestService` | done |
| `act.requests.incoming` | UC-D1 | dispatcher, admin | `IncomingRequests` | `dispatcher.incomingRequests` (admin: via группа «Диспетчер») | `IRequestService` | done |
| `act.requests.assign` | UC-D2 | dispatcher, admin | `RequestDetail` | — (deep-link) | `IRequestService` | done |
| `act.maintenance.schedule` | UC-D3, UC-D5 | dispatcher, admin | `MaintenanceSchedule` | `dispatcher.maintenanceSchedule` (admin: via группа «Диспетчер») | `IMaintenanceService` | done |
| `act.maintenance.work_reports` | UC-D4 | dispatcher, admin | `WorkReports` | `dispatcher.workReports` (admin: via группа «Диспетчер») | `IMaintenanceService` | done |
| `act.requests.status_log` | UC-D6 | dispatcher, admin | `RequestDetail` | — (deep-link) | `IRequestService` | done |
| `act.materials.requisition` | UC-D7 | dispatcher, admin | `MaterialRequisition` | `dispatcher.materialRequisition` (admin: via группа «Диспетчер») | `IMaterialRequisitionService` / `IWarehouseIntegration` (DOCX) | done |
| `act.tools.requisition` | UC-D8 | dispatcher, admin | `ToolRequisition` | `dispatcher.toolRequisition` (admin: via группа «Диспетчер») | `IToolRequisitionService` | done |
| `act.admin.locations` | UC-A1 | admin | `Locations` | `admin.locations` | `ILocationCatalogService` | done |
| `act.admin.users` | UC-A2 | admin | `Users` | `admin.users` | `IProfileAdminService` | done |
| `act.dispatcher.personnel` | UC-D9 | dispatcher, admin | `PersonnelManagement` | `dispatcher.personnel` | `ITechnicianCatalogService` | done |
| `act.admin.assets` | UC-A4 | admin | `AssetRegistry` | `admin.equipment` | `IAssetCatalogService` | done |
| `act.admin.maintenance_norms` | UC-A5 | admin | `MaintenanceNorms` | `admin.maintenanceNorms` | `IMaintenanceService` | done |
| `act.admin.repair_departments` | UC-A6 | admin | `RepairDepartments` | `admin.repairDepartments` | `IRepairDepartmentCatalogService` | done |
| `act.admin.audit_log` | UC-A7 | admin | `AuditLog` | `admin.auditLog` | `IAuditLogService` | done |

**Navigation decisions (rationale):**
- `RepairOrders` leaf removed — no UC in overview; will be re-added when a UC is defined.
- `PpmSchedule` (admin) merged with `MaintenanceSchedule` (dispatcher) → single page `MaintenanceSchedule`.
- `Reports` (admin) + `MyReports` (dispatcher) merged → `WorkReports`.
- `Locations` is a new admin leaf (UC-A1 was missing from nav).
- `MaintenanceNorms` is a new admin leaf (UC-A5 was missing from nav).
- Admin accesses `IncomingRequests`, `MaintenanceSchedule`, and `WorkReports` only via the «Диспетчер» group (`dispatcher.*` nav items), not top-level admin leaves.

**Domain service interfaces** (see `BAAZ.CMMS.Core/Services/`):

| Interface | Aggregate root tables | Covers |
|---|---|---|
| `IRequestRepository` | `requests`, `request_status_history` | REST I/O, embed queries |
| `IRequestService` | (оркестрация над репозиторием) | UC-R1…R4, UC-D1, UC-D2, UC-D6 |
| `ICatalogService` | фасад: assets, locations, technicians, repair_departments | UC-A1, UC-A4, UC-A6, UC-D9 |
| `IAssetCatalogService` | `assets` | UC-A4 |
| `ILocationCatalogService` | `locations` | UC-A1 |
| `ITechnicianCatalogService` | `technicians` | UC-D9 |
| `IRepairDepartmentCatalogService` | `repair_departments` | UC-A6 |
| `IRequesterAssetCatalog` | (read: assets + scope) | NewRequest / RequesterAssets asset picker |
| `IMaintenanceService` | `maintenance_schedule`, `maintenance_schedule_departments`, `maintenance_norms`, `maintenance_norms_departments`, `asset_maintenance_status`, `work_reports` | UC-D3…D5, UC-A5 |
| `IProfileAdminService` | `profiles`, `profile_location_scopes`, `auth.users` (via Edge Function `admin-users`) | UC-A2 — list/create/ban/delete; scopes заявителя через `IProfileLocationScopeRepository` |

---

## Adjacent repo: BAAZ Tool Tracker (TMS)

Separate FastAPI + Supabase repo; CMMS ↔ TMS integration via HTTP + Edge Functions ([`docs/TOOL_TRACKER_INTEGRATION.md`](docs/TOOL_TRACKER_INTEGRATION.md)). TMS has its own agent guide: `baaz-tool-tracker/AGENTS.md`. **Local Supabase ports:** CMMS **54321**, TMS **55321** (both stacks on one machine).

**When changing TMS integration contract** (`170_integration_tool_tracker.sql`, `scripts/seed-tt-integration-data.mjs`, fixtures UUIDs):

1. Update [`docs/TOOL_TRACKER_INTEGRATION.md`](docs/TOOL_TRACKER_INTEGRATION.md).
2. **If `baaz-tool-tracker` workspace is available:** also update `baaz-tool-tracker/docs/CMMS_INTEGRATION.md` and symmetric fixtures in both repos.

---

## Adjacent repo: Prostoi (DowntimeTracker)

Separate WinForms repo; CMMS exposes read-only `integration.*` views ([`docs/DOWNTIME_TRACKER_INTEGRATION.md`](docs/DOWNTIME_TRACKER_INTEGRATION.md)).

**When changing DT integration contract** (`160_integration_downtime_tracker.sql`, `scripts/seed-dt-integration-data.mjs`, integration-related seed UUIDs):

1. Update [`docs/DOWNTIME_TRACKER_INTEGRATION.md`](docs/DOWNTIME_TRACKER_INTEGRATION.md).
2. **If the Prostoi workspace is available** in the same Cursor workspace (`Prostoi/` root): also update `Prostoi/docs/CMMS_INTEGRATION.md` (DT-facing sections) and `Prostoi/Integration/Fixtures/` to stay symmetric with seed UUIDs and view columns.
3. Do **not** recreate `downtime-tracker-integration-proposal.md` (removed; replaced by the two docs above).

---

## Adjacent system contracts

Integration UC from `docs/use-cases/tool-tracker.md`, `docs/use-cases/tms-integration-proposal.md`, and `docs/use-cases/downtime-tracker.md`. No UI in `BAAZ.CMMS.App` for adjacent systems — dispatcher UI for UC-D7/D8 lives in CMMS; warehouse/TMS adapters in `BAAZ.CMMS.Core/Contracts/Integrations/`.

| uc | system | operation | direction | interface | status |
|---|---|---|---|---|---|
| UC-TT1 | ToolTracker | `GetActiveTaskForTechnician` | TT → CMMS (read) | `IToolTrackerIntegration` | partial |
| UC-TT2 | ToolTracker | `NotifyWorkReportCreated` | CMMS → TT (event) | `IToolTrackerIntegration` | stub |
| UC-TT3 | ToolTracker | `CreateMaintenanceRequest` | TT → CMMS (write via DB) | — (direct DB insert) | — |
| UC-TT4 | ToolTracker | `NotifyRequestStatusChanged` | CMMS → TT (event) | `IToolTrackerIntegration` | partial |
| UC-TT5 | ToolTracker | `CreateToolRequisition` | CMMS → TT (write) | `IToolTrackerIntegration` | partial |
| UC-WH1 | Warehouse | `CreateMaterialRequisition` | CMMS → WH (write) | `IWarehouseIntegration` | stub |
| UC-DT1 | DowntimeTracker | `NotifyRequestStatusChanged` | CMMS → DT (event) | `IDowntimeTrackerIntegration` | stub |
| UC-DT2 | DowntimeTracker | `GetScheduledMaintenance` | DT → CMMS (read) | `IDowntimeTrackerIntegration` | stub |
| UC-DT3 | DowntimeTracker | `NotifyWorkReportCreated` | CMMS → DT (event) | `IDowntimeTrackerIntegration` | stub |
| UC-DT4 | DowntimeTracker | `GetWorkReportsForPeriod` | DT → CMMS (read) | `IDowntimeTrackerIntegration` | stub |

Interface files: `BAAZ.CMMS.Core/Contracts/Integrations/IToolTrackerIntegration.cs`, `IWarehouseIntegration.cs`, `IDowntimeTrackerIntegration.cs`.
Stub implementations registered in `BAAZ.CMMS.App/App.xaml.cs` DI (no-op, no exceptions).

---

## Learned Workspace Facts

- Seed UUID только hex (0-9, a-f); demo `maintenance_schedule` — префикс `51000000-...`, не `s...`
- Просрочка графика ППР: `mark_overdue_schedule_items` при загрузке списка и в начале `generate_ppr_schedule`, не только nightly pg_cron
- Статус-бейджи: `StatusBadgeFactory` → Fluent ThemeResource-ключи; контрол `Controls/StatusBadge/` — не submodule
- `requests.contractor_name` — без CHECK в DDL; инвариант «только при external» — RPC `update_request_repair_zone`. `target_repair_department_id` ≠ маршрутизация (`request_repair_departments`); после accept — смена отдела только через RPC на `RequestDetail`
- Интеграция Prostoi/DT: `Equipment.InventoryNumber` (MySQL) ↔ `assets.asset_number` (PostgreSQL)
- Demo auth: не INSERT в `auth.users` в seed.sql; аккаунты через `scripts/seed-test-users.mjs`
- Cloud Supabase + Supabase C# SDK 1.1.1: do **not** set `Headers["apikey"]` in `InvokeFunctionOptions` for `client.Functions.Invoke()` — SDK merges publishable key from `Client`; duplicate `apikey` → `401` on cloud (`AdminUsersFunctionClient`)
- WinUI `DataTemplate`: `ElementName` → page не работает (пустые Header/ComboBox); фикс — `UserControl` + `Row.Owner` на VM или code-behind (`MaterialRequisitionLineItemControl`, `IncomingRequestCard`)
- UC-D7/D8: расходники и инструмент — `accepted`/`in_progress` (заявки) или `scheduled`/`overdue`/`in_progress` (ППР); политика `WorkOrderRequisitionPolicy`; TMS: `accepted`/`overdue` → `scheduled`
- Workflow заявки UC-D2: assign/add dept — `accepted` или `in_progress` (assign только отделам без `work_reports`); zone/transfer — только `accepted`; `start_request_work` — `accepted` + все `rrd.assignee_id` заполнены (`ALL_DEPARTMENTS_NEED_ASSIGNEE`); после `work_reports` отдел locked (UI + `DEPARTMENT_ALREADY_REPORTED`); auto-`completed` — триггер NOT EXISTS по `rrd` без отчёта; admin `transfer_request_department` заменяет все `rrd`
- WinUI code-behind theme: `AppThemeHelper` + `ThemeBrushResolver` в `BAAZ.CMMS.App/Helpers/`; см. § WinUI theming (code-behind)
- TMS adjacent repo: own `AGENTS.md`; local Supabase port **55321** (TMS) vs **54321** (CMMS)
