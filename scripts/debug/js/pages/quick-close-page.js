import {
  getCurrentProfile,
  getSupabase,
  isAdmin,
} from "../supabase-client.js";
import { formatError } from "../format-error.js";
import { buildRandomWorkReport } from "../work-report-templates.js";
import { pickRandom } from "../random.js";

/** @typedef {{ id: string, full_name: string, repair_department_id: string }} TechnicianRow */

/** @type {TechnicianRow[]} */
let technicians = [];

/** @type {HTMLElement | null} */
let quickCloseRoot = null;

/** @param {{ onLog: (message: string, kind?: string) => void }} ctx */
export function createQuickClosePage(ctx) {
  return {
    label: "Закрытие «В работе»",
    mount(root) {
      quickCloseRoot = root;
      root.innerHTML = `
        <div class="stack">
          <p class="hint">
            Создаёт отчёты о работах с рандомным текстом для всех отделов без отчёта.
            Требуется роль <strong>dispatcher</strong> или <strong>admin</strong>.
          </p>
          <div class="toolbar-row">
            <button type="button" id="qc-refresh" class="secondary">Обновить списки</button>
          </div>
          <section>
            <h3 class="section-title">Заявки в работе</h3>
            <div id="qc-requests" class="item-list muted">Загрузка…</div>
          </section>
          <section>
            <h3 class="section-title">ППР в работе</h3>
            <div id="qc-schedule" class="item-list muted">Загрузка…</div>
          </section>
        </div>
      `;

      root.querySelector("#qc-refresh")?.addEventListener("click", () => {
        void loadLists(root, ctx);
      });
    },
    onShow(root) {
      return loadLists(root, ctx);
    },
  };
}

/** @param {ParentNode} scope @param {{ onLog: Function }} ctx */
async function loadLists(scope, ctx) {
  const profile = getCurrentProfile();
  const requestsEl = scope.querySelector("#qc-requests");
  const scheduleEl = scope.querySelector("#qc-schedule");

  if (!profile) {
    requestsEl && (requestsEl.textContent = "Требуется вход.");
    scheduleEl && (scheduleEl.textContent = "Требуется вход.");
    return;
  }

  if (profile.role !== "admin" && profile.role !== "dispatcher") {
    const msg = "Доступно только диспетчеру или администратору.";
    requestsEl && (requestsEl.textContent = msg);
    scheduleEl && (scheduleEl.textContent = msg);
    return;
  }

  if (requestsEl instanceof HTMLElement) {
    requestsEl.textContent = "Загрузка…";
  }
  if (scheduleEl instanceof HTMLElement) {
    scheduleEl.textContent = "Загрузка…";
  }

  try {
    const supabase = getSupabase();
    const techResult = await supabase
      .from("technicians")
      .select("id, full_name, repair_department_id")
      .eq("is_active", true);

    if (techResult.error) {
      throw techResult.error;
    }
    technicians = techResult.data ?? [];

    const [requestsResult, scheduleResult] = await Promise.all([
      supabase
        .from("requests")
        .select(
          "id, request_number, title, status, request_repair_departments(repair_department_id, repair_departments(name, code)), work_reports(repair_department_id)",
        )
        .eq("status", "in_progress")
        .order("updated_at", { ascending: false }),
      supabase
        .from("maintenance_schedule")
        .select(
          "id, maintenance_type, planned_date, status, assets(asset_number, name), maintenance_schedule_departments(repair_department_id, repair_departments(name, code)), work_reports(repair_department_id)",
        )
        .eq("status", "in_progress")
        .order("planned_date", { ascending: false }),
    ]);

    if (requestsResult.error) {
      throw requestsResult.error;
    }
    if (scheduleResult.error) {
      throw scheduleResult.error;
    }

    renderRequestList(requestsEl, requestsResult.data ?? [], ctx);
    renderScheduleList(scheduleEl, scheduleResult.data ?? [], ctx);
  } catch (error) {
    const detail = formatError(error);
    ctx.onLog(`Ошибка загрузки «В работе»: ${detail}`, "error");
    if (requestsEl instanceof HTMLElement) {
      requestsEl.textContent = detail;
    }
    if (scheduleEl instanceof HTMLElement) {
      scheduleEl.textContent = detail;
    }
  }
}

