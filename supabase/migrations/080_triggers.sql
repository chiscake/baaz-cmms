-- =============================================================================
-- 080_triggers.sql — Определения триггеров на таблицах public и auth.users
-- =============================================================================
create trigger locations_set_timestamps
before insert or update on public.locations
for each row execute function public.set_row_timestamps();

create trigger repair_departments_set_timestamps
before insert or update on public.repair_departments
for each row execute function public.set_row_timestamps();

create trigger profiles_set_timestamps
before insert or update on public.profiles
for each row execute function public.set_row_timestamps();

create trigger technicians_set_timestamps
before insert or update on public.technicians
for each row execute function public.set_row_timestamps();

create trigger assets_set_timestamps
before insert or update on public.assets
for each row execute function public.set_row_timestamps();

create trigger maintenance_norms_set_timestamps
before insert or update on public.maintenance_norms
for each row execute function public.set_row_timestamps();

create trigger equipment_categories_set_timestamps
before insert or update on public.equipment_categories
for each row execute function public.set_row_timestamps();

create trigger category_maintenance_norms_set_timestamps
before insert or update on public.category_maintenance_norms
for each row execute function public.set_row_timestamps();

create trigger maintenance_schedule_set_timestamps
before insert or update on public.maintenance_schedule
for each row execute function public.set_row_timestamps();

create trigger requests_set_timestamps
before insert or update on public.requests
for each row execute function public.set_row_timestamps();

create trigger tms_tool_requisition_links_set_timestamps
before insert or update on public.tms_tool_requisition_links
for each row execute function public.set_row_timestamps();

create trigger work_reports_check_schedule_completion
after insert on public.work_reports
for each row
execute function public.check_schedule_completion();

create trigger requests_sync_asset_status
after update of status on public.requests
for each row
execute function public.sync_asset_status_from_request();

create trigger work_reports_validate_department
before insert on public.work_reports
for each row
execute function public.validate_work_report_department();

create trigger work_reports_check_request_completion
after insert on public.work_reports
for each row
execute function public.check_request_completion();

create trigger on_auth_user_created
after insert on auth.users
for each row
execute function public.handle_new_user();
