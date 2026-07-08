import { NewRequestForm } from "../new-request-form.js";

/** @type {NewRequestForm | null} */
let form = null;

/** @param {{ onLog: (message: string, kind?: string) => void }} ctx */
export function createNewRequestPage(ctx) {
  return {
    label: "Новая заявка",
    mount(root) {
      root.innerHTML = `<div id="new-request-mount"></div>`;
      const mount = root.querySelector("#new-request-mount");
      if (!(mount instanceof HTMLElement)) {
        return;
      }
      form = new NewRequestForm({ onLog: ctx.onLog });
      form.mount(mount);
    },
    async onShow(root) {
      if (form) {
        await form.loadCatalogs();
        form.syncAdminFields();
      }
    },
  };
}

export function setNewRequestPageEnabled(enabled) {
  document.querySelector("#new-request-mount")?.classList.toggle("disabled", !enabled);
}