/** @param {Element | null} container @param {any[]} rows @param {{ onLog: Function }} ctx */
function renderRequestList(container, rows, ctx) {
  if (!(container instanceof HTMLElement)) {
    return;
  }

  if (rows.length === 0) {
    container.className = "item-list muted";
    container.textContent = "Нет заявок в статусе «В работе».";
    return;
  }

  container.className = "item-list";
  container.replaceChildren();

  for (const row of rows) {
    container.append(buildRequestCard(row, ctx));
  }
}

/** @param {Element | null} container @param {any[]} rows @param {{ onLog: Function }} ctx */
function renderScheduleList(container, rows, ctx) {
  if (!(container instanceof HTMLElement)) {
    return;
  }

  if (rows.length === 0) {
    container.className = "item-list muted";
    container.textContent = "Нет позиций ППР в статусе «В работе».";
    return;
  }

  container.className = "item-list";
  container.replaceChildren();

  for (const row of rows) {
    container.append(buildScheduleCard(row, ctx));
  }
}

/** @param {any} row @param {{ onLog: Function }} ctx */
function buildRequestCard(row, ctx) {
  const pending = getPendingDepartments(row);
  const card = document.createElement("article");
  card.className = "work-item";

  const title = document.createElement("div");
  title.className = "work-item-title";
  title.textContent = `${row.request_number} — ${row.title}`;

  const meta = document.createElement("div");
  meta.className = "work-item-meta";
  meta.textContent = formatPendingLabel(pending, row);

  const actions = document.createElement("div");
  actions.className = "work-item-actions";

  const button = document.createElement("button");
  button.type = "button";
  button.textContent = pending.actionable.length > 0 ? "Заполнить отчёт" : "Нет доступных отделов";
  button.disabled = pending.actionable.length === 0;
  button.addEventListener("click", () => {
    void submitReports({
      kind: "request",
      row,
      departmentIds: pending.actionable,
      ctx,
      button,
    });
  });

  actions.append(button);
  card.append(title, meta, actions);
  return card;
}

/** @param {any} row @param {{ onLog: Function }} ctx */
function buildScheduleCard(row, ctx) {
  const pending = getPendingDepartments(row);
  const asset = row.assets;
  const assetLabel = asset
    ? `${asset.asset_number} — ${asset.name}`
    : "—";

  const card = document.createElement("article");
  card.className = "work-item";

  const title = document.createElement("div");
  title.className = "work-item-title";
  title.textContent = `ППР ${String(row.maintenance_type).toUpperCase()} — ${assetLabel}`;

  const meta = document.createElement("div");
  meta.className = "work-item-meta";
  meta.textContent = `${formatDate(row.planned_date)} · ${formatPendingLabel(pending, row)}`;

  const actions = document.createElement("div");
  actions.className = "work-item-actions";

  const button = document.createElement("button");
  button.type = "button";
  button.textContent = pending.actionable.length > 0 ? "Заполнить отчёт" : "Нет доступных отделов";
  button.disabled = pending.actionable.length === 0;
  button.addEventListener("click", () => {
    void submitReports({
      kind: "schedule",
      row,
      departmentIds: pending.actionable,
      ctx,
      button,
    });
  });

  actions.append(button);
  card.append(title, meta, actions);
  return card;
}

