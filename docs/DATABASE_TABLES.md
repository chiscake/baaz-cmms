# Схема таблиц БД

---

# Схема `public` — BAAZ CMMS (ремонты, ППР, заявки)

```mermaid
erDiagram
    auth_users ||--|| profiles : "id"
    locations ||--o{ locations : "parent_id"
    locations ||--o{ profiles : "location_id"
    locations ||--|{ assets : "location_id"

    repair_departments ||--o{ profiles : "repair_department_id"
    repair_departments ||--o{ technicians : "repair_department_id"
    repair_departments ||--o{ work_reports : "repair_department_id"
    repair_departments ||--o{ request_repair_departments : "repair_department_id"
    repair_departments ||--o{ maintenance_norms_departments : "repair_department_id"
    repair_departments ||--o{ maintenance_schedule_departments : "repair_department_id"

    equipment_categories ||--o{ assets : "category_id nullable"
    equipment_categories ||--o{ category_maintenance_norms : "category_id"
    category_maintenance_norms ||--o{ category_maintenance_norms_departments : "category_norm_id"

    assets ||--o{ maintenance_norms : "asset_id sparse overrides"
    assets ||--|{ maintenance_schedule : "asset_id"
    assets ||--o{ requests : "asset_id"

    maintenance_norms ||--o{ maintenance_norms_departments : "norm_id"
    category_maintenance_norms }o..o{ maintenance_norms : "COALESCE per type"
    maintenance_norms ||..o{ asset_maintenance_status : "norm_id"
    maintenance_schedule ||--o{ maintenance_schedule_departments : "schedule_id"
    maintenance_schedule ||--o{ work_reports : "schedule_id"

    profiles ||--|{ requests : "requester_id"
    profiles ||--|{ request_status_history : "changed_by"
    profiles ||--|{ work_reports : "author_id"

    technicians ||--o{ request_repair_departments : "assignee_id"
    technicians ||--|{ work_reports : "technician_id"

    requests ||--o{ request_repair_departments : "request_id"
    requests ||--|{ request_status_history : "request_id"
    requests ||--o{ work_reports : "request_id"

    work_reports }o..o{ asset_maintenance_status : "источник дат ТО"

    auth_users { uuid id PK }

    locations {
        uuid id PK
        uuid parent_id FK
        text name
        text code
    }

    repair_departments {
        uuid id PK
        text name
        text code
    }

    profiles {
        uuid id PK_FK
        user_role role
        text full_name
        uuid location_id FK
        uuid repair_department_id FK
    }

    technicians {
        uuid id PK
        text full_name
        text specialty
        boolean is_active
        uuid repair_department_id FK
    }

    assets {
        uuid id PK
        text asset_number UK
        text name
        uuid location_id FK
        uuid category_id FK
        asset_status status
    }

    equipment_categories {
        uuid id PK
        text name UK
        text description
        boolean is_active
    }

    category_maintenance_norms {
        uuid id PK
        uuid category_id FK
        maintenance_type maintenance_type
        int interval_days
    }

    category_maintenance_norms_departments {
        uuid category_norm_id FK
        uuid repair_department_id FK
    }

    maintenance_norms {
        uuid id PK
        uuid asset_id FK
        maintenance_type maintenance_type
        int interval_days
        boolean override_departments
    }

    maintenance_norms_departments {
        uuid norm_id FK
        uuid repair_department_id FK
    }

    asset_maintenance_status {
        uuid norm_id
        uuid asset_id
        maintenance_type maintenance_type
        date last_maintenance_date
        date next_maintenance_date
    }

    maintenance_schedule {
        uuid id PK
        uuid asset_id FK
        maintenance_type maintenance_type
        date planned_date
        schedule_status status
    }

    maintenance_schedule_departments {
        uuid schedule_id FK
        uuid repair_department_id FK
    }

    requests {
        uuid id PK
        text request_number UK
        request_type type
        uuid asset_id FK
        uuid requester_id FK
        request_status status
        repair_zone repair_zone
        text contractor_name
    }

    request_repair_departments {
        uuid request_id FK
        uuid repair_department_id FK
        uuid assignee_id FK
        timestamptz added_at
    }

    request_status_history {
        uuid id PK
        uuid request_id FK
        uuid changed_by FK
        request_status old_status
        request_status new_status
    }

    work_reports {
        uuid id PK
        uuid request_id FK
        uuid schedule_id FK
        uuid repair_department_id FK
        maintenance_type maintenance_type
        uuid author_id FK
        uuid technician_id FK
        jsonb parts_used
    }
```

