-- =============================================================================
-- 160_integration_downtime_tracker.sql — Read-only API для DowntimeTracker (DT)
-- Publishable key (anon) + GRANT SELECT на view в схеме integration.
-- View без security_invoker: владелец postgres читает public.*; anon видит только проекцию.
-- =============================================================================

create schema integration;

revoke all on schema integration from public;
grant usage on schema integration to anon;

-- -----------------------------------------------------------------------------
-- v_assets_lookup — поиск станка по инвентарному номеру
-- -----------------------------------------------------------------------------

create or replace view integration.v_assets_lookup as
select
  a.id as asset_id,
  a.asset_number,
  a.name,
  l.code as location_code,
  a.status::text as status,
  a.manufacturer,
  a.model,
  a.serial_number,
  a.commissioning_date
from public.assets a
inner join public.locations l on l.id = a.location_id
where a.status <> 'decommissioned';

-- -----------------------------------------------------------------------------
-- v_requests_by_asset — заявки по станку
-- -----------------------------------------------------------------------------

create or replace view integration.v_requests_by_asset as
select
  r.asset_id,
  a.asset_number,
  r.id as request_id,
  r.request_number,
  r.type::text as type,
  r.status::text as status,
  r.title,
  coalesce(r.description, '') as description,
  r.priority::text as priority,
  r.repair_zone::text as repair_zone,
  coalesce(r.contractor_name, '') as contractor_name,
  coalesce(p.full_name, '') as requester_name,
  r.created_at,
  r.updated_at
from public.requests r
inner join public.assets a on a.id = r.asset_id
left join public.profiles p on p.id = r.requester_id
where r.asset_id is not null;

-- -----------------------------------------------------------------------------
-- v_work_reports_by_asset — отчёты о работах по станку
-- -----------------------------------------------------------------------------

create or replace view integration.v_work_reports_by_asset as
select
  coalesce(r.asset_id, ms.asset_id) as asset_id,
  wr.request_id,
  coalesce(r.request_number, '') as request_number,
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
left join public.requests r on r.id = wr.request_id
left join public.maintenance_schedule ms on ms.id = wr.schedule_id
inner join public.repair_departments rd on rd.id = wr.repair_department_id
inner join public.technicians t on t.id = wr.technician_id
where coalesce(r.asset_id, ms.asset_id) is not null;

-- -----------------------------------------------------------------------------
-- v_request_departments — отделы заявки (маршрутизация)
-- -----------------------------------------------------------------------------

create or replace view integration.v_request_departments as
select
  rrd.request_id,
  rd.name as repair_department_name,
  coalesce(t.full_name, '') as assignee_name,
  exists (
    select 1
    from public.work_reports wr
    where wr.request_id = rrd.request_id
      and wr.repair_department_id = rrd.repair_department_id
  ) as has_work_report
from public.request_repair_departments rrd
inner join public.repair_departments rd on rd.id = rrd.repair_department_id
left join public.technicians t on t.id = rrd.assignee_id;

-- -----------------------------------------------------------------------------
-- Права: только SELECT для anon (publishable key)
-- -----------------------------------------------------------------------------

grant select on integration.v_assets_lookup to anon;
grant select on integration.v_requests_by_asset to anon;
grant select on integration.v_work_reports_by_asset to anon;
grant select on integration.v_request_departments to anon;
