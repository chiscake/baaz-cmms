import { createClient } from "@supabase/supabase-js";
import {
  dtIntegrationActors,
  dtIntegrationRequests,
  dtIntegrationRouting,
  dtIntegrationWorkReports,
} from "./seed-dt-integration-data.mjs";
import { demoUserPassword, demoUsers } from "./seed-test-users-data.mjs";
import {
  buildLocationParentMap,
  buildRequesterCandidates,
  buildRequesterIndex,
  resolveRequesterEmail,
} from "./seed-requester-resolver.mjs";
import {
  workflowActors,
  workflowPprScheduleDepartments,
  workflowPprSchedules,
  workflowPprWorkReports,
  workflowRequests,
  workflowStatusHistory,
  workflowWorkReports,
} from "./seed-workflow-demo-data.mjs";

/**
 * Создание демо-учёток и DT-моков через Supabase Admin API.
 *
 * Схема UUID локаций (supabase/seed.sql):
 *   a1000000-0000-4000-8000-BBBCCCDDDRRR
 */

const supabaseUrl = process.env.SUPABASE_URL;
const serviceRoleKey = process.env.SUPABASE_SERVICE_ROLE_KEY;

if (!supabaseUrl || !serviceRoleKey) {
  throw new Error("Missing SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY in environment.");
}

const supabase = createClient(supabaseUrl, serviceRoleKey, {
  auth: {
    autoRefreshToken: false,
    persistSession: false,
  },
});

const RETRYABLE_HTTP_STATUSES = new Set([502, 503, 504]);

function isRetryableServiceError(error) {
  if (!error) {
    return false;
  }

  if (RETRYABLE_HTTP_STATUSES.has(error.status)) {
    return true;
  }

  return error.name === "AuthRetryableFetchError";
}

async function withServiceRetry(label, fn, { maxAttempts = 20, baseDelayMs = 500 } = {}) {
  let lastError;

  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const result = await fn();
      if (result?.error && isRetryableServiceError(result.error)) {
        throw result.error;
      }

      return result;
    } catch (error) {
      lastError = error;
      if (!isRetryableServiceError(error) || attempt === maxAttempts) {
        throw error;
      }

      const delayMs = Math.min(baseDelayMs * attempt, 5000);
      const detail = error.status ?? error.message ?? error.name;
      console.warn(
        `${label}: сервис ещё не готов (${detail}), повтор ${attempt}/${maxAttempts} через ${delayMs} мс...`,
      );
      await new Promise((resolve) => setTimeout(resolve, delayMs));
    }
  }

  throw lastError;
}

function requireUserByEmail(usersByEmail, email) {
  const user = usersByEmail.get(email.toLowerCase());
  if (!user?.id) {
    throw new Error(`User not found by email: ${email}`);
  }
  return user;
}

async function loadLocationsByCode() {
  const { data, error } = await withServiceRetry("PostgREST (locations)", () =>
    supabase.from("locations").select("id, code, parent_id"),
  );

  if (error) {
    throw error;
  }

  const locationsByCode = new Map();
  for (const location of data ?? []) {
    if (location.code) {
      locationsByCode.set(location.code, location.id);
    }
  }

  return { locationsByCode, locations: data ?? [] };
}

async function loadAssetLocationById() {
  const { data, error } = await withServiceRetry("PostgREST (assets)", () =>
    supabase.from("assets").select("id, location_id"),
  );

  if (error) {
    throw error;
  }

  const assetLocationById = new Map();
  for (const asset of data ?? []) {
    assetLocationById.set(asset.id, asset.location_id);
  }

  return assetLocationById;
}

function createRequesterResolver({ locations, locationsByCode }) {
  const parentById = buildLocationParentMap(locations);
  const requesterIndex = buildRequesterIndex(
    buildRequesterCandidates(demoUsers, locationsByCode),
    parentById,
  );
  const fallbackEmail = "sokolova.m@baaz.by";

  return {
    fallbackEmail,
    resolve(row) {
      return resolveRequesterEmail({
        assetLocationId: row.assetLocationId,
        explicitEmail: row.explicitEmail,
        requesterIndex,
        parentById,
        fallbackEmail,
      });
    },
  };
}

function logRequesterDistribution(label, assignments) {
  const counts = assignments.reduce((acc, email) => {
    acc[email] = (acc[email] ?? 0) + 1;
    return acc;
  }, {});

  console.log(
    `${label} requesters: ${Object.entries(counts)
      .map(([email, count]) => `${email.split("@")[0]}=${count}`)
      .join(", ")}`,
  );
}

