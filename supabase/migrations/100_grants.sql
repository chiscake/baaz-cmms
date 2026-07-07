-- =============================================================================
-- 100_grants.sql — Права доступа (grants) для authenticated и service_role
-- =============================================================================
grant usage on schema public to authenticated, service_role;

grant select on table public.locations to authenticated, service_role;
grant insert, update, delete on table public.locations to authenticated, service_role;
grant all on table public.locations to service_role;

grant select on table public.repair_departments to authenticated, service_role;
grant insert, update, delete on table public.repair_departments to authenticated, service_role;
grant all on table public.repair_departments to service_role;

grant select, insert, update on table public.profiles to authenticated, service_role;
grant all on table public.profiles to service_role;

grant select on table public.profile_location_scopes to authenticated, service_role;
grant insert, update, delete on table public.profile_location_scopes to authenticated, service_role;
grant all on table public.profile_location_scopes to service_role;

grant select on table public.technicians to authenticated, service_role;
grant insert, update, delete on table public.technicians to authenticated, service_role;
grant all on table public.technicians to service_role;

grant select on table public.assets to authenticated, service_role;
grant insert, update, delete on table public.assets to authenticated, service_role;
grant all on table public.assets to service_role;

grant select on table public.equipment_categories to authenticated, service_role;
grant insert, update, delete on table public.equipment_categories to authenticated, service_role;
grant all on table public.equipment_categories to service_role;

grant select on table public.maintenance_norms to authenticated, service_role;
grant insert, update, delete on table public.maintenance_norms to authenticated, service_role;
grant all on table public.maintenance_norms to service_role;

grant select on table public.maintenance_norms_departments to authenticated, service_role;
grant insert, update, delete on table public.maintenance_norms_departments to authenticated, service_role;
grant all on table public.maintenance_norms_departments to service_role;

grant select on table public.category_maintenance_norms to authenticated, service_role;
grant insert, update, delete on table public.category_maintenance_norms to authenticated, service_role;
grant all on table public.category_maintenance_norms to service_role;

grant select on table public.category_maintenance_norms_departments to authenticated, service_role;
grant insert, update, delete on table public.category_maintenance_norms_departments to authenticated, service_role;
grant all on table public.category_maintenance_norms_departments to service_role;

grant select on table public.maintenance_schedule to authenticated, service_role;
grant insert, update, delete on table public.maintenance_schedule to authenticated, service_role;
grant all on table public.maintenance_schedule to service_role;

grant select on table public.maintenance_schedule_departments to authenticated, service_role;
grant insert, update, delete on table public.maintenance_schedule_departments to authenticated, service_role;
grant all on table public.maintenance_schedule_departments to service_role;

grant select on table public.requests to authenticated, service_role;
grant insert, update on table public.requests to authenticated, service_role;
grant all on table public.requests to service_role;

grant select on table public.request_repair_departments to authenticated, service_role;
grant insert, update, delete on table public.request_repair_departments to authenticated, service_role;
grant all on table public.request_repair_departments to service_role;

grant select on table public.request_status_history to authenticated, service_role;
grant insert on table public.request_status_history to authenticated, service_role;
grant all on table public.request_status_history to service_role;

grant select on table public.work_reports to authenticated, service_role;
grant insert on table public.work_reports to authenticated, service_role;
grant all on table public.work_reports to service_role;

grant select on table public.tms_tool_requisition_links to authenticated, service_role;
grant insert, update on table public.tms_tool_requisition_links to authenticated, service_role;
grant all on table public.tms_tool_requisition_links to service_role;

grant select on table public.audit_log to authenticated, service_role;
grant all on table public.audit_log to service_role;

grant select on public.effective_maintenance_norms to authenticated, service_role;
grant all on public.effective_maintenance_norms to service_role;

grant select on public.asset_maintenance_status to authenticated, service_role;
grant all on public.asset_maintenance_status to service_role;

grant execute on function public.current_profile_role() to authenticated, service_role;
grant execute on function public.current_profile_repair_department_id() to authenticated, service_role;
grant execute on function public.location_subtree_ids(uuid) to authenticated, service_role;
grant execute on function public.profile_scope_anchors(uuid) to authenticated, service_role;
grant execute on function public.profile_accessible_location_ids(uuid) to authenticated, service_role;
grant execute on function public.requester_can_access_location(uuid) to authenticated, service_role;
grant execute on function public.request_is_routed_to_department(uuid, uuid) to authenticated, service_role;
grant execute on function public.request_is_owned_by(uuid, uuid) to authenticated, service_role;
grant execute on function public.request_visible_to_current_user(uuid) to authenticated, service_role;
grant execute on function public.archive_location_branch(uuid) to authenticated, service_role;
grant execute on function public.restore_location_branch(uuid) to authenticated, service_role;
grant execute on function public.hard_delete_location_branch(uuid) to authenticated, service_role;
grant execute on function public.create_schedule_entry(uuid, public.maintenance_type, date, uuid[], boolean) to authenticated, service_role;
grant execute on function public.get_effective_norm_departments(uuid, public.maintenance_type) to authenticated, service_role;
grant execute on function public.sync_schedule_after_norm_change(uuid, public.maintenance_type, text) to authenticated, service_role;
grant execute on function public.has_pending_schedule(uuid, public.maintenance_type) to authenticated, service_role;
grant execute on function public.get_pending_schedule_entry(uuid, public.maintenance_type) to authenticated, service_role;
grant execute on function public.mark_overdue_schedule_items() to authenticated, service_role;
grant execute on function public.generate_ppr_schedule(int) to authenticated, service_role;
grant execute on function public.create_request(text, public.request_type, public.request_priority, text, text, text, uuid, uuid, public.repair_zone, text) to authenticated, service_role;
grant execute on function public.accept_request(uuid, uuid, text) to authenticated, service_role;
grant execute on function public.reject_request(uuid, text) to authenticated, service_role;
grant execute on function public.transfer_request_department(uuid, uuid, text) to authenticated, service_role;
grant execute on function public.add_request_department(uuid, uuid, uuid, text) to authenticated, service_role;
grant execute on function public.assign_request_technician(uuid, uuid, uuid) to authenticated, service_role;
grant execute on function public.update_request_repair_zone(uuid, public.repair_zone, text, text) to authenticated, service_role;
grant execute on function public.start_request_work(uuid, text) to authenticated, service_role;
grant execute on function public.confirm_inventory_received(uuid, text) to authenticated, service_role;
grant execute on function public.start_schedule_work(uuid, text) to authenticated, service_role;
grant execute on function public.close_request_as_staff(uuid, text) to authenticated, service_role;
