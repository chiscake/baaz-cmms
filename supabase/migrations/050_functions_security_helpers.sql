-- =============================================================================
-- 050_functions_security_helpers.sql — Вспомогательные функции безопасности и управления локациями
-- =============================================================================
create or replace function public.current_profile_role()
returns public.user_role
language sql
stable
security definer
set search_path = public
as $$
  select role from public.profiles where id = auth.uid()
$$;

create or replace function public.current_profile_repair_department_id()
returns uuid
language sql
stable
security definer
set search_path = public
as $$
  select repair_department_id from public.profiles where id = auth.uid()
$$;

-- UC-A1: поддерево локаций (корень + все потомки)
create or replace function public.location_subtree_ids(p_root uuid)
returns uuid[]
language sql
stable
security definer
set search_path = public
as $$
  with recursive tree as (
    select id from public.locations where id = p_root
    union all
    select l.id
    from public.locations l
    inner join tree t on l.parent_id = t.id
  )
  select coalesce(array_agg(id), array[]::uuid[]) from tree;
$$;

-- Якоря зон доступа: явные scopes или fallback на profiles.location_id для requester.
create or replace function public.profile_scope_anchors(p_profile uuid)
returns uuid[]
language sql
stable
security definer
set search_path = public
as $$
  with explicit as (
    select array_agg(pls.location_id) as ids
    from public.profile_location_scopes pls
    where pls.profile_id = p_profile
  ),
  prof as (
    select role, location_id from public.profiles where id = p_profile
  )
  select case
    when coalesce(array_length((select ids from explicit), 1), 0) > 0
      then (select ids from explicit)
    when (select role from prof) in ('requester', 'dispatcher')
      and (select location_id from prof) is not null
      then array[(select location_id from prof)]
    else array[]::uuid[]
  end;
$$;

-- Объединение поддеревьев всех якорей профиля.
create or replace function public.profile_accessible_location_ids(p_profile uuid default auth.uid())
returns uuid[]
language sql
stable
security definer
set search_path = public
as $$
  select coalesce(
    array_agg(distinct loc_id),
    array[]::uuid[]
  )
  from (
    select unnest(public.location_subtree_ids(anchor)) as loc_id
    from unnest(public.profile_scope_anchors(p_profile)) as anchor
  ) expanded;
$$;

-- Проверка доступа к узлу локации при действиях заявителя (admin — всегда true).
create or replace function public.requester_can_access_location(p_location uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select case public.current_profile_role()
    when 'admin' then true
    when 'dispatcher' then p_location = any(public.profile_accessible_location_ids(auth.uid()))
    when 'requester' then p_location = any(public.profile_accessible_location_ids(auth.uid()))
    else false
  end;
$$;

-- Разрыв цикла RLS requests ↔ request_repair_departments (42P17 infinite recursion).
-- SECURITY DEFINER: чтение без повторного применения политик на связанных таблицах.

create or replace function public.request_is_routed_to_department(
  p_request_id uuid,
  p_department_id uuid
)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select exists (
    select 1
    from public.request_repair_departments rrd
    where rrd.request_id = p_request_id
      and rrd.repair_department_id = p_department_id
  );
$$;

create or replace function public.request_is_owned_by(
  p_request_id uuid,
  p_user_id uuid default auth.uid()
)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select exists (
    select 1
    from public.requests r
    where r.id = p_request_id
      and r.requester_id = p_user_id
  );
$$;

create or replace function public.request_visible_to_current_user(p_request_id uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
  select exists (
    select 1
    from public.requests r
    where r.id = p_request_id
      and (
        r.requester_id = auth.uid()
        or (select role from public.profiles where id = auth.uid()) in ('admin', 'dispatcher')
      )
  );
$$;

-- UC-A1: проверка перед архивацией/удалением ветки
create or replace function public.assert_location_branch_deletable(p_location_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  subtree uuid[];
begin
  if public.current_profile_role() <> 'admin' then
    raise exception 'UNAUTHORIZED';
  end if;

  subtree := public.location_subtree_ids(p_location_id);

  if exists (
    select 1 from public.profiles
    where location_id = any(subtree)
  ) then
    raise exception using errcode = 'P0001', message = 'LOCATIONS_HAS_PROFILES';
  end if;

  if exists (
    select 1 from public.profile_location_scopes pls
    where pls.location_id = any(subtree)
  ) then
    raise exception using errcode = 'P0001', message = 'LOCATIONS_HAS_PROFILE_SCOPES';
  end if;

  if exists (
    select 1 from public.assets
    where location_id = any(subtree)
      and status <> 'decommissioned'
  ) then
    raise exception using errcode = 'P0001', message = 'LOCATIONS_HAS_ASSETS';
  end if;
end;
$$;

create or replace function public.archive_location_branch(p_location_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  subtree uuid[];
begin
  perform public.assert_location_branch_deletable(p_location_id);
  subtree := public.location_subtree_ids(p_location_id);

  update public.locations
  set is_active = false,
      updated_at = now()
  where id = any(subtree);
end;
$$;

create or replace function public.restore_location_branch(p_location_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  subtree uuid[];
begin
  if public.current_profile_role() <> 'admin' then
    raise exception 'UNAUTHORIZED';
  end if;

  subtree := public.location_subtree_ids(p_location_id);

  update public.locations
  set is_active = true,
      updated_at = now()
  where id = any(subtree);
end;
$$;

create or replace function public.hard_delete_location_branch(p_location_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  subtree uuid[];
  node_id uuid;
begin
  perform public.assert_location_branch_deletable(p_location_id);
  subtree := public.location_subtree_ids(p_location_id);

  loop
    select l.id into node_id
    from public.locations l
    where l.id = any(subtree)
      and not exists (
        select 1
        from public.locations c
        where c.parent_id = l.id
          and c.id = any(subtree)
      )
    limit 1;

    exit when node_id is null;

    delete from public.locations where id = node_id;
  end loop;
end;
$$;
