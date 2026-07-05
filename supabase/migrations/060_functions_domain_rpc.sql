-- =============================================================================
-- 060_functions_domain_rpc.sql — RPC: создание ППР и жизненный цикл заявок
-- =============================================================================
create or replace function public.create_schedule_entry(
  p_asset_id uuid,
  p_maintenance_type public.maintenance_type,
  p_planned_date date,
  p_department_ids uuid[] default null,
  p_notify_dispatchers boolean default null
)
returns uuid
language plpgsql
security definer
set search_path = public
as $$
declare
  v_schedule_id uuid;
  v_dept_id uuid;
  v_dept_ids uuid[];
  v_dispatcher_dept uuid;
  v_notify boolean;
begin
  if auth.uid() is not null
     and public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  if not exists (
    select 1 from public.assets
    where id = p_asset_id and status <> 'decommissioned'
  ) then
    raise exception using errcode = 'P0001', message = 'ASSET_NOT_AVAILABLE';
  end if;

  if public.has_pending_schedule(p_asset_id, p_maintenance_type) then
    raise exception using errcode = 'P0001', message = 'PENDING_SCHEDULE_EXISTS';
  end if;

  v_dept_ids := coalesce(
    p_department_ids,
    public.get_effective_norm_departments(p_asset_id, p_maintenance_type)
  );

  -- Диспетчер: всегда включить свой отдел (RLS), даже если норматив без отделов.
  if p_department_ids is null
     and public.current_profile_role() = 'dispatcher' then
    select repair_department_id into v_dispatcher_dept
    from public.profiles
    where id = auth.uid();

    if v_dispatcher_dept is not null then
      select coalesce(array_agg(distinct d), array[]::uuid[])
      into v_dept_ids
      from unnest(coalesce(v_dept_ids, array[]::uuid[]) || v_dispatcher_dept) as t(d);
    end if;
  end if;

  v_notify := coalesce(
    p_notify_dispatchers,
    auth.uid() is null or public.current_profile_role() = 'admin'
  );

  insert into public.maintenance_schedule (
    asset_id,
    maintenance_type,
    planned_date,
    status,
    notify_dispatchers
  )
  values (
    p_asset_id,
    p_maintenance_type,
    p_planned_date,
    case
      when p_planned_date < current_date then 'overdue'::public.schedule_status
      else 'scheduled'::public.schedule_status
    end,
    v_notify
  )
  returning id into v_schedule_id;

  if v_dept_ids is not null then
    foreach v_dept_id in array v_dept_ids loop
      insert into public.maintenance_schedule_departments (schedule_id, repair_department_id)
      values (v_schedule_id, v_dept_id);
    end loop;
  end if;

  return v_schedule_id;
end;
$$;

