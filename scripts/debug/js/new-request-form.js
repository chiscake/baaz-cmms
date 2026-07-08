import { defaultConfig } from "./config.js";
import { enhanceSelects } from "./combobox.js";
import {
  labels,
  repairZones,
  requestPriorities,
  requestTypes,
} from "./labels.js";
import { generateRequestNumber, pickRandom } from "./random.js";
import { getCurrentProfile, getSupabase, isAdmin } from "./supabase-client.js";
import { titleTemplates } from "./title-templates.js";

/** @typedef {{ id: string, asset_number: string, name: string, locationName: string | null, displayName: string }} AssetRow */
/** @typedef {{ id: string, name: string, code: string }} RepairDepartmentRow */

export class NewRequestForm {
  /** @param {{ onLog: (message: string, kind?: string) => void, onSuccess?: (payload: object) => void }} hooks */
  constructor(hooks) {
    this.hooks = hooks;
    /** @type {AssetRow[]} */
    this.assets = [];
    /** @type {RepairDepartmentRow[]} */
    this.departments = [];
    this.selectedAssetId = null;
    this.subjectMode = "asset";
  }

  /** @param {HTMLElement} root */
  mount(root) {
    this.root = root;
    root.innerHTML = this.renderShell();
    this.bindElements();
    this.populateStaticSelects();
    this.bindEvents();
    this.syncAdminFields();
    this.applyRandomTitle();
    enhanceSelects(this.root);
  }

  renderShell() {
    return `
      <form id="new-request-form" class="stack">
        <div class="field">
          <label for="nr-type">${labels.type}</label>
          <select id="nr-type" name="type"></select>
        </div>

        <div class="field">
          <label for="nr-priority">${labels.priority}</label>
          <select id="nr-priority" name="priority"></select>
        </div>

        <div class="field">
          <label for="nr-department">${labels.repairDepartment}</label>
          <select id="nr-department" name="department" required>
            <option value="">${labels.repairDepartmentPlaceholder}</option>
          </select>
        </div>

        <fieldset class="subject-fieldset">
          <legend>${labels.subject}</legend>
          <div class="segmented" role="tablist" aria-label="${labels.subject}">
            <label class="segmented-item">
              <input type="radio" name="subject-mode" value="asset" checked />
              <span>${labels.subjectAsset}</span>
            </label>
            <label class="segmented-item">
              <input type="radio" name="subject-mode" value="location" />
              <span>${labels.subjectLocation}</span>
            </label>
          </div>

          <div class="field subject-asset">
            <label for="nr-asset-search">${labels.subjectAsset}</label>
            <div class="inline-actions">
              <input id="nr-asset-search" type="search" list="nr-asset-list"
                     placeholder="${labels.assetPlaceholder}" autocomplete="off" />
              <button type="button" id="nr-random-asset" class="secondary">${labels.randomAsset}</button>
            </div>
            <datalist id="nr-asset-list"></datalist>
            <p id="nr-asset-hint" class="hint" hidden></p>
          </div>

          <div class="field subject-location" hidden>
            <label for="nr-location">${labels.locationPlaceholder}</label>
            <textarea id="nr-location" rows="3"
                      placeholder="${labels.locationPlaceholder}"></textarea>
          </div>
        </fieldset>

        <div class="field">
          <label for="nr-title">${labels.title}</label>
          <div class="inline-actions">
            <input id="nr-title" name="title" type="text" required />
            <button type="button" id="nr-random-title" class="secondary">${labels.randomTitle}</button>
          </div>
        </div>

        <div class="field">
          <label for="nr-description">${labels.description}</label>
          <textarea id="nr-description" name="description" rows="5"></textarea>
        </div>

        <div id="nr-admin-fields" class="stack admin-fields" hidden>
          <div class="field">
            <label for="nr-repair-zone">${labels.repairZone}</label>
            <select id="nr-repair-zone" name="repairZone"></select>
          </div>
          <div class="field" id="nr-contractor-wrap" hidden>
            <label for="nr-contractor">${labels.contractorName}</label>
            <input id="nr-contractor" type="text" placeholder="${labels.contractorPlaceholder}" />
          </div>
        </div>

        <button type="submit" id="nr-submit">${labels.submit}</button>
      </form>
    `;
  }

