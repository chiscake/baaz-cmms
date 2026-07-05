-- =============================================================================
-- 110_rls_catalog.sql — RLS: локации, отделы, профили, техники, оборудование
-- =============================================================================
-- -----------------------------------------------------------------------------
-- locations — читают все аутентифицированные, управляет admin
-- -----------------------------------------------------------------------------

alter table public.locations enable row level security;

create policy locations_select_authenticated
on public.locations
for select
to authenticated
using (true);

create policy locations_insert_admin
on public.locations
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy locations_update_admin
on public.locations
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy locations_delete_admin
on public.locations
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- repair_departments — читают все staff, управляет admin
-- -----------------------------------------------------------------------------

alter table public.repair_departments enable row level security;

create policy repair_departments_select_staff
on public.repair_departments
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy repair_departments_select_requester
on public.repair_departments
for select
to authenticated
using (
  public.current_profile_role() = 'requester'
  and is_active = true
);

create policy repair_departments_insert_admin
on public.repair_departments
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy repair_departments_update_admin
on public.repair_departments
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy repair_departments_delete_admin
on public.repair_departments
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- profiles
-- Свой профиль — каждый.
-- Staff (admin/dispatcher) — видят все профили (нужно для отображения имён
-- в work_reports.author_id и т.п.)
-- Обновление — только admin.
-- -----------------------------------------------------------------------------

alter table public.profiles enable row level security;

create policy profiles_select_own
on public.profiles
for select
to authenticated
using (auth.uid() = id);

create policy profiles_select_staff
on public.profiles
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy profiles_update_admin
on public.profiles
for update
to authenticated
using (
  public.current_profile_role() = 'admin'
  and (role <> 'admin' or id = auth.uid())
)
with check (
  public.current_profile_role() = 'admin'
  and (role <> 'admin' or id = auth.uid())
);

-- -----------------------------------------------------------------------------
-- profile_location_scopes — свои зоны + admin управляет всеми
-- -----------------------------------------------------------------------------

alter table public.profile_location_scopes enable row level security;

create policy profile_location_scopes_select_own
on public.profile_location_scopes
for select
to authenticated
using (profile_id = auth.uid());

create policy profile_location_scopes_select_admin
on public.profile_location_scopes
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy profile_location_scopes_insert_admin
on public.profile_location_scopes
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy profile_location_scopes_update_admin
on public.profile_location_scopes
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy profile_location_scopes_delete_admin
on public.profile_location_scopes
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- technicians
-- Dispatcher видит только technicians своего отдела (для назначения исполнителей).
-- Admin видит всех.
-- Управление — admin и dispatcher своего отдела.
-- -----------------------------------------------------------------------------

alter table public.technicians enable row level security;

create policy technicians_select_admin
on public.technicians
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy technicians_select_dispatcher
on public.technicians
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);

create policy technicians_insert_staff
on public.technicians
for insert
to authenticated
with check (
  public.current_profile_role() = 'admin'
  or (
    public.current_profile_role() = 'dispatcher'
    and repair_department_id = public.current_profile_repair_department_id()
  )
);

create policy technicians_update_staff
on public.technicians
for update
to authenticated
using (
  public.current_profile_role() = 'admin'
  or (
    public.current_profile_role() = 'dispatcher'
    and repair_department_id = public.current_profile_repair_department_id()
  )
)
with check (
  public.current_profile_role() = 'admin'
  or (
    public.current_profile_role() = 'dispatcher'
    and repair_department_id = public.current_profile_repair_department_id()
  )
);

create policy technicians_delete_admin
on public.technicians
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- equipment_categories (UC-A5) — читают admin/dispatcher, управляет admin
-- -----------------------------------------------------------------------------

alter table public.equipment_categories enable row level security;

create policy equipment_categories_select_staff
on public.equipment_categories
for select
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'));

create policy equipment_categories_insert_admin
on public.equipment_categories
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy equipment_categories_update_admin
on public.equipment_categories
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy equipment_categories_delete_admin
on public.equipment_categories
for delete
to authenticated
using (public.current_profile_role() = 'admin');

-- -----------------------------------------------------------------------------
-- assets — читают все staff и requester (нужно для создания заявки)
-- Управление — admin
-- -----------------------------------------------------------------------------

alter table public.assets enable row level security;

create policy assets_select_admin
on public.assets
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy assets_select_dispatcher
on public.assets
for select
to authenticated
using (public.current_profile_role() = 'dispatcher');

create policy assets_select_requester
on public.assets
for select
to authenticated
using (
  public.current_profile_role() = 'requester'
  and public.requester_can_access_location(location_id)
);

create policy assets_insert_admin
on public.assets
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy assets_update_admin
on public.assets
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy assets_delete_admin
on public.assets
for delete
to authenticated
using (public.current_profile_role() = 'admin');
