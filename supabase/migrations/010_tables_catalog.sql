-- =============================================================================
-- 010_tables_catalog.sql — Справочники: локации, отделы, профили, техники, оборудование
-- =============================================================================
create table public.locations (
  id uuid primary key default gen_random_uuid(),
  parent_id uuid references public.locations(id) on delete restrict,
  name text not null,
  code text unique,
  is_active boolean not null default true,
  created_at timestamptz default now(),
  updated_at timestamptz default now()
);

-- -----------------------------------------------------------------------------
-- Ремонтные отделы / службы
-- (РМУ, Энергослужба, КИПиА и т.п.)
-- -----------------------------------------------------------------------------

create table public.repair_departments (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  code text unique,
  is_active boolean not null default true,
  created_at timestamptz default now(),
  updated_at timestamptz default now()
);

-- -----------------------------------------------------------------------------
-- Профили пользователей (расширение auth.users)
-- -----------------------------------------------------------------------------

create table public.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  role public.user_role not null default 'requester',
  full_name text,
  location_id uuid references public.locations(id) on delete restrict,
  -- Привязка к ремонтному отделу: обязательно для dispatcher, NULL для остальных.
  -- Проверяется CHECK ниже.
  repair_department_id uuid references public.repair_departments(id) on delete set null,
  phone text,
  created_at timestamptz default now(),
  updated_at timestamptz default now(),
  -- Диспетчер обязан иметь отдел. admin видит всё через RLS и не привязывается к одному.
  check (role <> 'dispatcher' or repair_department_id is not null)
);

-- Зоны доступа заявителя к локациям (якоря → поддеревья через profile_accessible_location_ids).
create table public.profile_location_scopes (
  profile_id uuid not null references public.profiles(id) on delete cascade,
  location_id uuid not null references public.locations(id) on delete restrict,
  primary key (profile_id, location_id)
);

create index on public.profile_location_scopes (location_id);

-- -----------------------------------------------------------------------------
-- Персонал ТОиР (слесари, электрики — без учётной записи)
-- -----------------------------------------------------------------------------

create table public.technicians (
  id uuid primary key default gen_random_uuid(),
  full_name text not null,
  specialty text not null,
  is_active boolean not null default true,
  repair_department_id uuid references public.repair_departments(id) on delete set null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

-- -----------------------------------------------------------------------------
-- Категории оборудования (UC-A5) — режим эксплуатации/назначение, источник
-- пресетов нормативов ТО. Свободный текст (name/description), без enum:
-- у разных предприятий деление разное (по действию, по материалу, по модели…).
-- -----------------------------------------------------------------------------

create table public.equipment_categories (
  id uuid primary key default gen_random_uuid(),
  name text not null unique,
  description text,
  is_active boolean not null default true,
  created_at timestamptz default now(),
  updated_at timestamptz default now()
);

-- -----------------------------------------------------------------------------
-- Реестр оборудования (инвентарные основные средства)
-- -----------------------------------------------------------------------------

create table public.assets (
  id uuid primary key default gen_random_uuid(),
  asset_number text not null unique,
  name text not null,
  location_id uuid not null references public.locations(id) on delete restrict,
  -- Опционально: источник пресета нормативов ТО (UC-A5). NULL — объект без
  -- категории; ППР возможен только через индивидуальные maintenance_norms.
  category_id uuid references public.equipment_categories(id) on delete set null,
  manufacturer text,
  model text,
  serial_number text,
  commissioning_date date,
  status public.asset_status default 'active',
  description text,
  created_at timestamptz default now(),
  updated_at timestamptz default now()
);