/** @param {any} row */
function getPendingDepartments(row) {
  const profile = getCurrentProfile();
  const routingKey =
    row.request_repair_departments !== undefined
      ? "request_repair_departments"
      : "maintenance_schedule_departments";

  const assigned = (row[routingKey] ?? []).map((entry) => ({
    id: entry.repair_department_id,
    name: entry.repair_departments?.name ?? entry.repair_department_id,
    code: entry.repair_departments?.code ?? "",
  }));

  const reported = new Set(
    (row.work_reports ?? []).map((report) => report.repair_department_id),
  );

  const pending = assigned.filter((dept) => !reported.has(dept.id));

  let actionable = pending.map((dept) => dept.id);
  if (!isAdmin() && profile?.repair_department_id) {
    actionable = actionable.filter((id) => id === profile.repair_department_id);
  } else if (!isAdmin()) {
    actionable = [];
  }

  return { pending, actionable, reportedCount: reported.size, assignedCount: assigned.length };
}

/** @param {{ pending: { id: string, name: string, code: string }[], actionable: string[], assignedCount: number, reportedCount: number }} pending @param {any} _row */
function formatPendingLabel(pending, _row) {
  const deptNames = pending.pending.map((d) => d.code || d.name).join(", ") || "—";
  const accessNote =
    pending.actionable.length === 0 && pending.pending.length > 0
      ? " · ваш отдел уже сдал или нет прав"
      : "";
  return `Отделы без отчёта: ${deptNames} (${pending.reportedCount}/${pending.assignedCount} сдано)${accessNote}`;
}

/** @param {string | null | undefined} value */
function formatDate(value) {
  if (!value) {
    return "—";
  }
  return new Date(value).toLocaleDateString("ru-RU");
}

/** @param {{ kind: "request" | "schedule", row: any, departmentIds: string[], ctx: { onLog: Function }, button: HTMLButtonElement }} params */
async function submitReports({ kind, row, departmentIds, ctx, button }) {
  const profile = getCurrentProfile();
  if (!profile) {
    ctx.onLog("Требуется вход.", "warn");
    return;
  }

  button.disabled = true;
  try {
    const supabase = getSupabase();
    let created = 0;

    for (const departmentId of departmentIds) {
      const technician = pickTechnicianForDepartment(departmentId);
      if (!technician) {
        ctx.onLog(`Нет активного техника для отдела ${departmentId}`, "warn");
        continue;
      }

      const randomReport = buildRandomWorkReport(
        kind === "schedule" ? row.maintenance_type : null,
      );

      const payload = {
        request_id: kind === "request" ? row.id : null,
        schedule_id: kind === "schedule" ? row.id : null,
        repair_department_id: departmentId,
        author_id: profile.id,
        technician_id: technician.id,
        work_performed: randomReport.work_performed,
        actual_duration_hours: randomReport.actual_duration_hours,
        defects_found: randomReport.defects_found,
        notes: randomReport.notes
          ? randomReport.parts_used
            ? `${randomReport.notes} ЗИП: ${randomReport.parts_used}`
            : randomReport.notes
          : randomReport.parts_used
            ? `ЗИП: ${randomReport.parts_used}`
            : null,
      };

      if (kind === "schedule" && randomReport.maintenance_type) {
        payload.maintenance_type = randomReport.maintenance_type;
      }

      const { error } = await supabase.from("work_reports").insert(payload);
      if (error) {
        throw error;
      }
      created += 1;
    }

    const label =
      kind === "request"
        ? row.request_number
        : `ППР ${row.maintenance_type} (${row.assets?.asset_number ?? row.id})`;

    ctx.onLog(`Создано отчётов: ${created} для «${label}»`, "success");
    if (quickCloseRoot) {
      await loadLists(quickCloseRoot, ctx);
    }
  } catch (error) {
    ctx.onLog(`Ошибка создания отчёта: ${formatError(error)}`, "error");
    button.disabled = false;
  }
}

/** @param {string} departmentId */
function pickTechnicianForDepartment(departmentId) {
  const pool = technicians.filter(
    (tech) => tech.repair_department_id === departmentId,
  );
  return pickRandom(pool) ?? null;
}
