import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

interface InventoryReceivedBody {
  request_id: string;
  inventory_id: string;
  handed_over_at?: string;
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

  let body: InventoryReceivedBody;
  try {
    body = await req.json();
  } catch {
    return errorResponse("Invalid JSON body", 400);
  }

  const requestId = body.request_id?.trim();
  const inventoryId = body.inventory_id?.trim();
  if (!requestId || !inventoryId) {
    return errorResponse("request_id and inventory_id are required", 400);
  }

  const adminClient = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });

  const { data: reqRow, error: fetchError } = await adminClient
    .from("requests")
    .select("id, inventory_id, inventory_handoff_mode, status, inventory_received_at")
    .eq("id", requestId)
    .maybeSingle();

  if (fetchError || !reqRow) {
    return errorResponse("Request not found", 404);
  }

  if (reqRow.inventory_id !== inventoryId) {
    return errorResponse("inventory_id mismatch", 400);
  }

  if (reqRow.inventory_handoff_mode !== "deliver_to_department") {
    return errorResponse("handoff mode is not deliver_to_department", 400);
  }

  if (reqRow.inventory_received_at && reqRow.status === "in_progress") {
    return jsonResponse({
      request_id: requestId,
      status: "in_progress",
      inventory_received_at: reqRow.inventory_received_at,
    });
  }

  const comment = body.handed_over_at
    ? `Инструмент передан в отдел (TMS, ${body.handed_over_at})`
    : "Инструмент передан в отдел (TMS)";

  const { error: rpcError } = await adminClient.rpc("confirm_inventory_received", {
    p_request_id: requestId,
    p_comment: comment,
  });

  if (rpcError) {
    const msg = rpcError.message ?? "confirm_inventory_received failed";
    if (msg.includes("REQUEST_NOT_ACCEPTED")) {
      return errorResponse(msg, 409);
    }
    if (msg.includes("ALL_DEPARTMENTS_NEED_ASSIGNEE")) {
      return errorResponse(msg, 409);
    }
    return errorResponse(msg, 500);
  }

  const { data: updated } = await adminClient
    .from("requests")
    .select("id, status, inventory_received_at")
    .eq("id", requestId)
    .single();

  return jsonResponse({
    request_id: updated?.id ?? requestId,
    status: updated?.status ?? "in_progress",
    inventory_received_at: updated?.inventory_received_at,
  });
});
