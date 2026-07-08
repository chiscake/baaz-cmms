/** @typedef {{ label: string, mount: (root: HTMLElement) => void | Promise<void>, onShow?: () => void | Promise<void>, onHide?: () => void }} PageDefinition */

export class AppRouter {
  /** @param {HTMLElement} navRoot @param {HTMLElement} contentRoot */
  constructor(navRoot, contentRoot) {
    this.navRoot = navRoot;
    this.contentRoot = contentRoot;
    /** @type {Map<string, PageDefinition & { root?: HTMLElement }>} */
    this.pages = new Map();
    this.activeId = null;
  }

  /** @param {string} id @param {PageDefinition} definition */
  register(id, definition) {
    this.pages.set(id, definition);
  }

  buildNav(defaultId) {
    this.navRoot.replaceChildren();

    for (const [id, page] of this.pages) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "nav-item";
      button.dataset.pageId = id;
      button.textContent = page.label;
      button.addEventListener("click", () => {
        void this.navigate(id);
      });
      this.navRoot.append(button);
    }

    void this.navigate(defaultId);
  }

  /** @param {string} id */
  async navigate(id) {
    if (!this.pages.has(id)) {
      return;
    }

    const previous = this.activeId ? this.pages.get(this.activeId) : null;
    previous?.onHide?.();

    this.activeId = id;

    for (const button of this.navRoot.querySelectorAll(".nav-item")) {
      button.classList.toggle(
        "is-active",
        button instanceof HTMLElement && button.dataset.pageId === id,
      );
    }

    this.contentRoot.replaceChildren();

    const page = this.pages.get(id);
    if (!page) {
      return;
    }

    const root = document.createElement("section");
    root.className = "page-section";
    this.contentRoot.append(root);
    page.root = root;

    await page.mount(root);
    await page.onShow?.(root);
  }

  /** @param {string | null} id */
  async refresh(id = this.activeId) {
    if (!id) {
      return;
    }
    const page = this.pages.get(id);
    if (page?.root) {
      await page.onShow?.(page.root);
    }
  }
}
