import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "npm:@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

type UserRole = "admin" | "dispatcher" | "requester";

interface AdminRequest {
  action: "list" | "create" | "ban" | "unban" | "delete" | "updateEmail";
  email?: string;
  password?: string;
  fullName?: string;
  role?: UserRole;
  locationId?: string;
  locationScopeIds?: string[];
  phone?: string;
  repairDepartmentId?: string | null;
  userId?: string;
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
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY") ?? "";
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";

  if (!supabaseUrl || !anonKey || !serviceRoleKey) {
    return errorResponse("Server configuration error", 500);
  }

  const authHeader = req.headers.get("Authorization");
  if (!authHeader?.startsWith("Bearer ")) {
    return errorResponse("Unauthorized", 401);
  }

  const jwt = authHeader.replace("Bearer ", "");

  const userClient = createClient(supabaseUrl, anonKey, {
    global: { headers: { Authorization: authHeader } },
    auth: { persistSession: false, autoRefreshToken: false },
  });

  const { data: userData, error: userError } = await userClient.auth.getUser(jwt);
  if (userError || !userData.user) {
    return errorResponse("Unauthorized", 401);
  }

  const callerId = userData.user.id;

  const { data: callerProfile, error: profileError } = await userClient
    .from("profiles")
    .select("role")
    .eq("id", callerId)
    .maybeSingle();

  if (profileError || callerProfile?.role !== "admin") {
    return errorResponse("Forbidden", 403);
  }

  const adminClient = createClient(supabaseUrl, serviceRoleKey, {
    auth: { persistSession: false, autoRefreshToken: false },
  });

  let body: AdminRequest;
  try {
    body = await req.json();
  } catch {
    return errorResponse("Invalid JSON body", 400);
  }

  const { action } = body;

  async function loadTargetProfile(userId: string) {
    const { data, error } = await adminClient
      .from("profiles")
      .select("id, role")
      .eq("id", userId)
      .maybeSingle();
    if (error) throw error;
    return data;
  }