async function loadRepairDepartmentsByCode() {
  const { data, error } = await withServiceRetry("PostgREST (repair_departments)", () =>
    supabase.from("repair_departments").select("id, code"),
  );

  if (error) {
    throw error;
  }

  const departmentsByCode = new Map();
  for (const department of data ?? []) {
    if (department.code) {
      departmentsByCode.set(department.code, department.id);
    }
  }

  return departmentsByCode;
}

async function getExistingUsersByEmail() {
  const usersByEmail = new Map();
  let page = 1;

  for (;;) {
    const { data, error } = await withServiceRetry("Supabase Auth (listUsers)", () =>
      supabase.auth.admin.listUsers({
        page,
        perPage: 200,
      }),
    );

    if (error) {
      throw error;
    }

    for (const user of data.users) {
      if (user.email) {
        usersByEmail.set(user.email.toLowerCase(), user);
      }
    }

    if (data.users.length < 200) {
      break;
    }

    page += 1;
  }

  return usersByEmail;
}

async function createOrGetUser(target, usersByEmail) {
  const { data, error } = await withServiceRetry("Supabase Auth (createUser)", () =>
    supabase.auth.admin.createUser({
      email: target.email,
      password: demoUserPassword,
      email_confirm: true,
      user_metadata: {
        full_name: target.fullName,
      },
    }),
  );

  if (error && !/already|exists|registered/i.test(error.message)) {
    throw error;
  }

  if (data?.user) {
    usersByEmail.set(target.email.toLowerCase(), data.user);
    return data.user;
  }

  return requireUserByEmail(usersByEmail, target.email);
}

async function updateProfile(target, userId, locationsByCode, repairDepartmentsByCode) {
  const locationId = locationsByCode.get(target.locationCode);
  if (!locationId) {
    throw new Error(`Unknown location code: ${target.locationCode}`);
  }

  let repairDepartmentId = null;
  if (target.repairDepartmentCode) {
    repairDepartmentId = repairDepartmentsByCode.get(target.repairDepartmentCode) ?? null;
    if (!repairDepartmentId) {
      throw new Error(`Unknown repair department code: ${target.repairDepartmentCode}`);
    }
  } else if (target.role === "dispatcher") {
    throw new Error(`Dispatcher ${target.email} must have repairDepartmentCode`);
  }

  const { error } = await supabase
    .from("profiles")
    .update({
      role: target.role,
      full_name: target.fullName,
      phone: target.phone,
      location_id: locationId,
      repair_department_id: repairDepartmentId,
      updated_at: new Date().toISOString(),
    })
    .eq("id", userId);

  if (error) {
    throw error;
  }

  const rolesWithScopes = new Set(["requester", "dispatcher"]);
  if (rolesWithScopes.has(target.role) && target.locationScopeCodes?.length) {
    await supabase.from("profile_location_scopes").delete().eq("profile_id", userId);

    const scopeRows = target.locationScopeCodes.map((code) => {
      const scopeLocationId = locationsByCode.get(code);
      if (!scopeLocationId) {
        throw new Error(`Unknown location scope code: ${code}`);
      }
      return { profile_id: userId, location_id: scopeLocationId };
    });

    const { error: scopeError } = await supabase
      .from("profile_location_scopes")
      .insert(scopeRows);

    if (scopeError) {
      throw scopeError;
    }
  } else if (target.role === "admin") {
    await supabase.from("profile_location_scopes").delete().eq("profile_id", userId);
  }
}

