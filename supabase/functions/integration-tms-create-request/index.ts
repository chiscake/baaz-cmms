import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

interface CreateRepairRequestBody {
  client_reference_id: string;
  inventory_id: string;
  inventory_kind?: string;
  inventory_name: string;
  inventory_serial?: string;
  inventory_type_name?: string;
  request_type: "inspection" | "service";
  title: string;
  description?: string;
  target_repair_department_id?: string;
  inventory_handoff_mode?: "pickup_at_warehouse" | "deliver_to_department";
  inventory_warehouse_name?: string;
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

  let body: CreateRepairRequestBody;
  try {
    body = await req.json();
  } catch {
    return errorResponse("Invalid JSON body", 400);
  }

  const adminClient = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });

  const clientRef = body.client_reference_id?.trim();
  const inventoryId = body.inventory_id?.trim();
  const title = body.title?.trim();

  if (!clientRef || !inventoryId || !title) {
    return errorResponse("client_reference_id, inventory_id and title are required", 400);
  }

  const { data: existingRef } = await adminClient
    .schema("integration")
    .from("tms_repair_client_refs")
    .select("request_id")
    .eq("client_reference_id", clientRef)
    .maybeSingle();

  if (existingRef?.request_id) {
    const { data: existingReq } = await adminClient
      .from("requests")
      .select("id, request_number, status, created_at")
      .eq("id", existingRef.request_id)
      .maybeSingle();
    if (existingReq) {
      return jsonResponse({
        request_id: existingReq.id,
        request_number: existingReq.request_number,
        status: existingReq.status,
        created_at: existingReq.created_at,
      });
    }
  }

  const inventoryKind = body.inventory_kind ?? "tool";
  const locationDescription =
    `TMS: ${body.inventory_name}${body.inventory_serial ? ` (${body.inventory_serial})` : ""}`;

  const requestNumber = `З-TMS-${crypto.randomUUID().slice(0, 8)}`;

  const { data: openConflict } = await adminClient
    .from("requests")
    .select("id")
    .eq("inventory_id", inventoryId)
    .eq("inventory_kind", inventoryKind)
    .not("status", "in", "(closed,rejected,cancelled)")
    .maybeSingle();

  if (openConflict?.id) {
    return errorResponse("Open inventory request already exists", 409);
  }

  const integrationUserId = Deno.env.get("CMMS_INTEGRATION_REQUESTER_ID");
  let requesterId = integrationUserId;
  if (!requesterId) {
    const { data: adminProfile } = await adminClient
      .from("profiles")
      .select("id")
      .eq("role", "admin")
      .limit(1)
      .maybeSingle();
    requesterId = adminProfile?.id;
  }

  if (!requesterId) {
    return errorResponse("No integration requester configured", 500);
  }

  const targetDept = body.target_repair_department_id?.trim();
  if (!targetDept) {
    const { data: defaultDept } = await adminClient
      .from("repair_departments")
      .select("id")
      .eq("is_active", true)
      .limit(1)
      .maybeSingle();
    if (!defaultDept?.id) {
      return errorResponse("target_repair_department_id required", 400);
    }
    body.target_repair_department_id = defaultDept.id;
  } else {
    const { data: deptRow } = await adminClient
      .from("repair_departments")
      .select("id")
      .eq("id", targetDept)
      .eq("is_active", true)
      .maybeSingle();
    if (!deptRow?.id) {
      return errorResponse("target_repair_department_id invalid or inactive", 400);
    }
    body.target_repair_department_id = deptRow.id;
  }

  const handoffMode = body.inventory_handoff_mode ?? "deliver_to_department";
  if (handoffMode !== "pickup_at_warehouse" && handoffMode !== "deliver_to_department") {
    return errorResponse("inventory_handoff_mode invalid", 400);
  }

  const { data: inserted, error: insertError } = await adminClient
    .from("requests")
    .insert({
      request_number: requestNumber,
      type: body.request_type,
      inventory_id: inventoryId,
      inventory_kind: inventoryKind,
      inventory_name: body.inventory_name,
      inventory_serial: body.inventory_serial ?? null,
      inventory_type_name: body.inventory_type_name ?? null,
      inventory_source: "tms",
      inventory_handoff_mode: handoffMode,
      inventory_warehouse_name: body.inventory_warehouse_name?.trim() || null,
      location_description: locationDescription,
      requester_id: requesterId,
      title,
      description: body.description ?? null,
      target_repair_department_id: body.target_repair_department_id,
      repair_zone: "workshop",
      status: "new",
    })
    .select("id, request_number, status, created_at")
    .single();

  if (insertError || !inserted) {
    return errorResponse(insertError?.message ?? "Insert failed", 500);
  }

  await adminClient.schema("integration").from("tms_repair_client_refs").insert({
    client_reference_id: clientRef,
    request_id: inserted.id,
  });

  return jsonResponse(
    {
      request_id: inserted.id,
      request_number: inserted.request_number,
      status: inserted.status,
      created_at: inserted.created_at,
    },
    201,
  );
});
