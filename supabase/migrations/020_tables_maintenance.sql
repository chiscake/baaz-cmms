-- =============================================================================
-- 020_tables_maintenance.sql — Таблицы нормативов ТО и графика ППР
-- =============================================================================
-- Sparse overrides (UC-A5): строка существует только когда хотя бы одно поле
-- переопределено относительно пресета категории (или это standalone-норматив
-- для asset без category_id — тогда interval_days обязателен по смыслу, но
-- остаётся nullable в схеме, т.к. общий CHECK применим к обоим случаям).
-- interval_days/description NULL = наследовать из category_maintenance_norms.
create table public.maintenance_norms (
  id uuid primary key default gen_random_uuid(),
  asset_id uuid not null references public.assets(id) on delete cascade,
  maintenance_type public.maintenance_type not null,
  interval_days integer,
  description text,
  -- true — отделы переопределены индивидуально (maintenance_norms_departments);
  -- false — наследуются из пресета категории через get_effective_norm_departments.
  override_departments boolean not null default false,
  created_at timestamptz default now(),
  updated_at timestamptz default now(),
  unique (asset_id, maintenance_type)
);

-- Отвечающие службы за вид ТО по нормативу (многие-ко-многим).
-- Используется только когда override_departments = true.
-- Шаблон: при генерации позиции ППР копируется в maintenance_schedule_departments.
create table public.maintenance_norms_departments (
  norm_id uuid not null references public.maintenance_norms(id) on delete cascade,
  repair_department_id uuid not null references public.repair_departments(id) on delete restrict,
  primary key (norm_id, repair_department_id)
);

-- -----------------------------------------------------------------------------
-- Пресеты нормативов ТО по категории оборудования (UC-A5)
-- -----------------------------------------------------------------------------

create table public.category_maintenance_norms (
  id uuid primary key default gen_random_uuid(),
  category_id uuid not null references public.equipment_categories(id) on delete cascade,
  maintenance_type public.maintenance_type not null,
  interval_days integer not null,
  description text,
  created_at timestamptz default now(),
  updated_at timestamptz default now(),
  unique (category_id, maintenance_type)
);

-- Отделы пресета — зеркало maintenance_norms_departments.
create table public.category_maintenance_norms_departments (
  category_norm_id uuid not null references public.category_maintenance_norms(id) on delete cascade,
  repair_department_id uuid not null references public.repair_departments(id) on delete restrict,
  primary key (category_norm_id, repair_department_id)
);

-- -----------------------------------------------------------------------------
-- График ППР
-- -----------------------------------------------------------------------------

create table public.maintenance_schedule (
  id uuid primary key default gen_random_uuid(),
  asset_id uuid not null references public.assets(id) on delete cascade,
  maintenance_type public.maintenance_type not null,
  planned_date date not null,
  status public.schedule_status default 'scheduled',
  -- true только при INSERT из create_schedule_entry (admin / nightly job); не norm sync / dispatcher.
  notify_dispatchers boolean not null default false,
  created_at timestamptz default now(),
  updated_at timestamptz default now()
);

-- Исполнители конкретной позиции ППР (многие-ко-многим).
-- Копируется из maintenance_norms_departments при создании позиции;
-- может быть скорректировано диспетчером.
-- Используется триггером завершения ППР (см. ниже).
create table public.maintenance_schedule_departments (
  schedule_id uuid not null references public.maintenance_schedule(id) on delete cascade,
  repair_department_id uuid not null references public.repair_departments(id) on delete restrict,
  primary key (schedule_id, repair_department_id)
);

create index on public.maintenance_schedule_departments (repair_department_id);

-- Одна открытая позиция на пару (asset, type) — идемпотентность generate_ppr_schedule.
create unique index maintenance_schedule_one_open_per_asset_type
  on public.maintenance_schedule (asset_id, maintenance_type)
  where status in ('scheduled', 'overdue', 'in_progress');

create index maintenance_schedule_status_planned_date_idx
  on public.maintenance_schedule (status, planned_date);