  bindElements() {
    this.form = /** @type {HTMLFormElement} */ (this.root.querySelector("#new-request-form"));
    this.typeSelect = /** @type {HTMLSelectElement} */ (this.root.querySelector("#nr-type"));
    this.prioritySelect = /** @type {HTMLSelectElement} */ (this.root.querySelector("#nr-priority"));
    this.departmentSelect = /** @type {HTMLSelectElement} */ (this.root.querySelector("#nr-department"));
    this.assetSearch = /** @type {HTMLInputElement} */ (this.root.querySelector("#nr-asset-search"));
    this.assetList = /** @type {HTMLDataListElement} */ (this.root.querySelector("#nr-asset-list"));
    this.assetHint = /** @type {HTMLParagraphElement} */ (this.root.querySelector("#nr-asset-hint"));
    this.locationInput = /** @type {HTMLTextAreaElement} */ (this.root.querySelector("#nr-location"));
    this.titleInput = /** @type {HTMLInputElement} */ (this.root.querySelector("#nr-title"));
    this.descriptionInput = /** @type {HTMLTextAreaElement} */ (this.root.querySelector("#nr-description"));
    this.adminFields = /** @type {HTMLElement} */ (this.root.querySelector("#nr-admin-fields"));
    this.repairZoneSelect = /** @type {HTMLSelectElement} */ (this.root.querySelector("#nr-repair-zone"));
    this.contractorWrap = /** @type {HTMLElement} */ (this.root.querySelector("#nr-contractor-wrap"));
    this.contractorInput = /** @type {HTMLInputElement} */ (this.root.querySelector("#nr-contractor"));
    this.submitButton = /** @type {HTMLButtonElement} */ (this.root.querySelector("#nr-submit"));
    this.subjectAssetBlock = /** @type {HTMLElement} */ (this.root.querySelector(".subject-asset"));
    this.subjectLocationBlock = /** @type {HTMLElement} */ (this.root.querySelector(".subject-location"));
  }

  populateStaticSelects() {
    for (const item of requestTypes) {
      this.typeSelect.add(new Option(item.label, item.value));
    }

    for (const item of requestPriorities) {
      this.prioritySelect.add(new Option(item.label, item.value));
    }
    this.prioritySelect.value = "normal";

    for (const item of repairZones) {
      this.repairZoneSelect.add(new Option(item.label, item.value));
    }
    this.repairZoneSelect.value = "on_site";
  }

  bindEvents() {
    this.form.addEventListener("submit", (event) => {
      event.preventDefault();
      void this.submit();
    });

    this.root.querySelector("#nr-random-title")?.addEventListener("click", () => {
      this.applyRandomTitle();
    });

    this.root.querySelector("#nr-random-asset")?.addEventListener("click", () => {
      this.applyRandomAsset();
    });

    this.assetSearch.addEventListener("input", () => {
      this.syncAssetSelectionFromInput();
    });

    this.repairZoneSelect.addEventListener("change", () => {
      this.syncContractorVisibility();
    });

    for (const radio of this.root.querySelectorAll('input[name="subject-mode"]')) {
      radio.addEventListener("change", (event) => {
        const target = /** @type {HTMLInputElement} */ (event.target);
        if (target.checked) {
          this.setSubjectMode(target.value === "location" ? "location" : "asset");
        }
      });
    }
  }

  setSubjectMode(mode) {
    this.subjectMode = mode;
    const isAsset = mode === "asset";
    this.subjectAssetBlock.hidden = !isAsset;
    this.subjectLocationBlock.hidden = isAsset;

    if (isAsset) {
      this.locationInput.value = "";
    } else {
      this.clearAssetSelection();
    }
  }

  syncAdminFields() {
    const show = isAdmin();
    this.adminFields.hidden = !show;
    if (!show) {
      this.repairZoneSelect.value = "on_site";
      this.contractorInput.value = "";
      this.contractorWrap.hidden = true;
    } else {
      this.syncContractorVisibility();
    }
  }

  syncContractorVisibility() {
    const external = this.repairZoneSelect.value === "external";
    this.contractorWrap.hidden = !external;
    if (!external) {
      this.contractorInput.value = "";
    }
  }

  applyRandomTitle() {
    const template = pickRandom(titleTemplates);
    if (!template) {
      return;
    }
    this.titleInput.value = template.title;
    this.descriptionInput.value = template.description;
  }

  applyRandomAsset() {
    if (this.assets.length === 0) {
      this.hooks.onLog("Список оборудования пуст — войдите и дождитесь загрузки.", "warn");
      return;
    }

    const asset = pickRandom(this.assets);
    if (!asset) {
      return;
    }
    this.selectAsset(asset);
    this.setSubjectMode("asset");
    const assetRadio = /** @type {HTMLInputElement | null} */ (
      this.root.querySelector('input[name="subject-mode"][value="asset"]')
    );
    if (assetRadio) {
      assetRadio.checked = true;
    }
  }

  /** @param {AssetRow} asset */
  selectAsset(asset) {
    this.selectedAssetId = asset.id;
    this.assetSearch.value = asset.displayName;
    this.assetHint.textContent = asset.locationName
      ? `Локация: ${asset.locationName}`
      : "";
    this.assetHint.hidden = !asset.locationName;
  }

  clearAssetSelection() {
    this.selectedAssetId = null;
    this.assetSearch.value = "";
    this.assetHint.textContent = "";
    this.assetHint.hidden = true;
  }

  syncAssetSelectionFromInput() {
    const text = this.assetSearch.value.trim();
    const match = this.assets.find((asset) => asset.displayName === text);
    if (match) {
      this.selectAsset(match);
      return;
    }

    const partial = this.assets.filter((asset) =>
      asset.displayName.toLowerCase().includes(text.toLowerCase()),
    );
    if (partial.length === 1 && text.length > 2) {
      this.selectAsset(partial[0]);
      return;
    }

    this.selectedAssetId = null;
    this.assetHint.hidden = true;
  }

