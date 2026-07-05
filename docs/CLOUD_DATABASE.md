# Облачная БД Supabase — памятка команд

Краткий справочник для проекта **baaz-cmms**: связка локального репозитория с hosted-проектом Supabase, деплой схемы и seed.

**Источник схемы:** `supabase/migrations/` (не dashboard).  
**Облачный project ref:** `nuygawdgrzoiehefysfv` (West EU, Ireland).

---

## Предварительно (один раз)

```powershell
cd C:\Users\Chis\Desktop\baaz-cmms

# Авторизация CLI (Personal Access Token)
supabase login

# Привязка каталога к облачному проекту
supabase link --project-ref nuygawdgrzoiehefysfv
# Пароль БД — из Dashboard → Project Settings → Database
```

Для CI / без интерактива:

```powershell
$env:SUPABASE_ACCESS_TOKEN = "sbp_..."
$env:SUPABASE_DB_PASSWORD = "..."
supabase link --project-ref nuygawdgrzoiehefysfv
```

Проверка связи:

```powershell
supabase projects list
supabase migration list
```

---

## Локально vs облако

| Задача | Локально | Облако (linked) |
|--------|----------|-----------------|
| Полный reset + миграции + `seed.sql` | `supabase db reset` | `supabase db reset --linked` |
| Только новые миграции | `supabase migration up --local` | `supabase db push` или `supabase migration up --linked` |
| Сравнить историю мигraций | `supabase migration list --local` | `supabase migration list` (LOCAL \| REMOTE) |
| Схема облака → файлы | — | `supabase db pull` |
| Seed без reset | — | после reset или `psql` / SQL Editor |

Флаг `--linked` явно не обязателен, если проект уже привязан через `supabase link` — CLI по умолчанию работает с linked remote.

---

## Деплой схемы в облако (без wipe)

Инкрементально — только мигraции, которых ещё нет в remote:

```powershell
supabase migration list
supabase db push --dry-run    # план без применения
supabase db push
```

`db push` **не** запускает `seed.sql` по умолчанию. Seed в remote отдельно:

```powershell
supabase db push --include-seed
```

---

## Полный reset облака (деструктивно)

```powershell
supabase db reset --linked
```

Что происходит:

1. Очищаются пользовательские объекты в remote (в т.ч. truncate `auth.*` — все пользователи удаляются).
2. Применяются все файлы из `supabase/migrations/` (000 … 150).
3. Выполняется `supabase/seed.sql` (если в `config.toml` `[db.seed] enabled = true`).

Без seed:

```powershell
supabase db reset --linked --no-seed
```

**Не использовать на production** без явной необходимости.

После reset в конце может появиться **Warning** про `pgdelta-target-ca.crt` — это сбой кэша pg-delta для diff, **не** ошибка миграций/seed. При `exit code 0` операция считается успешной.

---

## Демо-учётки (отдельно от seed.sql)

`seed.sql` наполняет справочники и оборудование, но **не** создаёт `auth.users`.

После reset (локально или в облаке) нужен JS-seed:

```powershell
# В .env или .env.cloud — URL и service_role **облачного** проекта:
# SUPABASE_URL=https://nuygawdgrzoiehefysfv.supabase.co
# SUPABASE_SERVICE_ROLE_KEY=...

node --env-file=.env.cloud scripts/seed-test-users.mjs
# или:
pnpm db:seed:cloud
# локально после reset:
pnpm db:seed
```

Пароль всех демо-аккаунтов: `123` (см. комментарии в `scripts/seed-test-users.mjs`).

---

## Типовые сценарии

### Первый депл schema в пустой cloud-проект

```powershell
supabase link --project-ref nuygawdgrzoiehefysfv
supabase migration list          # REMOTE пустой
supabase db reset --linked       # или: supabase db push
pnpm db:seed:cloud
pnpm fn:deploy                   # Edge Function admin-users
```

### Изменили мигraции локально, нужно только догнать облако

```powershell
supabase db reset                # проверка локально
supabase db push                 # только новые мигraции в cloud
```

### Полностью пересобрать облако «как локально после db reset»

```powershell
supabase db reset --linked
pnpm db:seed:cloud
pnpm fn:deploy
```

Локальный аналог одной командой (Docker + seed + edge):

```powershell
pnpm db:reset
```

---

## Edge Functions (облако)

Локально на Windows предпочтительно `pnpm fn:serve` (см. `AGENTS.md`). В облако:

```powershell
pnpm fn:deploy
pnpm fn:logs
```

Reset/push **не** деплоит functions — только схема БД.

---

## Если история мигraций разъехалась

```powershell
supabase migration list
# Пометить версию как applied/reverted без выполнения SQL:
supabase migration repair 20240414044403 --status reverted
supabase migration repair 20240414044403 --status applied
```

Подробнее: [Supabase CLI — migration repair](https://supabase.com/docs/reference/cli/supabase-migration-repair).

---

## Ключи и приложение

- **Dashboard:** Project Settings → API (URL, anon/publishable, service_role).
- **Приложение:** URL и ключи можно переопределить на странице Settings в WinUI.
- **`.env.example`** — шаблон для **локального** стека (`127.0.0.1:54321`). Для облака заведите `.env.cloud` (в `.gitignore`), не коммитьте секреты.

---

## Ссылки

- [Local development & deploy](https://supabase.com/docs/guides/cli/local-development)
- [CLI: db push](https://supabase.com/docs/reference/cli/supabase-db-push)
- [CLI: db reset](https://supabase.com/docs/reference/cli/supabase-db-reset)
- [Seeding](https://supabase.com/docs/guides/local-development/seeding-your-database)
- В репозитории: `AGENTS.md` (§ Database workflow, Edge Functions), `package.json` (`db:seed`, `fn:deploy`)
