-- =============================================================================
-- 085_triggers_audit_log.sql — AFTER I/U/D триггеры аудита на все public-таблицы
-- =============================================================================

do $$
declare
  t text;
begin
  foreach t in array array[
    'locations',
    'repair_departments',
    'profiles',
    'profile_location_scopes',
    'technicians',
    'equipment_categories',
    'assets',
    'maintenance_norms',
    'maintenance_norms_departments',
    'category_maintenance_norms',
    'category_maintenance_norms_departments',
    'maintenance_schedule',
    'maintenance_schedule_departments',
    'requests',
    'request_repair_departments',
    'request_status_history',
    'work_reports',
    'tms_tool_requisition_links'
  ]
  loop
    execute format(
      'drop trigger if exists trg_audit_%I on public.%I',
      t,
      t
    );
    execute format(
      'create trigger trg_audit_%I
       after insert or update or delete on public.%I
       for each row execute function public.audit_row_changes()',
      t,
      t
    );
  end loop;
end;
$$;