> **Примечание:** `asset_maintenance_status` — представление (view), не таблица. Учитывает только **завершённые** позиции ППР (`maintenance_schedule.status = 'completed'`) и **закрытые** аварийные заявки (`requests.status = 'closed'`).

---

# locations

Справочник локаций предприятия (древовидная структура: корпус → цех → участок).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `parent_id` | uuid | YES | — | FK → `locations(id) ON DELETE RESTRICT`; `NULL` — корневой узел |
| `name` | text | NO | — | Наименование |
| `code` | text | YES | — | Краткий код |
| `is_active` | boolean | NO | `true` | Архивный статус узла |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

# repair_departments

Ремонтные отделы / службы (РМУ, Энергетика, КИПиА и т.п.).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `name` | text | NO | — | Полное название |
| `code` | text | YES | — | UNIQUE, краткий код (`RMU`, `ENERGY`, `KIP`) |
| `is_active` | boolean | NO | `true` | Архив; неактивные скрываются из ComboBox |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

# profiles

Профили пользователей (расширение `auth.users`).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK, `REFERENCES auth.users(id) ON DELETE CASCADE` |
| `role` | user_role | NO | `requester` | Роль |
| `full_name` | text | YES | — | ФИО |
| `location_id` | uuid | YES | — | FK → `locations(id) ON DELETE RESTRICT` — **место работы** (кабинет, отдел) |
| `repair_department_id` | uuid | YES | — | FK → `repair_departments(id) ON DELETE SET NULL`; обязателен для `dispatcher`, NULL для остальных |
| `phone` | text | YES | — | Телефон |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

**CHECK:** `role <> 'dispatcher' OR repair_department_id IS NOT NULL`

Зоны доступа заявителя к оборудованию/локациям — отдельная таблица `profile_location_scopes` (см. ниже). Для `requester` и `dispatcher` (режим заявителя): если нет явных scopes, fallback — поддерево `location_id`.

# profile_location_scopes

Якоря зон доступа заявителя (M:N `profiles` ↔ `locations`): роли `requester` и `dispatcher`. Каждая строка — узел-якорь; эффективный доступ = объединение поддеревьев (`profile_accessible_location_ids`).

| Колонка | Тип | NULL | Описание |
| --- | --- | --- | --- |
| `profile_id` | uuid | NO | FK → `profiles(id) ON DELETE CASCADE` |
| `location_id` | uuid | NO | FK → `locations(id) ON DELETE RESTRICT` |

**PK:** `(profile_id, location_id)`

**SQL-функции:** `profile_scope_anchors`, `profile_accessible_location_ids`, `requester_can_access_location` — см. `050_functions_security_helpers.sql`.

# technicians

Персонал ТОиР без учётной записи (слесари, электрики и т.д.).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `full_name` | text | NO | — | ФИО |
| `specialty` | text | NO | — | Специализация |
| `is_active` | boolean | NO | `true` | Работает; `false` — архив |
| `repair_department_id` | uuid | YES | — | FK → `repair_departments(id) ON DELETE SET NULL` |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

# assets

Реестр инвентарных объектов обслуживания (станки, прессы и т.п.).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `asset_number` | text | NO | — | UNIQUE, инвентарный номер |
| `name` | text | NO | — | Наименование |
| `location_id` | uuid | NO | — | FK → `locations(id) ON DELETE RESTRICT` |
| `category_id` | uuid | YES | — | FK → `equipment_categories(id) ON DELETE SET NULL`; nullable — ППР без категории только через индивидуальные `maintenance_norms` |
| `manufacturer` | text | YES | — | Производитель |
| `model` | text | YES | — | Модель |
| `serial_number` | text | YES | — | Серийный номер |
| `commissioning_date` | date | YES | — | Дата ввода в эксплуатацию |
| `status` | asset_status | YES | `active` | Синхронизируется триггером из `requests` |
| `description` | text | YES | — | Описание |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

# equipment_categories

Категория эксплуатации оборудования (UC-A5) — свободный текст, без enum (напр. «Резка (твёрдый материал)»). Опциональна на `assets.category_id`; служит источником пресетов ТО.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `name` | text | NO | — | UNIQUE, отображаемое имя |
| `description` | text | YES | — | Режим эксплуатации, примечания |
| `is_active` | boolean | NO | `true` | |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

# category_maintenance_norms

