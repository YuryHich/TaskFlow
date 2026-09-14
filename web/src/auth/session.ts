import type { TokenResponse } from "../types";

const REFRESH_KEY = "taskflow.refreshToken";
const EXPIRES_KEY = "taskflow.expiresAt";
const LOGOUT_EVENT = "taskflow:logout";

let accessToken: string | null = null;
let refreshPromise: Promise<boolean> | null = null;

export function apiBase(): string {
  return import.meta.env.VITE_API_URL ?? "";
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function getRefreshToken(): string | null {
  return sessionStorage.getItem(REFRESH_KEY);
}

export function getExpiresAtMs(): number | null {
  const raw = sessionStorage.getItem(EXPIRES_KEY);
  if (!raw) return null;
  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
}

export function setSession(tokens: TokenResponse): void {
  accessToken = tokens.accessToken;
  sessionStorage.setItem(REFRESH_KEY, tokens.refreshToken);
  sessionStorage.setItem(EXPIRES_KEY, String(new Date(tokens.expiresAt).getTime()));
}

export function clearSession(): void {
  accessToken = null;
  sessionStorage.removeItem(REFRESH_KEY);
  sessionStorage.removeItem(EXPIRES_KEY);
}

export function notifyLoggedOut(): void {
  window.dispatchEvent(new Event(LOGOUT_EVENT));
}

export function onLoggedOut(handler: () => void): () => void {
  window.addEventListener(LOGOUT_EVENT, handler);
  return () => window.removeEventListener(LOGOUT_EVENT, handler);
}

async function doRefresh(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;

  const response = await fetch(`${apiBase()}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    clearSession();
    notifyLoggedOut();
    return false;
  }

  const tokens = (await response.json()) as TokenResponse;
  setSession(tokens);
  return true;
}

export function refreshSession(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = doRefresh().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

export async function ensureFreshAccess(skewMs = 60_000): Promise<void> {
  const expiresAt = getExpiresAtMs();
  if (accessToken && expiresAt && expiresAt - skewMs > Date.now()) return;
  if (getRefreshToken()) await refreshSession();
}
