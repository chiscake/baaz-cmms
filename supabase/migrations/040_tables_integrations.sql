-- =============================================================================
-- 040_tables_integrations.sql — Интеграция TMS: enum и таблица ссылок на заявки инструмента
-- =============================================================================
create type public.tms_work_order_kind as enum ('request', 'schedule');

create table public.tms_tool_requisition_links (
  id uuid primary key default gen_random_uuid(),
  client_reference_id uuid not null unique,
  tms_requisition_id uuid not null unique,
  warehouse_id uuid not null,
  warehouse_name text,
  work_order_kind public.tms_work_order_kind not null,
  cmms_request_id uuid references public.requests(id) on delete cascade,
  cmms_schedule_id uuid references public.maintenance_schedule(id) on delete cascade,
  last_known_status text not null default 'new',
  last_synced_at timestamptz,
  sync_etag text,
  notes text,
  created_by uuid not null references public.profiles(id) on delete restrict,
  created_at timestamptz default now(),
  updated_at timestamptz default now(),
  check (
    (work_order_kind = 'request' and cmms_request_id is not null and cmms_schedule_id is null)
    or (work_order_kind = 'schedule' and cmms_schedule_id is not null and cmms_request_id is null)
  )
);

create index tms_tool_requisition_links_request_idx
  on public.tms_tool_requisition_links (cmms_request_id)
  where cmms_request_id is not null;

create index tms_tool_requisition_links_schedule_idx
  on public.tms_tool_requisition_links (cmms_schedule_id)
  where cmms_schedule_id is not null;

-- Одна активная заявка TMS на наряд + склад; отменённые и возвращённые не блокируют повтор.
create unique index tms_tool_requisition_links_request_warehouse_active_uidx
  on public.tms_tool_requisition_links (cmms_request_id, warehouse_id)
  where cmms_request_id is not null
    and last_known_status not in ('cancelled', 'returned');

create unique index tms_tool_requisition_links_schedule_warehouse_active_uidx
  on public.tms_tool_requisition_links (cmms_schedule_id, warehouse_id)
  where cmms_schedule_id is not null
    and last_known_status not in ('cancelled', 'returned');
