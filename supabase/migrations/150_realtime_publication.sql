-- =============================================================================
-- 150_realtime_publication.sql — Публикация таблиц для Supabase Realtime
-- =============================================================================
alter publication supabase_realtime add table public.requests;
alter publication supabase_realtime add table public.maintenance_schedule;
alter publication supabase_realtime add table public.work_reports;
alter publication supabase_realtime add table public.request_repair_departments;
alter publication supabase_realtime add table public.tms_tool_requisition_links;

-- Полная replica identity: Realtime UPDATE с RLS отдаёт record целиком (last_known_status для toast/UI).
alter table public.tms_tool_requisition_links replica identity full;
