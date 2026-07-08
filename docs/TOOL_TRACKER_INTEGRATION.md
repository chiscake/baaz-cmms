# Интеграция с BAAZ Tool Tracker (TMS)

Двусторонний контракт CMMS ↔ TMS. Парная документация (репозиторий **baaz-tool-tracker**): `docs/CMMS_INTEGRATION.md`.

Use-cases: [docs/use-cases/tool-tracker.md](use-cases/tool-tracker.md).

## Контуры

| Контур                     | Направление | Транспорт MVP                                                                    |
| -------------------------- | ----------- | -------------------------------------------------------------------------------- |
| **А — ремонт инструмента** | TMS → CMMS  | Edge Function `integration-tms-create-request`; read `integration.v_inventory_*` |
| **Б — выдача на наряд**    | CMMS → TMS  | HTTP FastAPI `/api/v1/integration/cmms/*`                                        |
| **REP-EVT-1**              | CMMS → TMS  | Webhook `/api/v1/integration/cmms/repair-request-status`                         |
| **ISS-EVT-1**              | TMS → CMMS  | Edge Function `integration-tms-tool-requisition-status`                         |

## Объект заявки CMMS (контур А)

Третий вариант на `public.requests` ([`030_tables_requests.sql`](../supabase/migrations/030_tables_requests.sql)):

| Вариант             | Поля                                                             |
| ------------------- | ---------------------------------------------------------------- |
| ОС                  | `asset_id`                                                       |
| Место               | `location_description`                                           |
| **Inventory (TMS)** | `inventory_id`, `inventory_kind`, snapshot (`inventory_name`, …) |

MVP: `inventory_kind = 'tool'`, `inventory_source = 'tms'`.

## Схема `integration`

Миграция: [`170_integration_tool_tracker.sql`](../supabase/migrations/170_integration_tool_tracker.sql).

| Объект                                     | Назначение            |
| ------------------------------------------ | --------------------- |
| `integration.tms_repair_client_refs`       | Idempotency REP-API-1 |
| `integration.v_inventory_requests`         | REP-API-2             |
| `integration.v_inventory_work_reports`     | REP-API-3             |
| `integration.v_repair_departments`         | REP-API-4 (справочник отделов для TMS) |
| `integration.v_active_task_for_technician` | UC-TT1 stub           |

PostgREST: `Accept-Profile: integration`.

## Локальные ссылки (контур Б)

[`040_tables_integrations.sql`](../supabase/migrations/040_tables_integrations.sql) — `tms_tool_requisition_links`.

## Edge Function

| Функция | ID | Назначение |
| ------- | -- | ---------- |
| `integration-tms-create-request/` | REP-API-1 | Создание inventory-заявки |
| `integration-tms-inventory-received/` | REP-API-2 | Подтверждение передачи (deliver) → `in_progress` |
| `integration-tms-tool-requisition-status/` | ISS-EVT-1 | Обновление `tms_tool_requisition_links.last_known_status` из TMS |

Auth: `CMMS_INTEGRATION_SECRET`. RPC `confirm_inventory_received` — также из UI CMMS (pickup).

Env: `CMMS_INTEGRATION_SECRET`, опционально `CMMS_INTEGRATION_REQUESTER_ID`.

В `supabase/config.toml`: `[functions.integration-tms-create-request] verify_jwt = false` — REP-API-1 использует shared secret, не JWT пользователя CMMS.

## Настройка ключей и секретов (сторона CMMS)

Парная инструкция для TMS: baaz-tool-tracker/docs/CMMS_INTEGRATION.md § Настройка ключей

### Типы credentials

| Тип                                      | Назначение                                  | Где задаётся (CMMS)                                                                 |
| ---------------------------------------- | ------------------------------------------- | ----------------------------------------------------------------------------------- |
| **Publishable key** CMMS                 | Обычный вход пользователей, PostgREST с RLS | Settings приложения → `SupabaseAnonKey`; шаблон в [`.env.example`](../.env.example) |
| **Service role** CMMS                    | Edge Function, seed-скрипты                 | [`.env`](../.env.example) / [`.env.cloud`](../.env.cloud.example); **не** в клиенте |
| **`TMS_INTEGRATION_SECRET`** (shared A)  | CMMS → TMS: контур Б (ISS-API), REP-EVT-1   | Settings → **TmsIntegrationSecret**                                                 |
| **`CMMS_INTEGRATION_SECRET`** (shared B) | TMS → CMMS: контур А (REP-API-1)            | Supabase Edge secrets (см. ниже)                                                    |

Integration-секреты — **отдельные случайные строки**, не ключи Supabase (`service_role`, `sb_secret_…`, JWT).

### Связка между системами

```
shared-secret-A  →  CMMS Settings: TmsIntegrationSecret
                 →  TMS .env:       TMS_INTEGRATION_SECRET

shared-secret-B  →  CMMS Edge:      CMMS_INTEGRATION_SECRET
                 →  TMS .env:       CMMS_INTEGRATION_SECRET
```

### Локально (dev)

**1. Ключи Supabase CMMS** — после `supabase start`:

```powershell
supabase status
# SUPABASE_URL, Publishable key, Secret key → .env (для pnpm db:seed)
```

**2. Edge Function** (контур А, опционально):

```powershell
# Пустой секрет = проверка Bearer отключена (удобно для отладки)
supabase secrets set CMMS_INTEGRATION_SECRET=
# Или явно:
supabase secrets set CMMS_INTEGRATION_SECRET=dev-local-cmms
```

