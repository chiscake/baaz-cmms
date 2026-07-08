import { createClient } from "https://esm.sh/@supabase/supabase-js@2.108.2";
import { defaultConfig } from "./config.js";

let client = null;
let currentProfile = null;

export function getSupabase() {
  if (!client) {
    throw new Error("Supabase client is not initialized");
  }
  return client;
}

export function getCurrentProfile() {
  return currentProfile;
}

export function isAdmin() {
  return currentProfile?.role === "admin";
}

export function isDispatcherOrAdmin() {
  return currentProfile?.role === "admin" || currentProfile?.role === "dispatcher";
}

export async function initSupabase(url, publishableKey) {
  client = createClient(url, publishableKey, {
    auth: {
      persistSession: true,
      autoRefreshToken: true,
    },
  });

  const { data } = await client.auth.getSession();
  if (data.session) {
    await refreshProfile();
  }

  return client;
}

export async function signIn(email, password) {
  const supabase = getSupabase();
  const { data, error } = await supabase.auth.signInWithPassword({
    email: email.trim(),
    password,
  });

  if (error) {
    throw error;
  }

  await refreshProfile();
  return data.session;
}

export async function signOut() {
  const supabase = getSupabase();
  const { error } = await supabase.auth.signOut();
  if (error) {
    throw error;
  }
  currentProfile = null;
}

async function refreshProfile() {
  const supabase = getSupabase();
  const { data: userData, error: userError } = await supabase.auth.getUser();
  if (userError || !userData.user) {
    currentProfile = null;
    return null;
  }

  const { data, error } = await supabase
    .from("profiles")
    .select("id, full_name, role, repair_department_id")
    .eq("id", userData.user.id)
    .maybeSingle();

  if (error) {
    throw error;
  }

  currentProfile = data
    ? { ...data, email: userData.user.email ?? "" }
    : null;
  return currentProfile;
}

export function getDefaultEndpointInputs() {
  return {
    url: localStorage.getItem("debug.supabaseUrl") ?? defaultConfig.supabaseUrl,
    key:
      localStorage.getItem("debug.supabasePublishableKey") ??
      defaultConfig.supabasePublishableKey,
    email: localStorage.getItem("debug.email") ?? defaultConfig.defaultEmail,
    password:
      localStorage.getItem("debug.password") ?? defaultConfig.defaultPassword,
  };
}

export function saveEndpointInputs({ url, key, email, password }) {
  localStorage.setItem("debug.supabaseUrl", url);
  localStorage.setItem("debug.supabasePublishableKey", key);
  localStorage.setItem("debug.email", email);
  if (password) {
    localStorage.setItem("debug.password", password);
  }
}