-- UC-A5: применить смену норматива к графику ППР по выбранной админом политике.
create or replace function public.sync_schedule_after_norm_change(
  p_asset_id uuid,
  p_maintenance_type public.maintenance_type,
  p_policy text -- 'recalculate_pending' | 'next_cycle_only' | 'norm_only'
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_pending_id uuid;
  v_interval integer;
  v_next_date date;
  v_dept_ids uuid[];
begin
  if public.current_profile_role() <> 'admin' then
    raise exception 'UNAUTHORIZED';
  end if;

  if p_policy = 'norm_only' then
    return;
  end if;

  select id into v_pending_id
  from public.maintenance_schedule
  where asset_id = p_asset_id
    and maintenance_type = p_maintenance_type
    and status in ('scheduled', 'overdue', 'in_progress')
  order by planned_date
  limit 1;

  if p_policy = 'recalculate_pending' then
    if v_pending_id is null then
      return;
    end if;

    select interval_days into v_interval
    from public.asset_maintenance_status
    where asset_id = p_asset_id and maintenance_type = p_maintenance_type;

    if v_interval is null then
      return;
    end if;

    update public.maintenance_schedule
    set planned_date = current_date + v_interval, status = 'scheduled', updated_at = now()
    where id = v_pending_id;

    return;
  end if;

  if p_policy = 'next_cycle_only' then
    if v_pending_id is not null then
      return;
    end if;

    select next_maintenance_date, interval_days
    into v_next_date, v_interval
    from public.asset_maintenance_status
    where asset_id = p_asset_id and maintenance_type = p_maintenance_type;

    if v_next_date is null then
      return;
    end if;

    v_dept_ids := public.get_effective_norm_departments(p_asset_id, p_maintenance_type);
    perform public.create_schedule_entry(
      p_asset_id, p_maintenance_type, v_next_date, v_dept_ids, false);

    return;
  end if;

  raise exception using errcode = 'P0001', message = 'UNKNOWN_POLICY';
end;
$$;

-- UC-A5: есть ли открытая позиция графика для (asset, type) — для диалога при save.
create or replace function public.has_pending_schedule(
  p_asset_id uuid,
  p_maintenance_type public.maintenance_type
)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select exists (
    select 1 from public.maintenance_schedule
    where asset_id = p_asset_id
      and maintenance_type = p_maintenance_type
      and status in ('scheduled', 'overdue', 'in_progress')
  );
$$;

-- UC-A5: ближайшая открытая позиция графика для бейджа в UI (до save).
create or replace function public.get_pending_schedule_entry(
  p_asset_id uuid,
  p_maintenance_type public.maintenance_type
)
returns table (planned_date date, schedule_status public.schedule_status)
language sql
stable
security definer
set search_path = public
as $$
  select planned_date, status
  from public.maintenance_schedule
  where asset_id = p_asset_id
    and maintenance_type = p_maintenance_type
    and status in ('scheduled', 'overdue', 'in_progress')
  order by planned_date
  limit 1;
$$;

-- UC-D5: автоматическая пометка просроченных позиций (pg_cron + опционально admin).
create or replace function public.mark_overdue_schedule_items()
returns int
language plpgsql
security definer
set search_path = public
as $$
declare
  v_count int;
begin
  with updated as (
    update public.maintenance_schedule
    set status = 'overdue', updated_at = now()
    where status = 'scheduled' and planned_date < current_date
    returning 1
  )
  select count(*)::int into v_count from updated;

  return v_count;
end;
$$;

-- UC-D3/D5: пакетная генерация графика из asset_maintenance_status (идемпотентно).
create or replace function public.generate_ppr_schedule(p_horizon_days int default 30)
returns int
language plpgsql
security definer
set search_path = public
as $$
declare
  v_created int := 0;
  r record;
  v_dept_ids uuid[];
begin
  if auth.uid() is not null and public.current_profile_role() <> 'admin' then
    raise exception 'UNAUTHORIZED';
  end if;

  perform public.mark_overdue_schedule_items();

  for r in
    select ams.asset_id, ams.maintenance_type, ams.next_maintenance_date
    from public.asset_maintenance_status ams
    inner join public.assets a on a.id = ams.asset_id
    where a.status <> 'decommissioned'
      and ams.next_maintenance_date <= current_date + p_horizon_days
      and not public.has_pending_schedule(ams.asset_id, ams.maintenance_type)
  loop
    v_dept_ids := public.get_effective_norm_departments(r.asset_id, r.maintenance_type);
    perform public.create_schedule_entry(
      r.asset_id, r.maintenance_type, r.next_maintenance_date, v_dept_ids);
    v_created := v_created + 1;
  end loop;

  return v_created;
end;
$$;

-- Ночной job: просрочка → генерация на 30 дней вперёд.
select cron.schedule(
  'nightly-ppr-maintenance',
  '0 0 * * *',
  $$select public.mark_overdue_schedule_items(); select public.generate_ppr_schedule(30);$$
);

create or replace function public.create_request(
  p_request_number text,
  p_type public.request_type,
  p_priority public.request_priority,
  p_title text,
  p_description text,
  p_location_description text,
  p_asset_id uuid,
  p_target_repair_department_id uuid,
  p_repair_zone public.repair_zone default 'on_site',
  p_contractor_name text default null
)
returns uuid
language plpgsql
security definer
set search_path = public
as $$
declare
  v_id uuid;
  v_role public.user_role;
  v_repair_zone public.repair_zone;
  v_contractor_name text;
begin
  v_role := public.current_profile_role();
  if v_role not in ('requester', 'dispatcher', 'admin') then
    raise exception 'UNAUTHORIZED';
  end if;

  if p_target_repair_department_id is null then
    raise exception using errcode = 'P0001', message = 'TARGET_DEPARTMENT_REQUIRED';
  end if;

  if not exists (
    select 1 from public.repair_departments
    where id = p_target_repair_department_id and is_active = true
  ) then
    raise exception using errcode = 'P0001', message = 'INVALID_TARGET_DEPARTMENT';
  end if;

  if p_asset_id is not null then
    if not exists (
      select 1 from public.assets a
      where a.id = p_asset_id
        and (
          v_role = 'admin'
          or public.requester_can_access_location(a.location_id)
        )
    ) then
      raise exception using errcode = 'P0001', message = 'ASSET_NOT_ACCESSIBLE';
    end if;
  elsif nullif(trim(coalesce(p_location_description, '')), '') is null then
    raise exception using errcode = 'P0001', message = 'ASSET_OR_LOCATION_REQUIRED';
  end if;

  if v_role = 'admin' then
    v_repair_zone := coalesce(p_repair_zone, 'on_site');
    if v_repair_zone = 'external' then
      v_contractor_name := nullif(trim(coalesce(p_contractor_name, '')), '');
      if v_contractor_name is null then
        raise exception using errcode = 'P0001', message = 'CONTRACTOR_REQUIRED';
      end if;
    else
      v_contractor_name := null;
    end if;
  else
    v_repair_zone := 'on_site';
    v_contractor_name := null;
  end if;

  insert into public.requests (
    request_number, type, priority, title, description,
    location_description, asset_id, requester_id,
    target_repair_department_id, repair_zone, contractor_name, status
  )
  values (
    p_request_number, p_type, p_priority, trim(p_title), nullif(trim(coalesce(p_description, '')), ''),
    coalesce(nullif(trim(p_location_description), ''), '—'),
    p_asset_id, auth.uid(),
    p_target_repair_department_id, v_repair_zone, v_contractor_name, 'new'
  )
  returning id into v_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  values (v_id, auth.uid(), 'new', 'new', 'Заявка создана');

  return v_id;
end;
$$;

-- UC-D1: принять заявку в свой отдел, опционально назначить техника.
create or replace function public.accept_request(
  p_request_id uuid,
  p_assignee_id uuid default null,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_dept_id uuid;
  v_status public.request_status;
  v_target_dept uuid;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  v_dept_id := public.current_profile_repair_department_id();
  if v_dept_id is null and public.current_profile_role() <> 'admin' then
    raise exception 'NO_REPAIR_DEPARTMENT';
  end if;

  select status, target_repair_department_id
  into v_status, v_target_dept
  from public.requests where id = p_request_id for update;

  if v_status is distinct from 'new' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_NEW';
  end if;

  if public.current_profile_role() = 'dispatcher'
     and v_target_dept is not null
     and v_target_dept is distinct from v_dept_id then
    raise exception using errcode = 'P0001', message = 'WRONG_TARGET_DEPARTMENT';
  end if;

  if v_dept_id is null then
    v_dept_id := v_target_dept;
  end if;

  insert into public.request_repair_departments (request_id, repair_department_id, assignee_id)
  values (p_request_id, v_dept_id, p_assignee_id)
  on conflict (request_id, repair_department_id) do update set assignee_id = excluded.assignee_id;

  update public.requests set status = 'accepted', updated_at = now() where id = p_request_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  values (p_request_id, auth.uid(), 'new', 'accepted', p_comment);
end;
$$;

-- UC-D1: отклонить заявку (ложная/дублирующая).
create or replace function public.reject_request(
  p_request_id uuid,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id for update;
  if v_status is distinct from 'new' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_NEW';
  end if;

  update public.requests set status = 'rejected', updated_at = now() where id = p_request_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  values (p_request_id, auth.uid(), 'new', 'rejected', p_comment);
end;
$$;

-- Передать заявку целиком в другой отдел (по результатам осмотра).
create or replace function public.transfer_request_department(
  p_request_id uuid,
  p_new_department_id uuid,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_own_dept uuid;
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id;
  if v_status is distinct from 'accepted' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_IN_PREPARATION';
  end if;

  v_own_dept := public.current_profile_repair_department_id();

  if public.current_profile_role() = 'admin' then
    delete from public.request_repair_departments
    where request_id = p_request_id;
  elsif public.current_profile_role() = 'dispatcher' then
    if not exists (
      select 1 from public.request_repair_departments
      where request_id = p_request_id and repair_department_id = v_own_dept
    ) then
      raise exception 'UNAUTHORIZED';
    end if;
    delete from public.request_repair_departments
    where request_id = p_request_id and repair_department_id = v_own_dept;
  end if;

  insert into public.request_repair_departments (request_id, repair_department_id)
  values (p_request_id, p_new_department_id)
  on conflict (request_id, repair_department_id) do nothing;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  select p_request_id, auth.uid(), status, status,
    coalesce(p_comment, 'Заявка передана в другой отдел по результатам осмотра')
  from public.requests where id = p_request_id;
end;
$$;

-- Подключить дополнительный отдел к заявке (совместная работа).
create or replace function public.add_request_department(
  p_request_id uuid,
  p_department_id uuid,
  p_assignee_id uuid default null,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id;
  if v_status not in ('accepted', 'in_progress') then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_ASSIGNABLE';
  end if;

  insert into public.request_repair_departments (request_id, repair_department_id, assignee_id)
  values (p_request_id, p_department_id, p_assignee_id)
  on conflict (request_id, repair_department_id) do update set assignee_id = excluded.assignee_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  select p_request_id, auth.uid(), status, status,
    coalesce(p_comment, 'Подключён дополнительный отдел для совместной работы')
  from public.requests where id = p_request_id;
end;
$$;

-- Назначить/сменить техника в рамках своего отдела (dispatcher) или указанного отдела заявки (admin).
create or replace function public.assign_request_technician(
  p_request_id uuid,
  p_technician_id uuid,
  p_repair_department_id uuid default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_dept_id uuid;
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id;
  if v_status not in ('accepted', 'in_progress') then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_ASSIGNABLE';
  end if;

  if public.current_profile_role() = 'admin' then
    v_dept_id := p_repair_department_id;
    if v_dept_id is null then
      raise exception using errcode = 'P0001', message = 'REPAIR_DEPARTMENT_REQUIRED';
    end if;
  else
    v_dept_id := public.current_profile_repair_department_id();
    if v_dept_id is null then
      raise exception 'NO_REPAIR_DEPARTMENT';
    end if;
  end if;

  if not exists (
    select 1 from public.technicians
    where id = p_technician_id and repair_department_id = v_dept_id
  ) then
    raise exception using errcode = 'P0001', message = 'TECHNICIAN_WRONG_DEPARTMENT';
  end if;

  if not exists (
    select 1 from public.request_repair_departments
    where request_id = p_request_id and repair_department_id = v_dept_id
  ) then
    raise exception 'UNAUTHORIZED';
  end if;

  if exists (
    select 1 from public.work_reports
    where request_id = p_request_id and repair_department_id = v_dept_id
  ) then
    raise exception using errcode = 'P0001', message = 'DEPARTMENT_ALREADY_REPORTED';
  end if;

  update public.request_repair_departments
  set assignee_id = p_technician_id
  where request_id = p_request_id and repair_department_id = v_dept_id;

  if not found then
    raise exception 'UNAUTHORIZED';
  end if;
end;
$$;

-- Сменить зону ремонта по результатам осмотра (не меняет status).
create or replace function public.update_request_repair_zone(
  p_request_id uuid,
  p_repair_zone public.repair_zone,
  p_contractor_name text default null,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id;
  if v_status is distinct from 'accepted' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_IN_PREPARATION';
  end if;

  update public.requests
  set repair_zone = p_repair_zone,
      contractor_name = case when p_repair_zone = 'external' then p_contractor_name else null end,
      updated_at = now()
  where id = p_request_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  select p_request_id, auth.uid(), status, status,
    coalesce(p_comment, 'Изменена зона ремонта: ' || p_repair_zone::text)
  from public.requests where id = p_request_id;
end;
$$;

-- Начать работы (accepted → in_progress).
create or replace function public.start_request_work(
  p_request_id uuid,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id for update;
  if v_status is distinct from 'accepted' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_ACCEPTED';
  end if;

  if exists (
    select 1 from public.request_repair_departments
    where request_id = p_request_id and assignee_id is null
  ) then
    raise exception using errcode = 'P0001', message = 'ALL_DEPARTMENTS_NEED_ASSIGNEE';
  end if;

  update public.requests set status = 'in_progress', updated_at = now() where id = p_request_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  values (p_request_id, auth.uid(), 'accepted', 'in_progress', p_comment);
end;
$$;

-- Начать работы по позиции ППР (scheduled | overdue → in_progress).
create or replace function public.start_schedule_work(
  p_schedule_id uuid,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.schedule_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status
  from public.maintenance_schedule
  where id = p_schedule_id
  for update;

  if v_status is null then
    raise exception using errcode = 'P0001', message = 'SCHEDULE_NOT_FOUND';
  end if;

  if v_status not in ('scheduled', 'overdue') then
    raise exception using errcode = 'P0001', message = 'SCHEDULE_NOT_OPEN';
  end if;

  update public.maintenance_schedule
  set status = 'in_progress', updated_at = now()
  where id = p_schedule_id;
end;
$$;

-- Закрыть заявку (completed → closed) от лица диспетчера/admin.
create or replace function public.close_request_as_staff(
  p_request_id uuid,
  p_comment text default null
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_status public.request_status;
begin
  if public.current_profile_role() not in ('admin', 'dispatcher') then
    raise exception 'UNAUTHORIZED';
  end if;

  select status into v_status from public.requests where id = p_request_id for update;
  if v_status is distinct from 'completed' then
    raise exception using errcode = 'P0001', message = 'REQUEST_NOT_COMPLETED';
  end if;

  update public.requests set status = 'closed', updated_at = now() where id = p_request_id;

  insert into public.request_status_history (request_id, changed_by, old_status, new_status, comment)
  values (p_request_id, auth.uid(), 'completed', 'closed', p_comment);
end;
$$;
