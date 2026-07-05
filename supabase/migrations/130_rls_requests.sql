-- =============================================================================
-- 130_rls_requests.sql — RLS: заявки, маршрутизация, история, отчёты
-- =============================================================================
-- -----------------------------------------------------------------------------
-- requests
-- requester — только свои.
-- dispatcher — направленные в его отдел + свои как заявитель.
-- admin — все.
-- -----------------------------------------------------------------------------

alter table public.requests enable row level security;

create policy requests_select_requester
on public.requests
for select
to authenticated
using (
  requester_id = auth.uid()
  and public.current_profile_role() = 'requester'
);

create policy requests_select_admin
on public.requests
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy requests_select_dispatcher
on public.requests
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and (
    (status = 'new' and (
      target_repair_department_id is null
      or target_repair_department_id = public.current_profile_repair_department_id()
    ))
    or requester_id = auth.uid()
    or public.request_is_routed_to_department(
      id,
      public.current_profile_repair_department_id()
    )
  )
);

create policy requests_insert_requester
on public.requests
for insert
to authenticated
with check (
  requester_id = auth.uid()
  and public.current_profile_role() = 'requester'
  and target_repair_department_id is not null
  and exists (
    select 1 from public.repair_departments rd
    where rd.id = target_repair_department_id and rd.is_active = true
  )
  and (
    asset_id is null
    or exists (
      select 1
      from public.assets a
      where a.id = asset_id
        and public.requester_can_access_location(a.location_id)
    )
  )
);

create policy requests_insert_admin
on public.requests
for insert
to authenticated
with check (
  requester_id = auth.uid()
  and public.current_profile_role() = 'admin'
);

create policy requests_insert_dispatcher
on public.requests
for insert
to authenticated
with check (
  requester_id = auth.uid()
  and public.current_profile_role() = 'dispatcher'
  and (
    asset_id is null
    or exists (
      select 1
      from public.assets a
      where a.id = asset_id
        and public.requester_can_access_location(a.location_id)
    )
  )
);

create policy requests_update_admin
on public.requests
for update
to authenticated
using (public.current_profile_role() = 'admin')
with check (public.current_profile_role() = 'admin');

create policy requests_update_dispatcher
on public.requests
for update
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and public.request_is_routed_to_department(
    id,
    public.current_profile_repair_department_id()
  )
)
with check (
  public.current_profile_role() = 'dispatcher'
  and public.request_is_routed_to_department(
    id,
    public.current_profile_repair_department_id()
  )
);

create policy requests_update_own
on public.requests
for update
to authenticated
using (
  requester_id = auth.uid()
  and public.current_profile_role() in ('requester', 'dispatcher')
)
with check (
  requester_id = auth.uid()
  and public.current_profile_role() in ('requester', 'dispatcher')
);

-- -----------------------------------------------------------------------------
-- request_repair_departments
-- -----------------------------------------------------------------------------

alter table public.request_repair_departments enable row level security;

create policy request_repair_depts_select_admin
on public.request_repair_departments
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy request_repair_depts_select_dispatcher
on public.request_repair_departments
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and public.request_is_routed_to_department(
    request_id,
    public.current_profile_repair_department_id()
  )
);

create policy request_repair_depts_select_requester
on public.request_repair_departments
for select
to authenticated
using (
  public.current_profile_role() = 'requester'
  and public.request_is_owned_by(request_id)
);

create policy request_repair_depts_insert_staff
on public.request_repair_departments
for insert
to authenticated
with check (public.current_profile_role() in ('admin', 'dispatcher'));

create policy request_repair_depts_update_dispatcher
on public.request_repair_departments
for update
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
)
with check (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);

create policy request_repair_depts_delete_admin
on public.request_repair_departments
for delete
to authenticated
using (public.current_profile_role() = 'admin');

create policy request_repair_depts_delete_dispatcher
on public.request_repair_departments
for delete
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);

-- -----------------------------------------------------------------------------
-- request_status_history
-- -----------------------------------------------------------------------------

alter table public.request_status_history enable row level security;

create policy request_status_history_select
on public.request_status_history
for select
to authenticated
using (public.request_visible_to_current_user(request_id));

create policy request_status_history_insert_staff
on public.request_status_history
for insert
to authenticated
with check (public.current_profile_role() in ('admin', 'dispatcher'));

create policy request_status_history_insert_own
on public.request_status_history
for insert
to authenticated
with check (
  public.current_profile_role() in ('requester', 'dispatcher')
  and public.request_is_owned_by(request_id)
);

-- -----------------------------------------------------------------------------
-- work_reports
-- dispatcher — только отчёты своего отдела.
-- admin — все.
-- -----------------------------------------------------------------------------

alter table public.work_reports enable row level security;

create policy work_reports_select_admin
on public.work_reports
for select
to authenticated
using (public.current_profile_role() = 'admin');

create policy work_reports_select_dispatcher
on public.work_reports
for select
to authenticated
using (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);

create policy work_reports_insert_admin
on public.work_reports
for insert
to authenticated
with check (public.current_profile_role() = 'admin');

create policy work_reports_insert_dispatcher
on public.work_reports
for insert
to authenticated
with check (
  public.current_profile_role() = 'dispatcher'
  and repair_department_id = public.current_profile_repair_department_id()
);
