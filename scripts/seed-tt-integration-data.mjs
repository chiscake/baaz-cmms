#!/usr/bin/env node
/**
 * Seed demo data for CMMS ↔ TMS integration (contour A/B).
 * Run after: supabase db reset && node scripts/seed-test-users.mjs
 */
import { createClient } from '@supabase/supabase-js';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const CMMS_URL = process.env.SUPABASE_URL ?? 'http://127.0.0.1:54321';
const CMMS_KEY = process.env.SUPABASE_SERVICE_ROLE_KEY ?? process.env.SUPABASE_SECRET_KEY;

/** Demo tool UUID (TMS seed micrometer) — inventory_id in CMMS */
export const DEMO_TMS_TOOL_ID = 'd1000000-0000-4000-8000-000000000001';

/** Demo inventory repair request (contour A) */
export const DEMO_INVENTORY_REQUEST_ID = 'b2220000-0000-4000-8000-000000000301';

/** ИНСТ — instrument repair department */
export const DEMO_INVENTORY_TARGET_DEPT_ID = 'd1000000-0000-4000-8000-000000000004';

export const ttIntegrationInventoryRequest = {
  id: DEMO_INVENTORY_REQUEST_ID,
  request_number: 'З-TMS-DEMO-001',
  type: 'inspection',
  priority: 'normal',
  repair_zone: 'workshop',
  title: 'Поверка микрометра МК-25',
  description: 'Демо inventory-заявка из TMS (контур А)',
  location_description: 'Склад: ИРК-1 (Мехцех), Микрометр МК-25 0.01',
  // Закрытая — иначе блокирует повторную отправку того же инструмента из TMS (БААЗ-00142).
  status: 'closed',
  inventory_id: DEMO_TMS_TOOL_ID,
  inventory_kind: 'tool',
  inventory_name: 'Микрометр МК-25 0.01',
  inventory_serial: 'SN-2024-001',
  inventory_type_name: 'Мерительный инструмент',
  inventory_source: 'tms',
  inventory_handoff_mode: 'deliver_to_department',
  inventory_warehouse_name: 'ИРК-1 (Мехцех)',
  target_repair_department_id: DEMO_INVENTORY_TARGET_DEPT_ID,
  created_at: '2026-07-01T10:00:00+00:00',
  updated_at: '2026-07-01T10:00:00+00:00',
};

async function resolveRequesterId(supabase) {
  const email = process.env.TT_INTEGRATION_REQUESTER_EMAIL ?? 'gromov.n@baaz.by';
  const { data: users, error } = await supabase.auth.admin.listUsers({ perPage: 200 });
  if (error) throw error;
  const user = users.users.find((u) => u.email === email);
  if (!user) {
    throw new Error(
      `Requester not found: ${email}. Убедитесь, что seed-test-users создал этого пользователя (demoUsers).`,
    );
  }
  return user.id;
}

export async function seedTtIntegrationDemo(supabase) {
  const requesterId = await resolveRequesterId(supabase);

  // Idempotent: закрыть устаревшие открытые inventory-заявки на демо-инструмент
  // (иначе REP-API-1 вернёт 409, а в TMS инструмент остаётся available без cmms_repair_links).
  const { error: staleCloseError } = await supabase
    .from('requests')
    .update({
      status: 'closed',
      updated_at: ttIntegrationInventoryRequest.updated_at,
    })
    .eq('inventory_id', DEMO_TMS_TOOL_ID)
    .eq('inventory_kind', 'tool')
    .not('status', 'in', '(closed,rejected,cancelled)');
  if (staleCloseError) throw staleCloseError;

  const row = {
    ...ttIntegrationInventoryRequest,
    requester_id: requesterId,
    asset_id: null,
  };

  const { error: reqError } = await supabase.from('requests').upsert(row, { onConflict: 'id' });
  if (reqError) throw reqError;

  const { error: rrdError } = await supabase.from('request_repair_departments').upsert(
    {
      request_id: DEMO_INVENTORY_REQUEST_ID,
      repair_department_id: DEMO_INVENTORY_TARGET_DEPT_ID,
      added_at: ttIntegrationInventoryRequest.created_at,
    },
    { onConflict: 'request_id,repair_department_id' },
  );
  if (rrdError) throw rrdError;

  const { error: refError } = await supabase.schema('integration').from('tms_repair_client_refs').upsert(
    {
      client_reference_id: 'f3000000-0000-4000-8000-000000000001',
      request_id: DEMO_INVENTORY_REQUEST_ID,
    },
    { onConflict: 'client_reference_id' },
  );
  if (refError) throw refError;

  console.log(`TT integration: inventory request ${row.request_number} (${DEMO_INVENTORY_REQUEST_ID})`);
}

async function main() {
  if (!CMMS_KEY) {
    console.error('Set SUPABASE_SERVICE_ROLE_KEY or SUPABASE_SECRET_KEY');
    process.exit(1);
  }

  const supabase = createClient(CMMS_URL, CMMS_KEY, { auth: { persistSession: false } });
  await seedTtIntegrationDemo(supabase);
}

const isDirectRun =
  process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url);

if (isDirectRun) {
  main().catch((e) => {
    console.error(e);
    process.exit(1);
  });
}
