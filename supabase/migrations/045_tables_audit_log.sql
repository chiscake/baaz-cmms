-- =============================================================================
-- 045_tables_audit_log.sql — Универсальный журнал изменений БД (admin-only read)
-- =============================================================================

create type public.audit_operation as enum ('INSERT', 'UPDATE', 'DELETE');

create table public.audit_log (
  id uuid primary key default gen_random_uuid(),
  table_name text not null,
  record_id uuid,
  record_key text not null,
  operation public.audit_operation not null,
  changed_by uuid references public.profiles(id) on delete set null,
  changed_at timestamptz not null default now(),
  old_data jsonb,
  new_data jsonb
);

create index audit_log_changed_at_idx on public.audit_log (changed_at desc);
create index audit_log_table_name_idx on public.audit_log (table_name);
create index audit_log_changed_by_idx on public.audit_log (changed_by);
