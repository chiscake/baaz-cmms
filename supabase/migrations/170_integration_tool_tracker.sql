-- =============================================================================
-- 170_integration_tool_tracker.sql — Read/write API для Tool Tracker (TMS)
-- Контур А: REP-API-2/3 (read views), idempotency table; REP-API-1 — Edge Function.
-- Схема integration уже создана в 160_integration_downtime_tracker.sql.
-- =============================================================================

grant usage on schema integration to authenticated, service_role;

-- -----------------------------------------------------------------------------
-- Idempotency REP-API-1 (client_reference_id → request_id)
-- -----------------------------------------------------------------------------

create table integration.tms_repair_client_refs (
  client_reference_id uuid primary key,
  request_id uuid references public.requests(id) on delete set null,
  created_at timestamptz not null default now()
);

create index tms_repair_client_refs_request_id_idx
  on integration.tms_repair_client_refs (request_id)
  where request_id is not null;

grant select, insert, update on integration.tms_repair_client_refs to service_role;

-- -----------------------------------------------------------------------------
-- v_inventory_requests — заявки на ремонт/поверку складского объекта (контур А)
-- -----------------------------------------------------------------------------

create or replace view integration.v_inventory_requests as
select
  r.id as request_id,
  r.request_number,
  r.inventory_id,
  r.inventory_kind::text as inventory_kind,
  coalesce(r.inventory_name, '') as inventory_name,
  coalesce(r.inventory_serial, '') as inventory_serial,
  coalesce(r.inventory_type_name, '') as inventory_type_name,
  r.inventory_source::text as inventory_source,
  r.type::text as type,
  r.status::text as status,
  r.title,
  coalesce(r.description, '') as description,
  r.priority::text as priority,
  r.repair_zone::text as repair_zone,
  coalesce(r.contractor_name, '') as contractor_name,
  coalesce(p.full_name, '') as requester_name,
  r.inventory_handoff_mode::text as inventory_handoff_mode,
  coalesce(r.inventory_warehouse_name, '') as inventory_warehouse_name,
  r.inventory_received_at,
  r.location_description,
  r.created_at,
  r.updated_at
from public.requests r
left join public.profiles p on p.id = r.requester_id
where r.inventory_id is not null;

-- -----------------------------------------------------------------------------
-- v_inventory_work_reports — отчёты по inventory-заявкам (REP-API-3)
-- -----------------------------------------------------------------------------

create or replace view integration.v_inventory_work_reports as
select
  r.inventory_id,
  r.inventory_kind::text as inventory_kind,
  wr.request_id,
  r.request_number,
  wr.id as work_report_id,
  wr.work_performed,
  wr.actual_duration_hours,
  wr.maintenance_type::text as maintenance_type,
  rd.name as repair_department_name,
  t.full_name as technician_full_name,
  coalesce(wr.defects_found, '') as defects_found,
  coalesce(wr.notes, '') as notes,
  wr.created_at
from public.work_reports wr
inner join public.requests r on r.id = wr.request_id
inner join public.repair_departments rd on rd.id = wr.repair_department_id
inner join public.technicians t on t.id = wr.technician_id
where r.inventory_id is not null;

-- -----------------------------------------------------------------------------
-- v_active_task_for_technician — UC-TT1 stub (заявки in_progress с assignee)
-- -----------------------------------------------------------------------------

create or replace view integration.v_active_task_for_technician as
select
  rrd.assignee_id as technician_id,
  r.id as request_id,
  null::uuid as schedule_id,
  r.request_number as work_order_number,
  r.status::text as status,
  'request'::text as work_order_kind,
  r.inventory_id,
  r.inventory_kind::text as inventory_kind,
  r.asset_id
from public.request_repair_departments rrd
inner join public.requests r on r.id = rrd.request_id
where rrd.assignee_id is not null
  and r.status = 'in_progress';

-- -----------------------------------------------------------------------------
-- Права: SELECT на views для anon/authenticated (publishable key + JWT)
-- -----------------------------------------------------------------------------

grant select on integration.v_inventory_requests to anon, authenticated;
grant select on integration.v_inventory_work_reports to anon, authenticated;
grant select on integration.v_active_task_for_technician to anon, authenticated;

-- -----------------------------------------------------------------------------
-- v_repair_departments — справочник отделов для формы «Отправить в ТОиР» (TMS)
-- -----------------------------------------------------------------------------

create or replace view integration.v_repair_departments as
select
  id as repair_department_id,
  name,
  code
from public.repair_departments
where is_active = true;

grant select on integration.v_repair_departments to anon, authenticated;
