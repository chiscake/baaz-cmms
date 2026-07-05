# Интеграция с DowntimeTracker (Prostoi)

Read-only API для учёта простоев. DT **не пишет** в CMMS — только читает view схемы `integration` через PostgREST (publishable key + `Accept-Profile: integration`).

Клиентская документация (репозиторий **Prostoi** / baaz-downtime-tracker): `docs/CMMS_INTEGRATION.md`.

Целевая модель v2 (Realtime, авто-события): [docs/use-cases/downtime-tracker.md](use-cases/downtime-tracker.md).

## Схема `integration`

Миграция: [`supabase/migrations/160_integration_downtime_tracker.sql`](../supabase/migrations/160_integration_downtime_tracker.sql).

| View | Назначение |
|------|------------|
| `v_assets_lookup` | Поиск ОС по `asset_number` (= инв. № в Prostoi) |
| `v_requests_by_asset` | Заявки по станку |
| `v_work_reports_by_asset` | Отчёты по станку / заявке |
| `v_request_departments` | Отделы маршрутизации заявки |

### `v_requests_by_asset`

| Колонка | Описание |
|---------|----------|
| `asset_id`, `asset_number` | ОС |
| `request_id`, `request_number` | Заявка |
| `type`, `status`, `title`, `description` | Основные поля |
| `priority`, `repair_zone`, `contractor_name` | Карточка заявки |
| `requester_name` | ФИО заявителя |
| `created_at`, `updated_at` | Аудит |

### `v_work_reports_by_asset`

| Колонка | Описание |
|---------|----------|
| `asset_id`, `request_id`, `request_number` | Связи |
| `work_report_id` | UUID отчёта |
| `work_performed`, `actual_duration_hours`, `maintenance_type` | Содержание |
| `repair_department_name`, `technician_full_name` | Исполнение |
| `defects_found`, `notes` | Доп. поля |
| `created_at` | Дата отчёта |

### `v_request_departments`

| Колонка | Описание |
|---------|----------|
| `request_id` | Заявка |
| `repair_department_name` | Отдел |
| `assignee_name` | Назначенный техник |
| `has_work_report` | Есть отчёт отдела |

## Seed и симметрия с Mock

DT-заявки для демо: [seed-dt-integration-data.mjs](../scripts/seed-dt-integration-data.mjs) — зеркало `Prostoi/Integration/Fixtures/` (репозиторий Prostoi).

- UUID `b222…201`–`207` (заявки), `c333…302`–`306` (отчёты)
- `REQ-2026-0047` — closed multi-dept (РМУ + КИПиА), 2 отчёта на одну заявку

После `supabase db reset`: `node scripts/seed-test-users.mjs`.

## Права

`GRANT SELECT` на все `integration.v_*` для роли `anon` (publishable key). RLS на `public.*` не применяется к anon напрямую — view owned by postgres.

## Синхронизация документации

При изменении контракта обновлять этот файл и, если доступен репозиторий Prostoi, `docs/CMMS_INTEGRATION.md` + fixtures там же (см. [AGENTS.md](../AGENTS.md)).
