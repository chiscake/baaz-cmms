# Логическая модель данных — BAAZ CMMS

Сущности предметной области с атрибутами, связями и кардинальностями. Охват: `auth.users` и основные таблицы `public.*`. Независима от конкретной СУБД.

> В блоках сущностей — только **содержательные** атрибуты. PK, FK и UK задаются связями. На диаграмме не показаны: junction-таблицы, `audit_log`, `request_status_history`, представления `effective_maintenance_norms` и `asset_maintenance_status`.

## ER-диаграмма

```mermaid
erDiagram
    auth_users ||--|| profiles : "расширяет"
    locations ||--o{ locations : "parent"
    locations ||--o{ profiles : "место работы"
    profiles }o--o{ locations : "зона доступа"
    locations ||--|{ assets : "размещён в"

    repair_departments ||--o{ profiles : "диспетчер"
    repair_departments ||--o{ technicians : "состоит в"
    repair_departments ||--o{ work_reports : "сдаёт"
    repair_departments }o--o{ requests : "маршрут"
    repair_departments }o--o{ category_maintenance_norms : "ответственен"
    repair_departments }o--o{ maintenance_norms : "ответственен"
    repair_departments }o--o{ maintenance_schedule : "исполняет"
    repair_departments ||--o{ requests : "целевой отдел"

    equipment_categories ||--o{ assets : "категория"
    equipment_categories ||--o{ category_maintenance_norms : "пресет"

    category_maintenance_norms }o..o{ maintenance_norms : "наследование"

    assets ||--o{ maintenance_norms : "override"
    assets ||--|{ maintenance_schedule : "график"
    assets ||--o{ requests : "объект"

    maintenance_schedule ||--o{ work_reports : "отчёты"

    profiles ||--|{ requests : "заявитель"
    profiles ||--|{ work_reports : "автор"

    technicians }o--o{ requests : "исполнитель"
    technicians ||--|{ work_reports : "выполнил"

    requests ||--o{ work_reports : "отчёты"

    auth_users {
        email
        password
    }

    profiles {
        role
        full_name
        phone
    }

    locations {
        name
        is_active
    }

    repair_departments {
        name
        is_active
    }

    technicians {
        full_name
        specialty
        is_active
    }

    equipment_categories {
        description
        is_active
    }

    assets {
        name
        manufacturer
        model
        serial_number
        commissioning_date
        status
        description
    }

    category_maintenance_norms {
        maintenance_type
        interval_days
        description
    }

    maintenance_norms {
        maintenance_type
        interval_days
        description
        override_departments
    }

    maintenance_schedule {
        maintenance_type
        planned_date
        status
        notify_dispatchers
    }

    requests {
        type
        location_description
        title
        description
        priority
        repair_zone
        contractor_name
        status
    }

    work_reports {
        maintenance_type
        maintenance_types
        work_performed
        actual_duration_hours
        parts_used
        defects_found
        notes
    }
```

## Перечисления (доменные типы)

| Enum | Значения | Использование |
| --- | --- | --- |
| `user_role` | admin, dispatcher, requester | Роль пользователя |
| `asset_status` | active, maintenance, decommissioned | Статус объекта |
| `maintenance_type` | to1, to2, kr | Вид ТО |
| `schedule_status` | scheduled, in_progress, completed, overdue, cancelled | Позиция ППР |
| `request_type` | breakdown, service, inspection | Тип заявки |
| `request_status` | new, accepted, in_progress, completed, closed, rejected, cancelled | Статус заявки |
| `request_priority` | low, normal, high, critical | Приоритет |
| `repair_zone` | on_site, workshop, external | Место ремонта |

## Кардинальности и ограничения

| Связь | Кардинальность | Ограничение |
| --- | --- | --- |
| auth.users → profiles | 1 : 1 | Профиль обязателен для работы в CMMS |
| locations → locations | 1 : 0..* | Дерево; корень без родителя |
| profiles ↔ locations | M : N | Зона доступа заявителя (реализована через `profile_location_scopes`) |
| assets → requests | 1 : 0..* | Заявка может быть без объекта (только описание места) |
| requests ↔ repair_departments | M : N | Маршрутизация (реализована через `request_repair_departments`) |
| requests → work_reports | 1 : 0..* | Один отчёт на отдел |
| maintenance_schedule → work_reports | 1 : 0..* | Один отчёт на отдел |
| work_reports | XOR | `request_id` **или** `schedule_id` обязателен |
| requests (объект) | — | `asset_id` необязателен; `location_description` обязателен |
| category_maintenance_norms | 1 : 1 type | UNIQUE `(category_id, maintenance_type)` |
| maintenance_norms | 1 : 1 type | UNIQUE `(asset_id, maintenance_type)` |
| profiles (dispatcher) | — | `repair_department_id` обязателен при `role = dispatcher` |

## Связь с другими уровнями

| Уровень | Файл |
| --- | --- |
| Концептуальный | [`db-conceptual.md`](db-conceptual.md) |
| Физический | [`db-physical.md`](db-physical.md) |

Детализация столбцов — [`../DATABASE_TABLES.md`](../DATABASE_TABLES.md).

PlantUML: [`db-logical.puml`](db-logical.puml)