async function seedDtIntegrationDemo(usersByEmail, repairDepartmentsByCode, requesterResolver, assetLocationById) {
  const author = requireUserByEmail(usersByEmail, dtIntegrationActors.authorEmail);

  const repairDepartmentId = repairDepartmentsByCode.get(dtIntegrationActors.repairDepartmentCode);
  if (!repairDepartmentId) {
    throw new Error(`Unknown repair department code: ${dtIntegrationActors.repairDepartmentCode}`);
  }

  const requesterEmails = [];
  const requests = dtIntegrationRequests.map((row) => {
    const requesterEmail = requesterResolver.resolve({
      assetLocationId: assetLocationById.get(row.asset_id),
      explicitEmail: row.requesterEmail,
    });
    requesterEmails.push(requesterEmail);
    const requester = requireUserByEmail(usersByEmail, requesterEmail);

    return {
      ...row,
      repair_zone: row.repair_zone ?? "on_site",
      requester_id: requester.id,
      target_repair_department_id: repairDepartmentId,
    };
  });

  const { error: reqError } = await supabase.from("requests").upsert(requests, { onConflict: "id" });
  if (reqError) {
    throw reqError;
  }

  const routedStatuses = new Set(["accepted", "in_progress", "completed", "closed"]);
  const multiRoutingByRequestId = new Map(
    dtIntegrationRouting.map((row) => [row.request_id, row.departments]),
  );

  const routing = [];
  for (const row of dtIntegrationRequests.filter((r) => routedStatuses.has(r.status))) {
    const multi = multiRoutingByRequestId.get(row.id);
    if (multi) {
      for (const dept of multi) {
        const deptId = repairDepartmentsByCode.get(dept.repairDepartmentCode);
        if (!deptId) {
          throw new Error(`Unknown repair department code: ${dept.repairDepartmentCode}`);
        }
        routing.push({
          request_id: row.id,
          repair_department_id: deptId,
          assignee_id: dept.technicianId,
        });
      }
      continue;
    }

    routing.push({
      request_id: row.id,
      repair_department_id: repairDepartmentId,
      assignee_id: dtIntegrationActors.technicianId,
    });
  }

  const { error: routeError } = await supabase
    .from("request_repair_departments")
    .upsert(routing, { onConflict: "request_id,repair_department_id" });

  if (routeError) {
    throw routeError;
  }

  const workReports = dtIntegrationWorkReports.map((row) => {
    const deptCode = row.repairDepartmentCode ?? dtIntegrationActors.repairDepartmentCode;
    const deptId = repairDepartmentsByCode.get(deptCode);
    if (!deptId) {
      throw new Error(`Unknown repair department code: ${deptCode}`);
    }

    const technicianId = row.technicianId ?? dtIntegrationActors.technicianId;
    const { repairDepartmentCode, technicianId: _tech, ...rest } = row;

    return {
      ...rest,
      repair_department_id: deptId,
      author_id: author.id,
      technician_id: technicianId,
    };
  });

  const { error: wrError } = await supabase
    .from("work_reports")
    .upsert(workReports, { onConflict: "id" });

  if (wrError) {
    throw wrError;
  }

  console.log(
    `DT integration: ${requests.length} requests, ${workReports.length} work_reports (fixtures DT)`,
  );
  logRequesterDistribution("DT integration", requesterEmails);
}

async function seedWorkflowDemo(usersByEmail, repairDepartmentsByCode, requesterResolver, assetLocationById) {
  const author = requireUserByEmail(usersByEmail, workflowActors.authorEmail);

  const requesterEmails = [];
  const requests = workflowRequests.map((row) => {
    const targetDepartmentId = repairDepartmentsByCode.get(row.target_repair_department_code);
    if (!targetDepartmentId) {
      throw new Error(`Unknown repair department code: ${row.target_repair_department_code}`);
    }

    const requesterEmail = requesterResolver.resolve({
      assetLocationId: assetLocationById.get(row.asset_id),
      explicitEmail: row.requesterEmail,
    });
    requesterEmails.push(requesterEmail);
    const requester = requireUserByEmail(usersByEmail, requesterEmail);

    const {
      target_repair_department_code: _code,
      routing: _routing,
      requesterEmail: _email,
      ...rest
    } = row;

    return {
      ...rest,
      repair_zone: row.repair_zone ?? "on_site",
      requester_id: requester.id,
      target_repair_department_id: targetDepartmentId,
    };
  });

  const { error: reqError } = await supabase.from("requests").upsert(requests, { onConflict: "id" });
  if (reqError) {
    throw reqError;
  }

  const routingRows = [];
  for (const row of workflowRequests) {
    if (!row.routing?.length) {
      continue;
    }

    for (const route of row.routing) {
      const repairDepartmentId = repairDepartmentsByCode.get(route.repair_department_code);
      if (!repairDepartmentId) {
        throw new Error(`Unknown repair department code: ${route.repair_department_code}`);
      }

      routingRows.push({
        request_id: row.id,
        repair_department_id: repairDepartmentId,
        assignee_id: route.assignee_id ?? null,
      });
    }
  }

  if (routingRows.length > 0) {
    const { error: routeError } = await supabase
      .from("request_repair_departments")
      .upsert(routingRows, { onConflict: "request_id,repair_department_id" });

    if (routeError) {
      throw routeError;
    }
  }

  const requestWorkReports = workflowWorkReports.map((row) => {
    const repairDepartmentId = repairDepartmentsByCode.get(row.repair_department_code);
    if (!repairDepartmentId) {
      throw new Error(`Unknown repair department code: ${row.repair_department_code}`);
    }

    const { repair_department_code: _code, technician_id, ...rest } = row;
    return {
      ...rest,
      repair_department_id: repairDepartmentId,
      author_id: author.id,
      technician_id: technician_id ?? workflowActors.defaultTechnicianId,
    };
  });

  if (requestWorkReports.length > 0) {
    const { error: wrError } = await supabase
      .from("work_reports")
      .upsert(requestWorkReports, { onConflict: "id" });

    if (wrError) {
      throw wrError;
    }
  }

  const statusHistoryRows = workflowStatusHistory.map((row) => {
    const changedBy = requireUserByEmail(usersByEmail, row.changed_by_email);
    const { changed_by_email: _email, ...rest } = row;
    return {
      ...rest,
      changed_by: changedBy.id,
    };
  });

  if (statusHistoryRows.length > 0) {
    const { error: historyError } = await supabase
      .from("request_status_history")
      .upsert(statusHistoryRows, { onConflict: "id" });

    if (historyError) {
      throw historyError;
    }
  }

  const statusCounts = workflowRequests.reduce((acc, row) => {
    acc[row.status] = (acc[row.status] ?? 0) + 1;
    return acc;
  }, {});

  console.log(
    `Workflow demo: ${requests.length} requests (${Object.entries(statusCounts)
      .map(([status, count]) => `${count} ${status}`)
      .join(", ")}), ${routingRows.length} routing, ${requestWorkReports.length} work_reports, ${statusHistoryRows.length} history`,
  );
  logRequesterDistribution("Workflow demo", requesterEmails);
}

