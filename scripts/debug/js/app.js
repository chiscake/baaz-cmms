import {
  createNewRequestPage,
  setNewRequestPageEnabled,
} from "./pages/new-request-page.js";
import { createQuickActionsPage } from "./pages/quick-actions-page.js";
import { createQuickClosePage } from "./pages/quick-close-page.js";
import { AppRouter } from "./router.js";
import {
  getCurrentProfile,
  getDefaultEndpointInputs,
  initSupabase,
  saveEndpointInputs,
  signIn,
  signOut,
} from "./supabase-client.js";

/** @type {AppRouter | null} */
let router = null;

function $(selector) {
  return document.querySelector(selector);
}

function log(message, kind = "info") {
  const logEl = $("#log-output");
  if (!logEl) {
    return;
  }

  const line = document.createElement("div");
  line.className = `log-line log-${kind}`;
  const time = new Date().toLocaleTimeString("ru-RU");
  line.textContent = `[${time}] ${message}`;
  logEl.prepend(line);
}

function setSessionInfo(text) {
  const el = $("#session-info");
  if (el) {
    el.textContent = text;
  }
}

function setAppEnabled(enabled) {
  $("#app-content")?.classList.toggle("app-locked", !enabled);
  setNewRequestPageEnabled(enabled);

  const loginForm = /** @type {HTMLFormElement | null} */ ($("#login-form"));
  if (loginForm) {
    for (const element of loginForm.elements) {
      if (element instanceof HTMLInputElement || element instanceof HTMLButtonElement) {
        element.disabled = enabled;
      }
    }
  }
}

function initRouter() {
  const nav = $("#app-nav");
  const content = $("#app-content");
  if (!(nav instanceof HTMLElement) || !(content instanceof HTMLElement)) {
    return;
  }

  const ctx = { onLog: log };
  router = new AppRouter(nav, content);
  router.register("new-request", createNewRequestPage(ctx));
  router.register("quick-close", createQuickClosePage(ctx));
  router.register("quick-actions", createQuickActionsPage(ctx));
  router.buildNav("new-request");
}

async function handleConnectAndLogin(event) {
  event.preventDefault();

  const url = /** @type {HTMLInputElement} */ ($("#cfg-url")).value.trim();
  const key = /** @type {HTMLInputElement} */ ($("#cfg-key")).value.trim();
  const email = /** @type {HTMLInputElement} */ ($("#cfg-email")).value.trim();
  const password = /** @type {HTMLInputElement} */ ($("#cfg-password")).value;

  if (!url || !key || !email || !password) {
    log("Заполните URL, ключ и учётные данные.", "warn");
    return;
  }

  saveEndpointInputs({ url, key, email, password });

  try {
    await initSupabase(url, key);
    await signIn(email, password);
    await onAuthenticated();
    log(`Вход выполнен: ${email}`, "success");
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    log(`Не удалось войти: ${detail}`, "error");
    setSessionInfo("Не авторизован");
    setAppEnabled(false);
  }
}

async function handleSignOut() {
  try {
    await signOut();
    setSessionInfo("Не авторизован");
    setAppEnabled(false);
    log("Выход из сессии.", "info");
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    log(`Ошибка выхода: ${detail}`, "error");
  }
}

async function onAuthenticated() {
  const profile = getCurrentProfile();
  if (!profile) {
    setSessionInfo("Не авторизован");
    setAppEnabled(false);
    return;
  }

  setSessionInfo(`${profile.full_name ?? profile.email} (${profile.role})`);
  setAppEnabled(true);
  await router?.refresh();
}

function fillDefaults() {
  const defaults = getDefaultEndpointInputs();
  /** @type {HTMLInputElement} */ ($("#cfg-url")).value = defaults.url;
  /** @type {HTMLInputElement} */ ($("#cfg-key")).value = defaults.key;
  /** @type {HTMLInputElement} */ ($("#cfg-email")).value = defaults.email;
  /** @type {HTMLInputElement} */ ($("#cfg-password")).value = defaults.password;
}

document.addEventListener("DOMContentLoaded", () => {
  fillDefaults();
  initRouter();
  setAppEnabled(false);

  $("#login-form")?.addEventListener("submit", (event) => {
    void handleConnectAndLogin(event);
  });

  $("#btn-sign-out")?.addEventListener("click", () => {
    void handleSignOut();
  });

  void tryRestoreSession();

  log("Отладочная панель готова. Подключитесь к локальному Supabase и войдите.", "info");
});

async function tryRestoreSession() {
  const defaults = getDefaultEndpointInputs();
  if (!defaults.url || !defaults.key) {
    return;
  }

  try {
    await initSupabase(defaults.url, defaults.key);
    const profile = getCurrentProfile();
    if (profile) {
      await onAuthenticated();
      log(`Сессия восстановлена: ${profile.email}`, "success");
    }
  } catch {
    // ignore — пользователь войдёт вручную
  }
}
