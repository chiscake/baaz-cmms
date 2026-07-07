-- =============================================================================
-- 145_rls_audit_log.sql — RLS: журнал изменений только для admin
-- =============================================================================

alter table public.audit_log enable row level security;

create policy audit_log_select_admin
on public.audit_log
for select
to authenticated
using (public.current_profile_role() = 'admin');
