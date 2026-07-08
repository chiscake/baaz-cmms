import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

interface ToolRequisitionStatusBody {
  schema_version?: number;
  tms_requisition_id: string;
  status: string;
  previous_status?: string;
  occurred_at?: string;
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}

function errorResponse(message: string, status: number): Response {
  return jsonResponse({ error: message }, status);
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "POST") {
    return errorResponse("Method not allowed", 405);
  }

  const supabaseUrl = Deno.env.get("SUPABASE_URL") ?? "";
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  const integrationSecret = Deno.env.get("CMMS_INTEGRATION_SECRET") ?? "";

  if (!supabaseUrl || !serviceRoleKey) {
    return errorResponse("Server configuration error", 500);
  }

  const authHeader = req.headers.get("Authorization");
  if (integrationSecret) {
    if (!authHeader?.startsWith("Bearer ")) {
      return errorResponse("Unauthorized", 401);
    }
    const token = authHeader.replace("Bearer ", "").trim();
    if (token !== integrationSecret) {
      return errorResponse("Forbidden", 403);
    }
  }

  let body: ToolRequisitionStatusBody;
  try {
    body = await req.json();
  } catch {
    return errorResponse("Invalid JSON body", 400);
  }

  const tmsRequisitionId = body.tms_requisition_id?.trim();
  const status = body.status?.trim();
  if (!tmsRequisitionId || !status) {
    return errorResponse("tms_requisition_id and status are required", 400);
  }

  const adminClient = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });

  const syncedAt = body.occurred_at ?? new Date().toISOString();

  const { data: existing, error: fetchError } = await adminClient
    .from("tms_tool_requisition_links")
    .select("id, last_known_status")
    .eq("tms_requisition_id", tmsRequisitionId)
    .maybeSingle();

  if (fetchError) {
    return errorResponse(fetchError.message ?? "Failed to load link", 500);
  }

  if (!existing) {
    return jsonResponse({ ok: true, skipped: "link_not_found" });
  }

  if (existing.last_known_status === status) {
    return jsonResponse({
      ok: true,
      link_id: existing.id,
      status,
      unchanged: true,
    });
  }

  const { data: updated, error: updateError } = await adminClient
    .from("tms_tool_requisition_links")
    .update({
      last_known_status: status,
      last_synced_at: syncedAt,
    })
    .eq("id", existing.id)
    .select("id, last_known_status, last_synced_at")
    .single();

  if (updateError) {
    return errorResponse(updateError.message ?? "Failed to update link", 500);
  }

  return jsonResponse({
    ok: true,
    link_id: updated?.id ?? existing.id,
    status: updated?.last_known_status ?? status,
    previous_status: body.previous_status ?? existing.last_known_status,
    last_synced_at: updated?.last_synced_at ?? syncedAt,
  });
});
