-- =============================================================================
-- 140_rls_integrations.sql — RLS: ссылки на заявки TMS
-- =============================================================================
-- -----------------------------------------------------------------------------
-- tms_tool_requisition_links — локальные ссылки на заявки TMS (UC-D8)
-- -----------------------------------------------------------------------------

alter table public.tms_tool_requisition_links enable row level security;

create policy tms_tool_requisition_links_select
on public.tms_tool_requisition_links
for select
to authenticated
using (
  public.current_profile_role() = 'admin'
  or (
    cmms_request_id is not null
    and public.request_visible_to_current_user(cmms_request_id)
  )
  or (
    cmms_schedule_id is not null
    and public.current_profile_role() = 'dispatcher'
    and exists (
      select 1
      from public.maintenance_schedule_departments msd
      where msd.schedule_id = cmms_schedule_id
        and msd.repair_department_id = public.current_profile_repair_department_id()
    )
  )
);

create policy tms_tool_requisition_links_insert_staff
on public.tms_tool_requisition_links
for insert
to authenticated
with check (
  public.current_profile_role() in ('admin', 'dispatcher')
  and created_by = auth.uid()
);

create policy tms_tool_requisition_links_update_staff
on public.tms_tool_requisition_links
for update
to authenticated
using (public.current_profile_role() in ('admin', 'dispatcher'))
with check (public.current_profile_role() in ('admin', 'dispatcher'));