Пресеты нормативов ТО на уровне категории эксплуатации.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `category_id` | uuid | NO | — | FK → `equipment_categories(id) ON DELETE CASCADE` |
| `maintenance_type` | maintenance_type | NO | — | Вид ТО |
| `interval_days` | integer | NO | — | Межремонтный интервал пресета |
| `description` | text | YES | — | Что входит |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

**UNIQUE:** `(category_id, maintenance_type)`

# category_maintenance_norms_departments

Ответственные службы пресета (многие-ко-многим). Зеркало `maintenance_norms_departments`.

| Колонка | Тип | Описание |
| --- | --- | --- |
| `category_norm_id` | uuid FK | → `category_maintenance_norms(id) ON DELETE CASCADE` |
| `repair_department_id` | uuid FK | → `repair_departments(id) ON DELETE RESTRICT` |

**PK:** `(category_norm_id, repair_department_id)`

# maintenance_norms

Индивидуальные переопределения норматива поверх пресета категории (Hierarchical Override, UC-A5) — **sparse**: строка существует только если хотя бы одно поле переопределено, либо это standalone-норматив объекта без `category_id`. Рабочее значение = `COALESCE(maintenance_norms.*, category_maintenance_norms.*)` (см. view `effective_maintenance_norms`).

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `asset_id` | uuid | NO | — | FK → `assets(id) ON DELETE CASCADE` |
| `maintenance_type` | maintenance_type | NO | — | Вид ТО |
| `interval_days` | integer | YES | — | `NULL` — наследовать из пресета категории |
| `description` | text | YES | — | `NULL` — наследовать из пресета |
| `override_departments` | boolean | NO | `false` | `true` — отделы берутся из `maintenance_norms_departments`, иначе из пресета |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

**UNIQUE:** `(asset_id, maintenance_type)`

# maintenance_norms_departments

Ответственные службы по индивидуальному override (многие-ко-многим, используется только при `override_departments = true`). Шаблон для `maintenance_schedule_departments`.

| Колонка | Тип | Описание |
| --- | --- | --- |
| `norm_id` | uuid FK | → `maintenance_norms(id) ON DELETE CASCADE` |
| `repair_department_id` | uuid FK | → `repair_departments(id) ON DELETE RESTRICT` |

**PK:** `(norm_id, repair_department_id)`

# effective_maintenance_norms (view)

Merge пресета категории и индивидуального override по каждому `(asset_id, maintenance_type)` — источник для `asset_maintenance_status` и UI нормативов.

| Колонка | Тип | Описание |
| --- | --- | --- |
| `asset_id` | uuid | Объект |
| `maintenance_type` | maintenance_type | Вид ТО |
| `effective_interval_days` | integer | `COALESCE(override, preset)` |
| `effective_description` | text | `COALESCE(override, preset)` |
| `category_norm_id` | uuid | Пресет (если есть) |
| `override_norm_id` | uuid | Индивидуальный override (если есть) |
| `source` | text | `'override'` / `'category'` / `'override_partial'` |
| `is_interval_overridden` / `is_description_overridden` / `is_departments_overridden` | boolean | Флаги переопределения по полю |

Ответственные отделы для пары `(asset_id, maintenance_type)` — функция `get_effective_norm_departments(p_asset_id, p_maintenance_type) returns uuid[]` (override, если `override_departments = true`, иначе пресет, иначе пустой массив).

# asset_maintenance_status (view)

Виртуальная карточка: дата последнего и следующего ТО по каждому **effective**-нормативу (`effective_maintenance_norms`). Объекты без effective-норматива (нет ни пресета, ни override) в этом view не появляются — штатное состояние («без ППР»).

Учитывает **два источника** фактов:
1. Плановое ТО — `work_reports.schedule_id` → `maintenance_schedule` (только `status = 'completed'`)
2. Аварийный ремонт с `work_reports.maintenance_type` задан → `requests` (только `status = 'closed'`)

| Колонка | Тип | Описание |
| --- | --- | --- |
| `norm_id` | uuid | `COALESCE(override_norm_id, category_norm_id)` — стабильный идентификатор для UI |
| `asset_id` | uuid | Объект |
| `maintenance_type` | maintenance_type | Вид ТО |
| `interval_days` | integer | Effective-интервал |
| `last_maintenance_date` | date | `MAX(work_reports.created_at)` |
| `next_maintenance_date` | date | `last_maintenance_date + interval_days`; если не было — `CURRENT_DATE` |

# maintenance_schedule