  async loadCatalogs() {
    const supabase = getSupabase();
    this.submitButton.disabled = true;

    try {
      const [assetsResult, departmentsResult] = await Promise.all([
        supabase
          .from("assets")
          .select("id, asset_number, name, location_id, locations(name)")
          .eq("status", "active")
          .order("asset_number"),
        supabase
          .from("repair_departments")
          .select("id, name, code")
          .eq("is_active", true)
          .order("name"),
      ]);

      if (assetsResult.error) {
        throw assetsResult.error;
      }
      if (departmentsResult.error) {
        throw departmentsResult.error;
      }

      this.assets = (assetsResult.data ?? []).map((row) => {
        const locationName = row.locations?.name ?? null;
        const locationSuffix = locationName ? ` — ${locationName}` : "";
        return {
          id: row.id,
          asset_number: row.asset_number,
          name: row.name,
          locationName,
          displayName: `${row.asset_number} — ${row.name}${locationSuffix}`,
        };
      });

      this.departments = departmentsResult.data ?? [];
      this.renderAssetOptions();
      this.renderDepartmentOptions();
      this.syncAdminFields();
      enhanceSelects(this.root);

      this.hooks.onLog(
        `Справочники: ${this.assets.length} ед. оборудования, ${this.departments.length} отделов.`,
        "info",
      );
    } finally {
      this.submitButton.disabled = false;
    }
  }

  renderAssetOptions() {
    this.assetList.replaceChildren();
    for (const asset of this.assets) {
      this.assetList.append(new Option(asset.displayName, asset.displayName));
    }
  }

  renderDepartmentOptions() {
    const previous = this.departmentSelect.value;
    this.departmentSelect.replaceChildren(
      new Option(labels.repairDepartmentPlaceholder, ""),
    );

    for (const department of this.departments) {
      this.departmentSelect.add(new Option(department.name, department.id));
    }

    const defaultDept = this.departments.find(
      (item) => item.code === defaultConfig.defaultRepairDepartmentCode,
    );
    if (defaultDept) {
      this.departmentSelect.value = defaultDept.id;
    } else if (previous) {
      this.departmentSelect.value = previous;
    }
  }

  resetAfterSubmit() {
    this.applyRandomTitle();
    this.applyRandomAsset();
    this.locationInput.value = "";
    this.contractorInput.value = "";
    this.repairZoneSelect.value = "on_site";
    this.syncContractorVisibility();
  }

  async submit() {
    const profile = getCurrentProfile();
    if (!profile) {
      this.hooks.onLog("Требуется вход в систему.", "error");
      return;
    }

    const title = this.titleInput.value.trim();
    if (!title) {
      this.hooks.onLog("Заполните краткое описание.", "warn");
      return;
    }

    let assetId = null;
    let locationDescription = "";

    if (this.subjectMode === "asset") {
      this.syncAssetSelectionFromInput();
      if (!this.selectedAssetId) {
        this.hooks.onLog("Выберите оборудование из списка.", "warn");
        return;
      }
      assetId = this.selectedAssetId;
    } else {
      locationDescription = this.locationInput.value.trim();
      if (!locationDescription) {
        this.hooks.onLog("Укажите место возникновения (режим «Инфраструктура»).", "warn");
        return;
      }
    }

    const departmentId = this.departmentSelect.value;
    if (!departmentId) {
      this.hooks.onLog("Выберите отдел ремонта.", "warn");
      return;
    }

    const payload = {
      p_request_number: generateRequestNumber(),
      p_type: this.typeSelect.value,
      p_priority: this.prioritySelect.value,
      p_title: title,
      p_description: this.descriptionInput.value.trim() || null,
      p_location_description: locationDescription,
      p_asset_id: assetId,
      p_target_repair_department_id: departmentId,
      p_repair_zone: "on_site",
      p_contractor_name: null,
    };

    if (isAdmin()) {
      payload.p_repair_zone = this.repairZoneSelect.value;
      if (payload.p_repair_zone === "external") {
        const contractor = this.contractorInput.value.trim();
        if (!contractor) {
          this.hooks.onLog("Укажите подрядчика для зоны «У внешнего подрядчика».", "warn");
          return;
        }
        payload.p_contractor_name = contractor;
      }
    }

    this.submitButton.disabled = true;
    try {
      const supabase = getSupabase();
      const { data, error } = await supabase.rpc("create_request", payload);
      if (error) {
        throw error;
      }

      const message = `Заявка создана: id=${data}, № ${payload.p_request_number}`;
      this.hooks.onLog(message, "success");
      this.hooks.onSuccess?.({ id: data, requestNumber: payload.p_request_number });
      this.resetAfterSubmit();
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      this.hooks.onLog(`Ошибка create_request: ${detail}`, "error");
    } finally {
      this.submitButton.disabled = false;
    }
  }
}
