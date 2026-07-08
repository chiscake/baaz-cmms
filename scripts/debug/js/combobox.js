/**
 * Заменяет нативный <select> кастомным списком — корректные цвета в dark mode на Windows.
 * Исходный select остаётся в DOM (hidden) для чтения/записи value.
 */

/** @param {HTMLSelectElement} select */
export function enhanceSelect(select) {
  if (select.dataset.comboEnhanced === "true") {
    return;
  }
  select.dataset.comboEnhanced = "true";

  const wrap = document.createElement("div");
  wrap.className = "combo";
  select.parentNode?.insertBefore(wrap, select);
  wrap.append(select);

  select.classList.add("combo-native");
  select.tabIndex = -1;
  select.setAttribute("aria-hidden", "true");

  const trigger = document.createElement("button");
  trigger.type = "button";
  trigger.className = "combo-trigger";
  trigger.setAttribute("aria-haspopup", "listbox");
  trigger.setAttribute("aria-expanded", "false");

  const list = document.createElement("ul");
  list.className = "combo-list";
  list.role = "listbox";
  list.hidden = true;

  wrap.append(trigger, list);

  /** @type {(() => void) | null} */
  let rebuild = null;

  const close = () => {
    list.hidden = true;
    trigger.setAttribute("aria-expanded", "false");
    wrap.classList.remove("combo-open");
  };

  const open = () => {
    document.querySelectorAll(".combo-open").forEach((node) => {
      if (node !== wrap) {
        node.classList.remove("combo-open");
        const otherList = node.querySelector(".combo-list");
        if (otherList instanceof HTMLElement) {
          otherList.hidden = true;
        }
        node.querySelector(".combo-trigger")?.setAttribute("aria-expanded", "false");
      }
    });

    list.hidden = false;
    trigger.setAttribute("aria-expanded", "true");
    wrap.classList.add("combo-open");

    const active = list.querySelector(".combo-option.is-selected");
    if (active instanceof HTMLElement) {
      active.scrollIntoView({ block: "nearest" });
    }
  };

  const syncTrigger = () => {
    const option = select.selectedOptions[0];
    trigger.textContent = option?.textContent?.trim() || "—";
    trigger.disabled = select.disabled;

    for (const item of list.querySelectorAll(".combo-option")) {
      item.classList.toggle("is-selected", item.dataset.value === select.value);
      item.setAttribute(
        "aria-selected",
        item.dataset.value === select.value ? "true" : "false",
      );
    }
  };

  rebuild = () => {
    list.replaceChildren();

    for (const option of select.options) {
      if (option.disabled && !option.value) {
        continue;
      }

      const item = document.createElement("li");
      item.className = "combo-option";
      item.role = "option";
      item.dataset.value = option.value;
      item.textContent = option.textContent?.trim() ?? "";
      item.tabIndex = -1;

      if (option.value === select.value) {
        item.classList.add("is-selected");
        item.setAttribute("aria-selected", "true");
      } else {
        item.setAttribute("aria-selected", "false");
      }

      item.addEventListener("click", () => {
        select.value = option.value;
        select.dispatchEvent(new Event("change", { bubbles: true }));
        syncTrigger();
        close();
      });

      list.append(item);
    }

    syncTrigger();
  };

  trigger.addEventListener("click", () => {
    if (select.disabled) {
      return;
    }
    if (list.hidden) {
      open();
    } else {
      close();
    }
  });

  select.addEventListener("change", syncTrigger);

  const observer = new MutationObserver(() => {
    rebuild?.();
  });
  observer.observe(select, { childList: true, subtree: true, attributes: true });

  document.addEventListener("click", (event) => {
    if (!wrap.contains(/** @type {Node} */ (event.target))) {
      close();
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      close();
    }
  });

  rebuild();
}

/** @param {ParentNode} root */
export function enhanceSelects(root) {
  for (const select of root.querySelectorAll("select")) {
    enhanceSelect(/** @type {HTMLSelectElement} */ (select));
  }
}
