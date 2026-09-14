import type { TokenResponse } from "../types";
import { apiRequest } from "./client";

export function login(email: string, password: string) {
  return apiRequest<TokenResponse>("/api/auth/login", {
    method: "POST",
    body: { email, password },
    skipAuth: true,
  });
}

export function register(email: string, username: string, password: string) {
  return apiRequest<TokenResponse>("/api/auth/register", {
    method: "POST",
    body: { email, username, password },
    skipAuth: true,
  });
}

export function logout(refreshToken: string) {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST",
    body: { refreshToken },
    skipAuth: true,
  });
}