  function assertNotAdminTarget(profile: { role: string } | null, userId: string) {
    if (userId === callerId) {
      throw new Response(JSON.stringify({ error: "Cannot modify own account" }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }
    if (profile?.role === "admin") {
      throw new Response(JSON.stringify({ error: "Cannot modify admin accounts" }), {
        status: 403,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }
  }

  try {
    if (action === "list") {
      const profilesRes = await adminClient
        .from("profiles")
        .select(
          "id, role, full_name, phone, location_id, repair_department_id, created_at, updated_at, locations!profiles_location_id_fkey(name), repair_departments(name), profile_location_scopes(location_id, locations(name, code))",
        )
        .order("full_name", { ascending: true });

      if (profilesRes.error) {
        return errorResponse(profilesRes.error.message, 500);
      }

      const profiles = profilesRes.data ?? [];
      const usersById = new Map<string, { email?: string; banned_until?: string | null }>();

      let page = 1;
      for (;;) {
        const { data: listData, error: listError } = await adminClient.auth.admin.listUsers({
          page,
          perPage: 200,
        });
        if (listError) {
          return errorResponse(listError.message, 500);
        }
        for (const u of listData.users) {
          usersById.set(u.id, {
            email: u.email,
            banned_until: u.banned_until ?? null,
          });
        }
        if (listData.users.length < 200) break;
        page += 1;
      }

      const items = profiles.map((p) => {
        const auth = usersById.get(p.id);
        const bannedUntil = auth?.banned_until;
        const isBanned = bannedUntil != null && new Date(bannedUntil) > new Date();
        const scopeRows = (p.profile_location_scopes ?? []) as Array<{
          location_id: string;
          locations: { name?: string; code?: string } | null;
        }>;
        const locationScopeIds = scopeRows.map((s) => s.location_id);
        const locationScopeLabels = scopeRows.map((s) => {
          const loc = s.locations;
          if (!loc) return s.location_id;
          return loc.name ?? s.location_id;
        });
        return {
          id: p.id,
          email: auth?.email ?? null,
          fullName: p.full_name,
          role: p.role,
          phone: p.phone,
          locationId: p.location_id,
          locationName: (p.locations as { name?: string } | null)?.name ?? null,
          locationScopeIds,
          locationScopeLabels,
          repairDepartmentId: p.repair_department_id,
          repairDepartmentName:
            (p.repair_departments as { name?: string } | null)?.name ?? null,
          createdAt: p.created_at,
          updatedAt: p.updated_at,
          isBanned,
          isAdminAccount: p.role === "admin",
        };
      });

      return jsonResponse({ items });
    }

    if (action === "create") {
      const { email, password, fullName, role, locationId, locationScopeIds, phone, repairDepartmentId } =
        body;

      if (!email?.trim() || !password || !fullName?.trim() || !role || !locationId) {
        return errorResponse("Missing required fields", 400);
      }

      if (role === "admin") {
        return errorResponse("Cannot create admin accounts from application", 403);
      }

      if (role !== "requester" && role !== "dispatcher") {
        return errorResponse("Invalid role", 400);
      }

      if (role === "dispatcher" && !repairDepartmentId) {
        return errorResponse("Dispatcher requires repairDepartmentId", 400);
      }

      const { data: created, error: createError } = await adminClient.auth.admin.createUser({
        email: email.trim(),
        password,
        email_confirm: true,
        user_metadata: { full_name: fullName.trim() },
      });

      if (createError || !created.user) {
        return errorResponse(createError?.message ?? "Create user failed", 400);
      }

      const userId = created.user.id;

      const { error: updateError } = await adminClient
        .from("profiles")
        .update({
          role,
          full_name: fullName.trim(),
          phone: phone?.trim() || null,
          location_id: locationId,
          repair_department_id: role === "dispatcher" ? repairDepartmentId : null,
          updated_at: new Date().toISOString(),
        })
        .eq("id", userId);

      if (updateError) {
        await adminClient.auth.admin.deleteUser(userId);
        return errorResponse(updateError.message, 500);
      }

      if (
        (role === "requester" || role === "dispatcher")
        && locationScopeIds
        && locationScopeIds.length > 0
      ) {
        const scopeRows = locationScopeIds.map((locId) => ({
          profile_id: userId,
          location_id: locId,
        }));
        const { error: scopeError } = await adminClient
          .from("profile_location_scopes")
          .insert(scopeRows);
        if (scopeError) {
          await adminClient.auth.admin.deleteUser(userId);
          return errorResponse(scopeError.message, 500);
        }
      }

      return jsonResponse({
        item: {
          id: userId,
          email: created.user.email,
          fullName: fullName.trim(),
          role,
          phone: phone?.trim() || null,
          locationId,
          locationScopeIds:
            role === "requester" || role === "dispatcher" ? (locationScopeIds ?? []) : [],
          locationScopeLabels: [],
          repairDepartmentId: role === "dispatcher" ? repairDepartmentId : null,
          isBanned: false,
          isAdminAccount: false,
        },
      });
    }

    if (action === "updateEmail") {
      const userId = body.userId;
      const email = body.email?.trim();
      if (!userId || !email) {
        return errorResponse("userId and email required", 400);
      }

      const target = await loadTargetProfile(userId);
      if (!target) {
        return errorResponse("User not found", 404);
      }

      try {
        assertNotAdminTarget(target, userId);
      } catch (r) {
        return r as Response;
      }

      const { data, error } = await adminClient.auth.admin.updateUserById(userId, { email });
      if (error) {
        return errorResponse(error.message, 400);
      }

      return jsonResponse({
        item: {
          id: userId,
          email: data.user?.email ?? email,
        },
      });
    }

    if (action === "ban" || action === "unban" || action === "delete") {
      const userId = body.userId;
      if (!userId) {
        return errorResponse("userId required", 400);
      }

      const target = await loadTargetProfile(userId);
      if (!target) {
        return errorResponse("User not found", 404);
      }

      try {
        assertNotAdminTarget(target, userId);
      } catch (r) {
        return r as Response;
      }

      if (action === "ban") {
        const { error } = await adminClient.auth.admin.updateUserById(userId, {
          ban_duration: "876000h",
        });
        if (error) return errorResponse(error.message, 400);
        return jsonResponse({ ok: true });
      }

      if (action === "unban") {
        const { error } = await adminClient.auth.admin.updateUserById(userId, {
          ban_duration: "none",
        });
        if (error) return errorResponse(error.message, 400);
        return jsonResponse({ ok: true });
      }

      const { error } = await adminClient.auth.admin.deleteUser(userId);
      if (error) return errorResponse(error.message, 400);
      return jsonResponse({ ok: true });
    }

    return errorResponse("Unknown action", 400);
  } catch (e) {
    if (e instanceof Response) return e;
    const message = e instanceof Error ? e.message : "Internal error";
    return errorResponse(message, 500);
  }
});
