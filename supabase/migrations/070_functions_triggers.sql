-- =============================================================================
-- 070_functions_triggers.sql — Тела триггерных функций (timestamps, завершение, валидация)
-- =============================================================================
create or replace function public.set_row_timestamps()
returns trigger
language plpgsql
as $$
begin
  if tg_op = 'INSERT' then
    new.created_at := coalesce(new.created_at, now());
    new.updated_at := coalesce(new.updated_at, now());
  elsif tg_op = 'UPDATE' then
    new.created_at := coalesce(old.created_at, new.created_at, now());
    new.updated_at := now();
  end if;
  return new;
end;
$$;

create or replace function public.check_schedule_completion()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_assigned integer;
  v_reported integer;
begin
  if new.schedule_id is null then
    return new;
  end if;

  select count(*) into v_assigned
  from public.maintenance_schedule_departments
  where schedule_id = new.schedule_id;

  -- Нет назначенных отделов — не трогаем статус (требуется ручная установка).
  if v_assigned = 0 then
    return new;
  end if;

  select count(distinct repair_department_id) into v_reported
  from public.work_reports
  where schedule_id = new.schedule_id;

  if v_reported >= v_assigned then
    update public.maintenance_schedule
    set status = 'completed', updated_at = now()
    where id = new.schedule_id
      and status <> 'completed';
  end if;

  return new;
end;
$$;

create or replace function public.sync_asset_status_from_request()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.asset_id is null then
    return new;
  end if;

  if new.status = 'in_progress'::public.request_status
     and old.status is distinct from new.status then
    update public.assets
    set status = 'maintenance'::public.asset_status,
        updated_at = now()
    where id = new.asset_id
      and status <> 'decommissioned'::public.asset_status;
  elsif new.status = 'closed'::public.request_status
        and old.status is distinct from new.status then
    update public.assets
    set status = 'active'::public.asset_status,
        updated_at = now()
    where id = new.asset_id
      and status <> 'decommissioned'::public.asset_status;
  end if;

  return new;
end;
$$;

create or replace function public.validate_work_report_department()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.request_id is not null then
    if not exists (
      select 1
      from public.request_repair_departments rrd
      where rrd.request_id = new.request_id
        and rrd.repair_department_id = new.repair_department_id
    ) then
      raise exception using errcode = 'P0001', message = 'WORK_REPORT_DEPARTMENT_NOT_ROUTED';
    end if;
  end if;

  if new.schedule_id is not null then
    if not exists (
      select 1
      from public.maintenance_schedule_departments msd
      where msd.schedule_id = new.schedule_id
        and msd.repair_department_id = new.repair_department_id
    ) then
      raise exception using errcode = 'P0001', message = 'WORK_REPORT_DEPARTMENT_NOT_ROUTED';
    end if;
  end if;

  return new;
end;
$$;

create or replace function public.check_request_completion()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if new.request_id is null then
    return new;
  end if;

  select status into v_status
  from public.requests
  where id = new.request_id;

  if v_status = 'in_progress'
    and exists (
      select 1 from public.request_repair_departments rrd
      where rrd.request_id = new.request_id
    )
    and not exists (
      select 1 from public.request_repair_departments rrd
      where rrd.request_id = new.request_id
        and not exists (
          select 1 from public.work_reports wr
          where wr.request_id = new.request_id
            and wr.repair_department_id = rrd.repair_department_id
        )
    )
  then
    update public.requests
    set status = 'completed', updated_at = now()
    where id = new.request_id
      and status = 'in_progress';

    insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
    values (new.request_id, new.author_id, 'in_progress', 'completed', 'Автоматически: все отделы сдали отчёты');
  end if;

  return new;
end;
$$;

create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = public, auth
as $$
begin
  insert into public.profiles (id, role, full_name)
  values (
    new.id,
    'requester'::public.user_role,
    coalesce(new.raw_user_meta_data ->> 'full_name', null)
  )
  on conflict (id) do nothing;

  return new;
end;
$$;
