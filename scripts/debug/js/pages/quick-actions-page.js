import { canRunAction, quickActions, runAllQuickActions, runQuickAction } from "../quick-actions.js";

/** @param {{ onLog: (message: string, kind?: string) => void }} ctx */
export function createQuickActionsPage(ctx) {
  return {
    label: "Быстрые действия",
    mount(root) {
      root.innerHTML = `
        <div class="stack">
          <p class="hint">
            Пакетные RPC и служебные операции для локальной отладки.
            Большинство действий требуют роль <strong>admin</strong>.
          </p>
          <div class="toolbar-row">
            <button type="button" id="qa-run-all">Выполнить все доступные</button>
          </div>
          <div id="qa-actions" class="action-grid"></div>
        </div>
      `;

      const grid = root.querySelector("#qa-actions");
      if (!(grid instanceof HTMLElement)) {
        return;
      }

      for (const action of quickActions) {
        grid.append(buildActionCard(action, ctx));
      }

      root.querySelector("#qa-run-all")?.addEventListener("click", (event) => {
        const button = /** @type {HTMLButtonElement} */ (event.currentTarget);
        button.disabled = true;
        void runAllQuickActions(ctx).finally(() => {
          button.disabled = false;
          renderAvailability(root, ctx);
        });
      });
    },
    onShow(root) {
      renderAvailability(root, ctx);
    },
  };
}

/** @param {import("../quick-actions.js").QuickAction} action @param {{ onLog: Function }} ctx */
function buildActionCard(action, ctx) {
  const card = document.createElement("article");
  card.className = "action-card";
  card.dataset.actionId = action.id;

  const title = document.createElement("h3");
  title.className = "action-card-title";
  title.textContent = action.label;

  const description = document.createElement("p");
  description.className = "action-card-desc";
  description.textContent = action.description;

  const roles = document.createElement("p");
  roles.className = "action-card-roles hint";
  roles.textContent = action.roles?.length
    ? `Роли: ${action.roles.join(", ")}`
    : "Роли: любая";

  const button = document.createElement("button");
  button.type = "button";
  button.textContent = "Выполнить";
  button.addEventListener("click", () => {
    void runQuickAction(action, ctx, button);
  });

  card.append(title, description, roles, button);
  return card;
}

/** @param {HTMLElement} root @param {{ onLog: Function }} ctx */
function renderAvailability(root, ctx) {
  for (const card of root.querySelectorAll(".action-card")) {
    if (!(card instanceof HTMLElement)) {
      continue;
    }
    const action = quickActions.find((item) => item.id === card.dataset.actionId);
    const button = card.querySelector("button");
    if (!(action instanceof Object) || !(button instanceof HTMLButtonElement)) {
      continue;
    }
    const allowed = canRunAction(action, ctx);
    button.disabled = !allowed;
    card.classList.toggle("action-card-disabled", !allowed);
  }
}
