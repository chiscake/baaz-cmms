-- =============================================================================
-- 030_tables_requests.sql — Таблицы заявок, маршрутизации, истории и отчётов
-- =============================================================================
create table public.requests (
  id uuid primary key default gen_random_uuid(),
  request_number text not null unique,
  type public.request_type not null default 'breakdown',
  asset_id uuid references public.assets(id) on delete restrict,
  -- Контур А (TMS → CMMS): ссылка на экземпляр во внешней складской системе (MVP: tms_tools.id).
  inventory_id uuid,
  inventory_kind public.inventory_kind,
  inventory_name text,
  inventory_serial text,
  inventory_type_name text,
  inventory_source public.inventory_source not null default 'tms',
  inventory_handoff_mode public.inventory_handoff_mode,
  inventory_warehouse_name text,
  inventory_received_at timestamptz,
  location_description text not null,
  requester_id uuid not null references public.profiles(id) on delete restrict,
  title text not null,
  description text,
  priority public.request_priority default 'normal',
  repair_zone public.repair_zone not null default 'on_site',
  -- Свободный текст подрядчика — заполняется при repair_zone = 'external'.
  contractor_name text,
  -- Отдел, выбранный заявителем при создании (до accept_request).
  target_repair_department_id uuid references public.repair_departments(id) on delete restrict,
  status public.request_status default 'new',
  created_at timestamptz default now(),
  updated_at timestamptz default now(),
  check (
    (
      asset_id is not null
      and inventory_id is null
    )
    or (
      inventory_id is not null
      and inventory_kind is not null
      and asset_id is null
    )
    or (
      asset_id is null
      and inventory_id is null
      and nullif(trim(location_description), '') is not null
    )
  )
);

-- Одна незакрытая заявка на пару (inventory_id, inventory_kind) — идемпотентность REP-API-1.
create unique index requests_one_open_inventory
  on public.requests (inventory_id, inventory_kind)
  where inventory_id is not null
    and status not in ('closed', 'rejected', 'cancelled');

-- Маршрутизация заявки по ремонтным отделам (многие-ко-многим).
-- Диспетчер указывает куда направить при принятии заявки; каждая строка
-- хранит своего исполнителя (assignee_id) — заявка может вестись
-- несколькими отделами параллельно, у каждого свой техник.
create table public.request_repair_departments (
  request_id uuid not null references public.requests(id) on delete cascade,
  repair_department_id uuid not null references public.repair_departments(id) on delete restrict,
  assignee_id uuid references public.technicians(id) on delete set null,
  added_at timestamptz not null default now(),
  primary key (request_id, repair_department_id)
);

create index on public.request_repair_departments (repair_department_id);

-- -----------------------------------------------------------------------------
-- История статусов заявки
-- -----------------------------------------------------------------------------

create table public.request_status_history (
  id uuid primary key default gen_random_uuid(),
  request_id uuid not null references public.requests(id) on delete cascade,
  changed_by uuid not null references public.profiles(id) on delete restrict,
  old_status public.request_status not null,
  new_status public.request_status not null,
  comment text,
  created_at timestamptz default now()
);

-- -----------------------------------------------------------------------------
-- Отчёты о выполненных работах
-- -----------------------------------------------------------------------------

create table public.work_reports (
  id uuid primary key default gen_random_uuid(),
  request_id uuid references public.requests(id) on delete restrict,
  schedule_id uuid references public.maintenance_schedule(id) on delete restrict,
  -- Каждый отдел сдаёт свой отчёт в рамках одной заявки/позиции ППР.
  repair_department_id uuid not null references public.repair_departments(id) on delete restrict,
  maintenance_type public.maintenance_type,
  -- Несколько видов ТО в одном отчёте отдела (уникальный индекс — одна строка на отдел/заявку).
  maintenance_types public.maintenance_type[],
  author_id uuid not null references public.profiles(id) on delete restrict,
  technician_id uuid not null references public.technicians(id) on delete restrict,
  work_performed text not null,
  actual_duration_hours numeric not null,
  -- Свободный текст/json: перечень материалов и запчастей в отчёте (без отдельной схемы склада в CMMS).
  parts_used jsonb,
  defects_found text,
  notes text,
  created_at timestamptz default now(),
  check (request_id is not null or schedule_id is not null)
);

-- Один отдел — один отчёт на позицию ППР / одну заявку.
create unique index work_reports_schedule_dept_unique
  on public.work_reports (schedule_id, repair_department_id)
  where schedule_id is not null;

create unique index work_reports_request_dept_unique
  on public.work_reports (request_id, repair_department_id)
  where request_id is not null;

create index on public.work_reports (repair_department_id);