График ППР. Создаётся через RPC `create_schedule_entry(...)` для атомарности с `maintenance_schedule_departments`.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `asset_id` | uuid | NO | — | FK → `assets(id) ON DELETE CASCADE` |
| `maintenance_type` | maintenance_type | NO | — | Вид ТО |
| `planned_date` | date | NO | — | Плановая дата |
| `status` | schedule_status | YES | `scheduled` | `completed` — триггером при отчётах всех назначенных отделов |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

**Правило завершения ППР:** `status = 'completed'` устанавливается автоматически триггером `work_reports_check_schedule_completion` когда отчитались **все** отделы из `maintenance_schedule_departments`. Если строк в `maintenance_schedule_departments` нет — статус меняется только вручную.

# maintenance_schedule_departments

Исполнители конкретной позиции ППР (многие-ко-многим). Используется триггером завершения и RLS диспетчера.

| Колонка | Тип | Описание |
| --- | --- | --- |
| `schedule_id` | uuid FK | → `maintenance_schedule(id) ON DELETE CASCADE` |
| `repair_department_id` | uuid FK | → `repair_departments(id) ON DELETE RESTRICT` |

**PK:** `(schedule_id, repair_department_id)`

# requests

Заявки на ремонт.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `request_number` | text | NO | — | UNIQUE |
| `type` | request_type | NO | `breakdown` | |
| `asset_id` | uuid | YES | — | FK → `assets(id) ON DELETE RESTRICT` |
| `location_description` | text | NO | — | Текстовое место/объект |
| `requester_id` | uuid | NO | — | FK → `profiles(id) ON DELETE RESTRICT` |
| `title` | text | NO | — | Краткое описание |
| `description` | text | YES | — | Подробное описание (необязательно) |
| `priority` | request_priority | YES | `normal` | |
| `repair_zone` | repair_zone | NO | `on_site` | Где ведутся работы |
| `contractor_name` | text | YES | — | Название подрядчика, заполняется при `repair_zone = 'external'` |
| `status` | request_status | YES | `new` | |
| `created_at` | timestamptz | YES | `now()` | |
| `updated_at` | timestamptz | YES | `now()` | |

**CHECK:** `asset_id IS NOT NULL OR location_description IS NOT NULL`

# request_repair_departments

Маршрутизация заявки по ремонтным отделам (многие-ко-многим). Заполняет диспетчер при принятии заявки (`accept_request`), при передаче в другой отдел (`transfer_request_department`) или подключении дополнительного отдела (`add_request_department`). Каждая строка хранит **своего** исполнителя — заявка может вестись несколькими отделами параллельно.

| Колонка | Тип | Описание |
| --- | --- | --- |
| `request_id` | uuid FK | → `requests(id) ON DELETE CASCADE` |
| `repair_department_id` | uuid FK | → `repair_departments(id) ON DELETE RESTRICT` |
| `assignee_id` | uuid FK | → `technicians(id) ON DELETE SET NULL` — исполнитель от этого отдела |
| `added_at` | timestamptz | Когда отдел подключён к заявке |

**PK:** `(request_id, repair_department_id)`

# request_status_history

История изменений статуса заявки.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `request_id` | uuid | NO | — | FK → `requests(id) ON DELETE CASCADE` |
| `changed_by` | uuid | NO | — | FK → `profiles(id) ON DELETE RESTRICT` |
| `old_status` | request_status | NO | — | |
| `new_status` | request_status | NO | — | |
| `comment` | text | YES | — | |
| `created_at` | timestamptz | YES | `now()` | |

# work_reports

Отчёты о выполненных работах. **Каждый ремонтный отдел сдаёт свой отчёт** в рамках одной заявки или позиции ППР.

| Колонка | Тип | NULL | По умолчанию | Описание |
| --- | --- | --- | --- | --- |
| `id` | uuid | NO | — | PK |
| `request_id` | uuid | YES | — | FK → `requests(id) ON DELETE RESTRICT` |
| `schedule_id` | uuid | YES | — | FK → `maintenance_schedule(id) ON DELETE RESTRICT` |
| `repair_department_id` | uuid | NO | — | FK → `repair_departments(id) ON DELETE RESTRICT` — кто сдаёт отчёт |
| `maintenance_type` | maintenance_type | YES | — | Для аварийных заявок, перекрывающих плановый вид ТО |
| `author_id` | uuid | NO | — | FK → `profiles(id) ON DELETE RESTRICT` |
| `technician_id` | uuid | NO | — | FK → `technicians(id) ON DELETE RESTRICT` |
| `work_performed` | text | NO | — | Описание работ |
| `actual_duration_hours` | numeric | NO | — | Фактическое время |
| `parts_used` | jsonb | YES | — | Материалы и запчасти (свободный jsonb в отчёте; учёт на складе — во внешней системе) |
| `defects_found` | text | YES | — | |
| `notes` | text | YES | — | |
| `created_at` | timestamptz | YES | `now()` | |

