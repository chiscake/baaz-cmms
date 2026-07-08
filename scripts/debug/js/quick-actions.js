import { formatError } from "./format-error.js";
import { getCurrentProfile, getSupabase } from "./supabase-client.js";

/** @typedef {{ id: string, label: string, description: string, roles?: string[], run: (ctx: ActionContext) => Promise<void> }} QuickAction */

/** @typedef {{ onLog: (message: string, kind?: string) => void }} ActionContext */

/** @type {QuickAction[]} */
export const quickActions = [
  {
    id: "generate-ppr",
    label: "Обновить график ТО",
    description:
      "RPC generate_ppr_schedule(30): пометка просроченных + генерация позиций ППР на 30 дней.",
    roles: ["admin"],
    async run({ onLog }) {
      const supabase = getSupabase();
      const { data, error } = await supabase.rpc("generate_ppr_schedule", {
        p_horizon_days: 30,
      });
      if (error) {
        throw error;
      }
      onLog(`График ТО обновлён: создано позиций — ${data ?? 0}.`, "success");
    },
  },
  {
    id: "mark-overdue",
    label: "Пометить просроченные ППР",
    description: "RPC mark_overdue_schedule_items: scheduled → overdue.",
    roles: ["admin"],
    async run({ onLog }) {
      const supabase = getSupabase();
      const { data, error } = await supabase.rpc("mark_overdue_schedule_items");
      if (error) {
        throw error;
      }
      onLog(`Просрочено позиций: ${data ?? 0}.`, "success");
    },
  },
];

/** @param {QuickAction} action @param {ActionContext} ctx */
export function canRunAction(action, ctx) {
  const profile = getCurrentProfile();
  if (!profile) {
    return false;
  }
  if (!action.roles?.length) {
    return true;
  }
  return action.roles.includes(profile.role);
}

/** @param {QuickAction} action @param {ActionContext} ctx @param {HTMLButtonElement} button */
export async function runQuickAction(action, ctx, button) {
  if (!canRunAction(action, ctx)) {
    ctx.onLog(`«${action.label}»: недостаточно прав (${action.roles?.join(", ") ?? "any"}).`, "warn");
    return;
  }

  button.disabled = true;
  try {
    await action.run(ctx);
  } catch (error) {
    const detail = formatError(error);
    ctx.onLog(`«${action.label}»: ${detail}`, "error");
  } finally {
    button.disabled = false;
  }
}

/** @param {ActionContext} ctx */
export async function runAllQuickActions(ctx) {
  const profile = getCurrentProfile();
  if (!profile) {
    ctx.onLog("Требуется вход.", "warn");
    return;
  }

  const runnable = quickActions.filter((action) => canRunAction(action, ctx));
  if (runnable.length === 0) {
    ctx.onLog("Нет доступных быстрых действий для текущей роли.", "warn");
    return;
  }

  for (const action of runnable) {
    try {
      await action.run(ctx);
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      ctx.onLog(`«${action.label}»: ${detail}`, "error");
      break;
    }
  }
}
