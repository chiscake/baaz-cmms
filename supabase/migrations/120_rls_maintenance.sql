-- =============================================================================
-- 120_rls_maintenance.sql — RLS: нормативы ТО и график ППР
-- =============================================================================
-- -----------------------------------------------------------------------------
-- maintenance_norms
-- -----------------------------------------------------------------------------

alter table public.maintenance_norms enable row level security;

create policy maintenance_norms_select_staff
on public.maintenance_norms
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy maintenance_norms_insert_admin
on public.maintenance_norms
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy maintenance_norms_update_admin
on public.maintenance_norms
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy maintenance_norms_delete_admin
on public.maintenance_norms
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- maintenance_norms_departments
-- -----------------------------------------------------------------------------

alter table public.maintenance_norms_departments enable row level security;

create policy maintenance_norms_depts_select_staff
on public.maintenance_norms_departments
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy maintenance_norms_depts_manage_admin
on public.maintenance_norms_departments
for all
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- category_maintenance_norms / category_maintenance_norms_departments (UC-A5)
-- -----------------------------------------------------------------------------

alter table public.category_maintenance_norms enable row level security;

create policy category_maintenance_norms_select_staff
on public.category_maintenance_norms
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy category_maintenance_norms_manage_admin
on public.category_maintenance_norms
for all
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

alter table public.category_maintenance_norms_departments enable row level security;

create policy category_maintenance_norms_depts_select_staff
on public.category_maintenance_norms_departments
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy category_maintenance_norms_depts_manage_admin
on public.category_maintenance_norms_departments
for all
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- maintenance_schedule
-- Dispatcher видит только позиции, в которых назначен его отдел.
-- Admin видит все.
-- Создание позиций через RPC create_schedule_entry (security definer).
-- UPDATE status — admin и dispatcher своего отдела (fallback ручного завершения).
-- -----------------------------------------------------------------------------

alter table public.maintenance_schedule enable row level security;

create policy maintenance_schedule_select_admin
on public.maintenance_schedule
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy maintenance_schedule_select_dispatcher
on public.maintenance_schedule
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and exists (
    select 1
    from public.maintenance_schedule_departments msd
    where msd.schedule_id = id
      and msd.repair_department_id = public.current_profile_repair_department_id()
  )
);

create policy maintenance_schedule_insert_admin
on public.maintenance_schedule
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy maintenance_schedule_update_admin
on public.maintenance_schedule
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

-- Dispatcher может вручную менять status своей позиции (fallback для ППР без триггера)
create policy maintenance_schedule_update_dispatcher
on public.maintenance_schedule
for update
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and exists (
    select 1
    from public.maintenance_schedule_departments msd
    where msd.schedule_id = id
      and msd.repair_department_id = public.current_profile_repair_department_id()
  )
)
with check (
  public.current_profile_role() = 'dispatcher'
  and exists (
    select 1
    from public.maintenance_schedule_departments msd
    where msd.schedule_id = id
      and msd.repair_department_id = public.current_profile_repair_department_id()
  )
);

create policy maintenance_schedule_delete_admin
on public.maintenance_schedule
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- maintenance_schedule_departments
-- -----------------------------------------------------------------------------

alter table public.maintenance_schedule_departments enable row level security;

create policy maintenance_schedule_depts_select_admin
on public.maintenance_schedule_departments
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy maintenance_schedule_depts_select_dispatcher
on public.maintenance_schedule_departments
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);

create policy maintenance_schedule_depts_manage_admin
on public.maintenance_schedule_departments
for all
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');
