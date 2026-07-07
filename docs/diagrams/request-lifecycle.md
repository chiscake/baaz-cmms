# Жизненный цикл заявки на ремонт — BAAZ CMMS

Диаграмма последовательности основного сценария и ключевых альтернатив. Источник: [`docs/use-cases/overview.md`](../use-cases/overview.md) (UC-R1…R4, UC-D1…D2), RPC в `supabase/migrations/060_functions_domain_rpc.sql`.

**Диаграмма состояний (statechart):** [`request-lifecycle-statechart.md`](request-lifecycle-statechart.md)

---

## Диаграмма последовательности (полная)

```mermaid
sequenceDiagram
    autonumber
    actor R as Заявитель
    actor D as Диспетчер
    participant App as Приложение (CMMS)
    participant DB as PostgreSQL (Supabase)

    rect rgb(240, 248, 255)
    Note over R,DB:  Создание
    R->>App: Форма новой заявки
    App->>DB: RPC create_request()
    DB->>DB: INSERT requests, status = new
    DB-->>App: request_id
    App-->>R: Заявка создана
    end

    rect rgb(255, 248, 240)
    Note over D,DB:  Приёмка / отклонение / отмена
    D->>App: Входящие (status = new)
    alt Принять
        D->>App: accept_request(assignee?)
        App->>DB: RPC accept_request()
        DB->>DB: status → accepted, request_repair_departments, history
        DB-->>App: OK
    else Отклонить
        D->>App: reject_request(comment)
        App->>DB: RPC reject_request()
        DB->>DB: status → rejected, history
    else Отмена заявителем (UC-R3)
        R->>App: Отменить
        App->>DB: UPDATE cancelled + history
    end
    end

    rect rgb(240, 255, 240)
    Note over D,DB:  Назначение (status = accepted)
    opt assign_request_technician
        D->>App: Назначить техника отдела
        App->>DB: RPC assign_request_technician()
    end
    opt update_request_repair_zone
        D->>App: Сменить зону ремонта
        App->>DB: RPC update_request_repair_zone()
    end
    opt transfer / add_request_department
        D->>App: Передать или подключить отдел
        App->>DB: RPC transfer / add
    end
    end

    rect rgb(248, 240, 255)
    Note over D,DB:  Запуск работ
    D->>App: start_request_work()
    App->>DB: RPC start_request_work()
    DB->>DB: status → in_progress, history
    DB->>DB: trigger: assets → maintenance
    DB-->>App: OK
    end

    rect rgb(255, 255, 240)
    Note over D,DB:  Отчёты по отделам
    loop Каждый отдел маршрута
        D->>App: work_report
        App->>DB: INSERT work_reports
    end
    DB->>DB: trigger: все отделы отчитались → completed
    DB-->>App: Realtime
    end

    rect rgb(240, 255, 255)
    Note over R,DB:  Закрытие
    alt Заявитель
        R->>App: Приёмка работ
        App->>DB: UPDATE closed + history
    else Диспетчер / admin
        D->>App: close_request_as_staff()
        App->>DB: RPC close_request_as_staff()
        DB->>DB: status → closed, history
    end
    DB->>DB: trigger: assets → active
    App-->>R: Заявка закрыта
    end
```

---

## Диаграмма последовательности (часть 1 — до запуска работ)

Создание, приёмка и назначение (статус `accepted`).

```mermaid
sequenceDiagram
    autonumber
    actor R as Заявитель
    actor D as Диспетчер
    participant App as Приложение (CMMS)
    participant DB as PostgreSQL (Supabase)

    rect rgb(240, 248, 255)
    Note over R,DB:  Создание
    R->>App: Форма новой заявки
    App->>DB: RPC create_request()
    DB->>DB: INSERT requests, status = new
    DB-->>App: request_id
    App-->>R: Заявка создана
    end

    rect rgb(255, 248, 240)
    Note over D,DB:  Приёмка / отклонение / отмена
    D->>App: Входящие (status = new)
    alt Принять
        D->>App: accept_request(assignee?)
        App->>DB: RPC accept_request()
        DB->>DB: status → accepted, request_repair_departments, history
        DB-->>App: OK
    else Отклонить
        D->>App: reject_request(comment)
        App->>DB: RPC reject_request()
        DB->>DB: status → rejected, history
    else Отмена заявителем (UC-R3)
        R->>App: Отменить
        App->>DB: UPDATE cancelled + history
    end
    end

    rect rgb(240, 255, 240)
    Note over D,DB:  Назначение (status = accepted)
    opt assign_request_technician
        D->>App: Назначить техника отдела
        App->>DB: RPC assign_request_technician()
    end
    opt update_request_repair_zone
        D->>App: Сменить зону ремонта
        App->>DB: RPC update_request_repair_zone()
    end
    opt transfer / add_request_department
        D->>App: Передать или подключить отдел
        App->>DB: RPC transfer / add
    end
    end
```

---

## Диаграмма последовательности (часть 2 — запуск работ и закрытие)

Продолжение после назначения (статус `accepted` → … → `closed`).

```mermaid
sequenceDiagram
    autonumber
    actor R as Заявитель
    actor D as Диспетчер
    participant App as Приложение (CMMS)
    participant DB as PostgreSQL (Supabase)

    rect rgb(248, 240, 255)
    Note over D,DB:  Запуск работ
    D->>App: start_request_work()
    App->>DB: RPC start_request_work()
    DB->>DB: status → in_progress, history
    DB->>DB: trigger: assets → maintenance
    DB-->>App: OK
    end

    rect rgb(255, 255, 240)
    Note over D,DB:  Отчёты по отделам
    loop Каждый отдел маршрута
        D->>App: work_report
        App->>DB: INSERT work_reports
    end
    DB->>DB: trigger: все отделы отчитались → completed
    DB-->>App: Realtime
    end

    rect rgb(240, 255, 255)
    Note over R,DB:  Закрытие
    alt Заявитель
        R->>App: Приёмка работ
        App->>DB: UPDATE closed + history
    else Диспетчер / admin
        D->>App: close_request_as_staff()
        App->>DB: RPC close_request_as_staff()
        DB->>DB: status → closed, history
    end
    DB->>DB: trigger: assets → active
    App-->>R: Заявка закрыта
    end
```

---

## Участники и операции

| Участник | Действия |
| --- | --- |
| **Заявитель** | `create_request`, отмена (`cancelled`), приёмка (`closed`) |
| **Диспетчер** | `accept_request` / `reject_request`, назначение и маршрутизация, `start_request_work`, `work_reports`, `close_request_as_staff` |
| **CMMS** | WinUI-страницы + `IRequestService` → PostgREST / RPC |
| **PostgreSQL** | Таблицы `requests`, `request_repair_departments`, `request_status_history`, `work_reports`; триггеры завершения и статуса оборудования |

## Переходы статуса

| Из | В | Инициатор | Механизм |
| --- | --- | --- | --- |
| — | `new` | Заявитель | RPC `create_request` |
| `new` | `accepted` | Диспетчер | RPC `accept_request` |
| `new` | `rejected` | Диспетчер | RPC `reject_request` |
| `new` / `accepted` / `in_progress` | `cancelled` | Заявитель | PATCH + history |
| `accepted` | `in_progress` | Диспетчер | RPC `start_request_work` |
| `in_progress` | `completed` | Система | Триггер после отчётов всех отделов |
| `completed` | `closed` | Заявитель или staff | PATCH или RPC `close_request_as_staff` |

PlantUML: [`request-lifecycle.puml`](request-lifecycle.puml) · Statechart: [`request-lifecycle-statechart.puml`](request-lifecycle-statechart.puml)