Опционально заявитель inventory-заявок:

```powershell
supabase secrets set CMMS_INTEGRATION_REQUESTER_ID=<uuid profiles после seed-test-users>
```

Перезапустить edge runtime (`pnpm fn:serve` или `supabase start`).

**3. Настройки WinUI-приложения** (Settings → интеграция TMS):

| Поле                              | Local Live                                                     |
| --------------------------------- | -------------------------------------------------------------- |
| `TmsIntegrationMode`              | `Live` (или `Mock` без TMS)                                    |
| `TmsBaseUrl`                      | `http://127.0.0.1:8000`                                        |
| `TmsIntegrationSecret`            | пусто **или** то же, что `TMS_INTEGRATION_SECRET` в TMS `.env` |
| `SupabaseUrl` / `SupabaseAnonKey` | из `supabase status` CMMS                                      |

**4. Файл `.env` в корне репо** (только CLI/seed, не читается приложением):

```env
SUPABASE_URL=http://127.0.0.1:54321
SUPABASE_PUBLISHABLE_KEY=sb_publishable_...
SUPABASE_SERVICE_ROLE_KEY=sb_secret_...
```

### Production

**1. `.env` / `.env.cloud`** (операции, seed):

```env
SUPABASE_URL=https://cmms-prod.supabase.co
SUPABASE_PUBLISHABLE_KEY=sb_publishable_...
SUPABASE_SERVICE_ROLE_KEY=sb_secret_...
```

Ключи: Dashboard → Project Settings → API. **Service role не коммитить.**

**2. Edge Function secrets** (Dashboard или CLI):

```powershell
supabase secrets set CMMS_INTEGRATION_SECRET=<shared-secret-B>
supabase secrets set CMMS_INTEGRATION_REQUESTER_ID=<uuid системного заявителя>
```

**3. Settings на рабочих станциях CMMS:**

| Поле                   | Production                      |
| ---------------------- | ------------------------------- |
| `SupabaseUrl`          | `https://cmms-prod.supabase.co` |
| `SupabaseAnonKey`      | publishable CMMS                |
| `TmsIntegrationMode`   | `Live`                          |
| `TmsBaseUrl`           | `https://tms.example.com`       |
| `TmsIntegrationSecret` | `<shared-secret-A>`             |

**4. Генерация shared-секретов** (PowerShell):

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
# или: python -c "import secrets; print(secrets.token_urlsafe(32))"
```

Два **разных** значения для A и B.

## Mock / Live (CMMS)

Настройки приложения: **Mock** (JSON fixtures) / **Live** (HTTP TMS).

| Ключ settings          | Default                 |
| ---------------------- | ----------------------- |
| `TmsIntegrationMode`   | `Mock`                  |
| `TmsBaseUrl`           | `http://127.0.0.1:8000` |
| `TmsIntegrationSecret` | (пусто)                 |

Fixtures: `BAAZ.CMMS.Core/Integrations/ToolTracker/Fixtures/`.

DI: `TmsIntegrationSettings` в [`App.xaml.cs`](../BAAZ.CMMS.App/App.xaml.cs).

## Seed

[`scripts/seed-tt-integration-data.mjs`](../scripts/seed-tt-integration-data.mjs) — после `supabase db reset` + `seed-test-users.mjs`.

Demo UUID: tool `d1000000-0000-4000-8000-000000000001`, CMMS request repair `b2220000-0000-4000-8000-000000000301`.

## Порты (локально)

| Сервис        | URL                      |
| ------------- | ------------------------ |
| CMMS Supabase | `http://127.0.0.1:54321` |
| TMS Supabase  | `http://127.0.0.1:55321` |
| TMS FastAPI   | `http://127.0.0.1:8000`  |

## E2E checklist (manual)

### Mock (offline TMS)

1. CMMS **Settings → TMS → Mock**, сохранить.
2. Открыть **RequestDetail** заявки с TMS-ссылкой — refresh статуса из fixture.
3. **ToolRequisition** — склады/каталог из JSON fixtures.

### Live (54321 + 55321 + :8000)

1. `supabase db reset` в обоих репо; CMMS: `node scripts/seed-test-users.mjs`.
2. TMS: `.env` по разделу «Настройка ключей и секретов (сторона TMS)» в `baaz-tool-tracker/docs/CMMS_INTEGRATION.md`; `uvicorn main:app --reload --port 8000`.
3. CMMS Settings → TMS **Live**, URL `http://127.0.0.1:8000`, **TmsIntegrationSecret** = `TMS_INTEGRATION_SECRET` из TMS (или пусто в dev).
4. **Контур А (pickup):** send → TMS `pending_repair` → CMMS accept + assign → «Инструмент получен» → `in_progress` / TMS `maintenance`.
5. **Контур А (deliver):** send deliver → «Передан в отдел» → `in_progress` без клика диспетчера.
6. **Контур Б:** CMMS ToolRequisition → ISS-API-1 → статус в RequestDetail.
7. **ISS-EVT-1:** clerk reserve/issue в TMS → CMMS toast «зарезервирована» / «готова к выдаче» без ручного refresh; история заявок обновляется через Realtime на `tms_tool_requisition_links`.
8. **REP-EVT-1:** закрыть inventory-заявку → TMS `pending_return` → «Принят на склад» → `available`.

## Синхронизация документации

При изменении контракта обновлять этот файл и `baaz-tool-tracker/docs/CMMS_INTEGRATION.md` + symmetric fixtures (см. [AGENTS.md](../AGENTS.md)).
