# Доступ приложения к БД

Схема взаимодействия **BAAZ.CMMS.App** с Supabase/PostgreSQL. Элементы диаграммы — типы и слои .NET.

## Диаграмма

```mermaid
flowchart TB
  subgraph APP["BAAZ.CMMS.App"]
    Login["LoginWindow"]
    VM["*ViewModel\n(NewRequest, Users, AssetRegistry, …)"]
    Cred["WindowsCredentialSessionPersistence"]
    VM --> Login
  end

  subgraph CORE_SVC["BAAZ.CMMS.Core — Services"]
    Auth["AuthService"]
    Conn["ConnectionService"]
    ReqSvc["RequestService"]
    CatSvc["*CatalogService\n(Asset, Location, Technician, RepairDept)"]
    ProfAdmin["ProfileAdminService"]
    Maint["MaintenanceService\n(stub)"]
    Realtime["RealtimeNotificationService"]
    ReqAsset["RequesterAssetCatalog"]
    LocTree["LocationTreeCache"]
  end

  subgraph CORE_REPO["BAAZ.CMMS.Core — Repositories"]
    ReqRepo["RequestRepository"]
    AssetRepo["AssetRepository"]
    LocRepo["LocationRepository"]
    TechRepo["TechnicianRepository"]
    DeptRepo["RepairDepartmentRepository"]
    ProfRepo["ProfileRepository"]
    ScopeRepo["ProfileLocationScopeRepository"]
    Junction["JunctionLinkRepository"]
  end

  subgraph CORE_DTO["BAAZ.CMMS.Core — модели"]
    UI["Models/*\n(ListItem, EditInput, CreateRequestInput)"]
    PG["Data/Models/*Model\n(PostgREST)"]
    RestDto["Repositories/Dtos/*\n(embed REST)"]
  end

  subgraph CORE_IO["BAAZ.CMMS.Core — доступ к Supabase"]
    Prov["SupabaseClientProvider"]
    GW["SupabaseGateway"]
    Rest["SupabaseRestClient"]
    Edge["AdminUsersFunctionClient"]
  end

  subgraph DB["Supabase / PostgreSQL"]
    GoTrue["auth.users\n(GoTrue)"]
    Tables["public.*\n(RLS)"]
    RPC["RPC\n(create_request, accept_request,\nprofile_accessible_location_ids, …)"]
    Fn["Edge Function\nadmin-users"]
    RT["Realtime\n(requests, maintenance_schedule,\nwork_reports, request_repair_departments)"]
  end

  Cred <-->|"Session JWT"| Prov
  Login --> Auth
  VM --> Auth & Conn & ReqSvc & CatSvc & ProfAdmin & Maint & Realtime & ReqAsset & LocTree

  Auth --> Prov
  Auth --> Rest
  Conn --> Prov
  Realtime --> Prov

  ReqSvc --> ReqRepo
  CatSvc --> AssetRepo & LocRepo & TechRepo & DeptRepo
  CatSvc --> Rest
  ProfAdmin --> Edge & ProfRepo & ScopeRepo
  ReqAsset --> AssetRepo
  LocTree --> LocRepo

  ReqRepo --> Rest & RestDto
  AssetRepo & LocRepo & TechRepo & DeptRepo & ProfRepo & ScopeRepo & Junction --> GW
  GW --> PG

  Prov --> GoTrue & Tables & RT
  GW -->|"PostgREST\n.From T()"| Tables
  Rest -->|"HTTP /rest/v1/*\nGET embed, PATCH, POST,\nCallRpc*, CallRpcVoid"| Tables & RPC
  Edge -->|"HTTP /functions/v1/admin-users"| Fn
  Fn --> GoTrue & Tables

  VM -.->|"маппинг"| UI
  ReqRepo & CatSvc -.-> UI
  GW -.-> PG
```

## Слои

| Слой | Проект | Роль |
|------|--------|------|
| UI | `BAAZ.CMMS.App` | Страницы, ViewModel, WinUI. Без прямого доступа к БД. |
| Домен | `BAAZ.CMMS.Core/Services` | Бизнес-логика, оркестрация, маппинг в UI-модели (`Models/*`). |
| Данные | `BAAZ.CMMS.Core/Repositories` | I/O: таблицы, RPC, embed-запросы. |
| Транспорт | `SupabaseGateway`, `SupabaseRestClient`, `AdminUsersFunctionClient` | Три канала к Supabase (см. ниже). |

Сессия (JWT) хранится в **Windows Credential Manager** (`WindowsCredentialSessionPersistence`) и подставляется во все HTTP-вызовы через `SupabaseClientProvider`.

## Три канала к БД

| Канал | .NET API | Когда |
|-------|----------|-------|
| **SDK PostgREST** | `ISupabaseGateway.From<*Model>()` | Простой CRUD по одной таблице (`assets`, `locations`, `technicians`, …). |
| **Raw REST / RPC** | `SupabaseRestClient` | Join/embed (`requests` + связи), RPC жизненного цикла заявок, скалярные и массивные RPC. |
| **Edge Function** | `AdminUsersFunctionClient` | Админ-операции с `auth.users` (создание, бан, удаление) — service-role только на сервере. |

Дополнительно:

- **GoTrue** — `AuthService` → `Supabase.Client.Auth` (вход/выход).
- **Health** — `ConnectionService` → `GET /auth/v1/health`.
- **Realtime** — `RealtimeNotificationService` → подписки на `requests`, `maintenance_schedule`, `work_reports`, `request_repair_departments`; reconnect при восстановлении связи. App: `ShellNotificationPresenter`, `INavBadgeService`, `IWindowsToastService`.

## Модели данных

- `Data/Models/*Model` — PostgREST-таблицы (`[Table]`, snake_case колонки).
- `Repositories/Dtos/*` — узкие DTO для embed/join через raw REST.
- `Models/*` — UI-формы: `*ListItem`, `*EditInput`, `CreateRequestInput` и т.п.

ViewModel работает только с UI-моделями; репозитории не протекают в App.

## Примеры потоков

**Новая заявка (UC-R1):**

`NewRequestViewModel` → `RequestService` → `RequestRepository.CreateViaRpcAsync` → `SupabaseRestClient.CallRpcScalarAsync<Guid>` → `POST /rest/v1/rpc/create_request` → PostgreSQL `create_request()` → `CreateRequestResult`.

**Каталог оборудования (UC-A4):**

`AssetRegistryViewModel` → `IAssetCatalogService` → `AssetRepository` → `ISupabaseGateway.From<AssetModel>()` → `public.assets` (RLS) → `AssetListItem`.

**Пользователи (UC-A2):**

`UsersViewModel` → `ProfileAdminService` → `AdminUsersFunctionClient` (list/create/ban/delete) + `ProfileLocationScopeRepository` (scopes через PostgREST).

## Заметки

- `RequestRepository` — гибрид: RPC + raw REST с embed, не только `From<T>()`.
- RPC со скалярным `returns uuid` (например `create_request`) — через `CallRpcScalarAsync`; `returns uuid[]` — через `CallRpcAsync`.
- `MaintenanceService` пока stub — в диаграмме отмечен, к БД не ходит.
- RLS на стороне Postgres ограничивает видимость по роли (`admin`, `dispatcher`, `requester`); клиент всегда ходит с JWT пользователя, не с service-role.