async function seedPprWorkReportDemo(usersByEmail, repairDepartmentsByCode) {
  const author = requireUserByEmail(usersByEmail, workflowActors.authorEmail);

  if (workflowPprSchedules.length > 0) {
    const { error: scheduleError } = await supabase
      .from("maintenance_schedule")
      .upsert(workflowPprSchedules, { onConflict: "id" });
    if (scheduleError) {
      throw scheduleError;
    }
  }

  if (workflowPprScheduleDepartments.length > 0) {
    const deptRows = workflowPprScheduleDepartments.map((row) => {
      const repairDepartmentId = repairDepartmentsByCode.get(row.repair_department_code);
      if (!repairDepartmentId) {
        throw new Error(`Unknown repair department code: ${row.repair_department_code}`);
      }

      return {
        schedule_id: row.schedule_id,
        repair_department_id: repairDepartmentId,
      };
    });

    const { error: deptError } = await supabase
      .from("maintenance_schedule_departments")
      .upsert(deptRows, { onConflict: "schedule_id,repair_department_id" });
    if (deptError) {
      throw deptError;
    }
  }

  const pprReports = workflowPprWorkReports.map((row) => {
    const repairDepartmentId = repairDepartmentsByCode.get(row.repair_department_code);
    if (!repairDepartmentId) {
      throw new Error(`Unknown repair department code: ${row.repair_department_code}`);
    }

    const { repair_department_code: _code, technician_id, ...rest } = row;
    return {
      ...rest,
      repair_department_id: repairDepartmentId,
      author_id: author.id,
      technician_id: technician_id ?? workflowActors.defaultTechnicianId,
    };
  });

  const { error: wrError } = await supabase.from("work_reports").upsert(pprReports, { onConflict: "id" });
  if (wrError) {
    throw wrError;
  }

  console.log(`PPR demo: ${pprReports.length} schedule work_reports (UC-D4)`);
}

async function main() {
  const { locationsByCode, locations } = await loadLocationsByCode();
  const repairDepartmentsByCode = await loadRepairDepartmentsByCode();
  const assetLocationById = await loadAssetLocationById();
  const requesterResolver = createRequesterResolver({ locations, locationsByCode });
  const usersByEmail = await getExistingUsersByEmail();

  for (const target of demoUsers) {
    const user = await createOrGetUser(target, usersByEmail);
    await updateProfile(target, user.id, locationsByCode, repairDepartmentsByCode);
    const dept = target.repairDepartmentCode ? `, ${target.repairDepartmentCode}` : "";
    const scopes = target.locationScopeCodes?.length
      ? `, scopes=[${target.locationScopeCodes.join(", ")}]`
      : "";
    console.log(`Seeded: ${target.email} (${target.role}, ${target.locationCode}${dept}${scopes})`);
  }

  await seedDtIntegrationDemo(usersByEmail, repairDepartmentsByCode, requesterResolver, assetLocationById);
  await seedWorkflowDemo(usersByEmail, repairDepartmentsByCode, requesterResolver, assetLocationById);
  await seedPprWorkReportDemo(usersByEmail, repairDepartmentsByCode);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