**CHECK:** `request_id IS NOT NULL OR schedule_id IS NOT NULL`

**Уникальные индексы:**
- `(schedule_id, repair_department_id) WHERE schedule_id IS NOT NULL`
- `(request_id, repair_department_id) WHERE request_id IS NOT NULL`

**Триггеры:**
- `work_reports_validate_department` (BEFORE INSERT) — `repair_department_id` должен присутствовать в `request_repair_departments` для данного `request_id`.
- `work_reports_check_request_completion` (AFTER INSERT) — когда отчитались все отделы из `request_repair_departments` и `requests.status = 'in_progress'`, переводит заявку в `completed` (по аналогии с `check_schedule_completion` для ППР).

# RPC-функции жизненного цикла заявки

Все функции — `security definer`, вызываются через `client.Rpc(name, params)`. Проверяют роль (`admin`/`dispatcher`) и принадлежность отдела через `request_repair_departments`; пишут запись в `request_status_history`.

| Функция | Назначение |
| --- | --- |
| `accept_request(p_request_id, p_assignee_id?, p_comment?)` | UC-D1: `new → accepted`; создаёт строку `request_repair_departments` для отдела текущего диспетчера, опционально с исполнителем |
| `reject_request(p_request_id, p_comment?)` | UC-D1: `new → rejected` |
| `transfer_request_department(p_request_id, p_new_department_id, p_comment?)` | По результатам осмотра: удаляет строку своего отдела, добавляет строку нового — статус не меняется |
| `add_request_department(p_request_id, p_department_id, p_assignee_id?, p_comment?)` | Подключает дополнительный отдел для совместной работы — статус не меняется |
| `assign_request_technician(p_request_id, p_technician_id)` | Назначает/меняет исполнителя в рамках своего отдела (`request_repair_departments.assignee_id`) |
| `update_request_repair_zone(p_request_id, p_repair_zone, p_contractor_name?, p_comment?)` | Меняет `requests.repair_zone`/`contractor_name` — не смена статуса, комментарий с `old_status = new_status` |
| `start_request_work(p_request_id, p_comment?)` | UC-D2: `accepted → in_progress` |
| `close_request_as_staff(p_request_id, p_comment?)` | UC-R4 (делегирование): `completed → closed` от лица диспетчера/admin |

---

# Перечисления (enums)

## `public.user_role`

| Значение | Описание |
| --- | --- |
| `admin` | Администратор |
| `dispatcher` | Диспетчер ТОиР (привязан к `repair_department_id`) |
| `requester` | Обычный сотрудник |

## `public.asset_status`

| Значение | Описание |
| --- | --- |
| `active` | В эксплуатации |
| `maintenance` | На обслуживании |
| `decommissioned` | Снято с эксплуатации |

## `public.maintenance_type`

| Значение | Описание |
| --- | --- |
| `to1` | ТО-1 (ежемесячное) |
| `to2` | ТО-2 (сезонное) |
| `kr` | КР — капитальный ремонт |

## `public.schedule_status`

| Значение | Описание |
| --- | --- |
| `scheduled` | Плановая |
| `completed` | Выполнена (все отделы отчитались) |
| `overdue` | Просрочена |
| `cancelled` | Отменена |

## `public.request_type`

| Значение | Описание |
| --- | --- |
| `breakdown` | Авария / поломка |
| `service` | Хозяйственный запрос |
| `inspection` | Внеплановый осмотр |

## `public.request_status`

| Значение | Описание |
| --- | --- |
| `new` | Новая |
| `accepted` | Принята |
| `in_progress` | Выполняется |
| `completed` | Выполнена |
| `closed` | Закрыта |
| `rejected` | Отклонена |
| `cancelled` | Отменена |

## `public.request_priority`

| Значение | Описание |
| --- | --- |
| `low` | Низкий |
| `normal` | Обычный |
| `high` | Высокий |
| `critical` | Критический |

## `public.repair_zone`

| Значение | Описание |
| --- | --- |
| `on_site` | На месте установки |
| `workshop` | В ЦРМ / ремонтном цехе |
| `external` | У внешнего подрядчика |
