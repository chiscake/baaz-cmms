-- =============================================================================
-- 000_extensions_and_enums.sql — Расширения и перечисления (enums) схемы public
-- =============================================================================

-- UC-D3/D5: ночная автогенерация графика ППР и пометка просроченных позиций.
create extension if not exists pg_cron with schema pg_catalog;

create type public.user_role as enum ('admin', 'dispatcher', 'requester');

create type public.asset_status as enum ('active', 'maintenance', 'decommissioned');
create type public.maintenance_type as enum ('to1', 'to2', 'kr');
create type public.schedule_status as enum ('scheduled', 'in_progress', 'completed', 'overdue', 'cancelled');
create type public.request_type as enum ('breakdown', 'service', 'inspection');
create type public.request_status as enum (
  'new',
  'accepted',
  'in_progress',
  'completed',
  'closed',
  'rejected',
  'cancelled'
);
create type public.request_priority as enum ('low', 'normal', 'high', 'critical');
create type public.repair_zone as enum ('on_site', 'workshop', 'external');

-- Контур А (TMS → CMMS): тип и источник внешнего складского объекта на заявке.
create type public.inventory_kind as enum ('tool');
create type public.inventory_source as enum ('tms');
create type public.inventory_handoff_mode as enum (
  'pickup_at_warehouse',
  'deliver_to_department'
);
