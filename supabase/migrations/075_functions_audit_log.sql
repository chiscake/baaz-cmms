-- =============================================================================
-- 075_functions_audit_log.sql — Триггерная функция аудита изменений строк
-- =============================================================================

create or replace function public.audit_record_key(
  p_table_name text,
  p_new record,
  p_old record
)
returns text
language plpgsql
immutable
as $$
declare
  v_row record;
begin
  v_row := coalesce(p_new, p_old);

  case p_table_name
    when 'profile_location_scopes' then
      return format(
        'profile_id=%s,location_id=%s',
        (v_row).profile_id,
        (v_row).location_id
      );
    when 'maintenance_norms_departments' then
      return format(
        'norm_id=%s,repair_department_id=%s',
        (v_row).norm_id,
        (v_row).repair_department_id
      );
    when 'category_maintenance_norms_departments' then
      return format(
        'category_norm_id=%s,repair_department_id=%s',
        (v_row).category_norm_id,
        (v_row).repair_department_id
      );
    when 'maintenance_schedule_departments' then
      return format(
        'schedule_id=%s,repair_department_id=%s',
        (v_row).schedule_id,
        (v_row).repair_department_id
      );
    when 'request_repair_departments' then
      return format(
        'request_id=%s,repair_department_id=%s',
        (v_row).request_id,
        (v_row).repair_department_id
      );
    else
      return coalesce((v_row).id::text, 'unknown');
  end case;
end;
$$;

create or replace function public.audit_record_id(
  p_table_name text,
  p_new record,
  p_old record
)
returns uuid
language plpgsql
immutable
as $$
declare
  v_row record;
begin
  v_row := coalesce(p_new, p_old);

  case p_table_name
    when 'profile_location_scopes',
         'maintenance_norms_departments',
         'category_maintenance_norms_departments',
         'maintenance_schedule_departments',
         'request_repair_departments' then
      return null;
    else
      return (v_row).id;
  end case;
end;
$$;

create or replace function public.audit_row_changes()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_actor uuid;
  v_record_key text;
  v_record_id uuid;
begin
  v_actor := auth.uid();
  if v_actor is null then
    begin
      v_actor := nullif(current_setting('app.actor_id', true), '')::uuid;
    exception
      when others then
        v_actor := null;
    end;
  end if;

  v_record_key := public.audit_record_key(tg_table_name, new, old);
  v_record_id := public.audit_record_id(tg_table_name, new, old);

  insert into public.audit_log (
    table_name,
    record_id,
    record_key,
    operation,
    changed_by,
    old_data,
    new_data
  )
  values (
    tg_table_name,
    v_record_id,
    v_record_key,
    tg_op::public.audit_operation,
    v_actor,
    case when tg_op in ('UPDATE', 'DELETE') then to_jsonb(old) else null end,
    case when tg_op in ('INSERT', 'UPDATE') then to_jsonb(new) else null end
  );

  return coalesce(new, old);
end;
$$;
