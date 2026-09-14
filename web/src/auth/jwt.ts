import type { Role } from "../types";

const ROLE_KEYS = [
  "role",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
];

const NAME_KEYS = [
  "nameid",
  "sub",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
];

function decodePayload(token: string): Record<string, unknown> {
  const part = token.split(".")[1];
  if (!part) return {};
  const padded = part.replace(/-/g, "+").replace(/_/g, "/");
  const json = atob(padded);
  return JSON.parse(json) as Record<string, unknown>;
}

function firstString(payload: Record<string, unknown>, keys: string[]): string | null {
  for (const key of keys) {
    const value = payload[key];
    if (typeof value === "string" && value.length > 0) return value;
    if (Array.isArray(value) && typeof value[0] === "string") return value[0];
  }
  return null;
}

export function parseAccessToken(token: string): { userId: string; role: Role; email: string | null } {
  const payload = decodePayload(token);
  const userId = firstString(payload, NAME_KEYS);
  const role = firstString(payload, ROLE_KEYS) as Role | null;
  const email =
    firstString(payload, [
      "email",
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
    ]);
  if (!userId || !role) {
    throw new Error("Access token is missing user id or role.");
  }
  return { userId, role, email };
}
