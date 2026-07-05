-- =============================================================================
-- 090_views.sql — Effective-нормативы (UC-A5, COALESCE категория→override)
-- и представление asset_maintenance_status
-- =============================================================================

-- Возвращает действующие отделы для (asset, type): индивидуальный override
-- (если override_departments = true), иначе — отделы пресета категории,
-- иначе — пустой массив (UI предупреждает, что отделы не заданы).
create or replace function public.get_effective_norm_departments(
  p_asset_id uuid,
  p_maintenance_type public.maintenance_type
)
returns uuid[]
language sql
stable
security definer
set search_path = public
as $$
  select coalesce(
    (
      select array_agg(nd.repair_department_id)
      from public.maintenance_norms mn
      inner join public.maintenance_norms_departments nd on nd.norm_id = mn.id
      where mn.asset_id = p_asset_id
        and mn.maintenance_type = p_maintenance_type
        and mn.override_departments = true
    ),
    (
      select array_agg(cnd.repair_department_id)
      from public.assets a
      inner join public.category_maintenance_norms cmn
        on cmn.category_id = a.category_id and cmn.maintenance_type = p_maintenance_type
      inner join public.category_maintenance_norms_departments cnd
        on cnd.category_norm_id = cmn.id
      where a.id = p_asset_id
        and not exists (
          select 1 from public.maintenance_norms mn2
          where mn2.asset_id = p_asset_id
            and mn2.maintenance_type = p_maintenance_type
            and mn2.override_departments = true
        )
    ),
    array[]::uuid[]
  );
$$;

-- Effective-норматив по типу ТО для каждого asset: override поля имеют
-- приоритет над пресетом категории (COALESCE); assets без category_id
-- используют maintenance_norms как standalone-норматив (не COALESCE).
-- Строка появляется только когда есть хотя бы одна сторона (пресет или
-- override) с ненулевым interval_days — по инварианту "asset без ППР".
create or replace view public.effective_maintenance_norms
with (security_invoker = true) as
select
  a.id as asset_id,
  types.maintenance_type,
  coalesce(mn.interval_days, cmn.interval_days) as effective_interval_days,
  coalesce(mn.description, cmn.description) as effective_description,
  cmn.id as category_norm_id,
  mn.id as override_norm_id,
  case
    when mn.id is not null and cmn.id is not null then 'override_partial'
    when mn.id is not null then 'override'
    else 'category'
  end as source,
  mn.interval_days is not null as is_interval_overridden,
  mn.description is not null as is_description_overridden,
  coalesce(mn.override_departments, false) as is_departments_overridden
from public.assets a
cross join (select unnest(enum_range(null::public.maintenance_type)) as maintenance_type) types
left join public.category_maintenance_norms cmn
  on cmn.category_id = a.category_id and cmn.maintenance_type = types.maintenance_type
left join public.maintenance_norms mn
  on mn.asset_id = a.id and mn.maintenance_type = types.maintenance_type
where coalesce(mn.interval_days, cmn.interval_days) is not null;

drop view if exists public.asset_maintenance_status;

create view public.asset_maintenance_status
with (security_invoker = true) as
with qualifying_reports as (
  select ms.asset_id, ms.maintenance_type, wr.created_at
  from public.work_reports wr
  inner join public.maintenance_schedule ms on ms.id = wr.schedule_id
  where ms.status = 'completed'

  union all

  select r.asset_id, mt.maintenance_type, wr.created_at
  from public.work_reports wr
  inner join public.requests r on r.id = wr.request_id
  cross join lateral (
    select unnest(
      case
        when wr.maintenance_types is not null and cardinality(wr.maintenance_types) > 0
        then wr.maintenance_types
        when wr.maintenance_type is not null
        then array[wr.maintenance_type]
        else array[]::public.maintenance_type[]
      end
    ) as maintenance_type
  ) mt
  where r.asset_id is not null
    and r.status = 'closed'
    and (
      wr.maintenance_type is not null
      or (wr.maintenance_types is not null and cardinality(wr.maintenance_types) > 0)
    )
)
select
  coalesce(en.override_norm_id, en.category_norm_id) as norm_id,
  en.asset_id,
  en.maintenance_type,
  en.effective_interval_days as interval_days,
  max(qr.created_at)::date as last_maintenance_date,
  case
    when max(qr.created_at) is not null
    then (max(qr.created_at)::date + en.effective_interval_days)
    else coalesce(a.commissioning_date, current_date)
  end as next_maintenance_date
from public.effective_maintenance_norms en
inner join public.assets a on a.id = en.asset_id
left join qualifying_reports qr
  on qr.asset_id = en.asset_id
 and qr.maintenance_type = en.maintenance_type
group by
  coalesce(en.override_norm_id, en.category_norm_id),
  en.asset_id, en.maintenance_type, en.effective_interval_days, a.commissioning_date;
