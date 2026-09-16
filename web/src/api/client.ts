import type { ProblemDetails } from "../types";
import {
  apiBase,
  clearSession,
  ensureFreshAccess,
  getAccessToken,
  notifyLoggedOut,
  refreshSession,
} from "../auth/session";

export class ApiError extends Error {
  readonly status: number;
  readonly title?: string;

  constructor(status: number, detail: string, title?: string) {
    super(detail);
    this.status = status;
    this.title = title;
  }
}

async function readProblem(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return new ApiError(
      response.status,
      problem.detail || problem.title || response.statusText,
      problem.title,
    );
  } catch {
    return new ApiError(response.status, response.statusText);
  }
}

type RequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown;
  skipAuth?: boolean;
};

export async function apiRequest<T>(
  path: string,
  options: RequestOptions = {},
  retried = false,
): Promise<T> {
  if (!options.skipAuth) await ensureFreshAccess();

  const headers = new Headers(options.headers);
  if (options.body !== undefined && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  const token = getAccessToken();
  if (!options.skipAuth && token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBase()}${path}`, {
    ...options,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 401 && !options.skipAuth && !retried) {
    const refreshed = await refreshSession();
    if (refreshed) return apiRequest<T>(path, options, true);
    clearSession();
    notifyLoggedOut();
    throw await readProblem(response);
  }

  if (response.status === 204) return undefined as T;

  if (!response.ok) throw await readProblem(response);

  if (response.status === 201 || response.headers.get("content-type")?.includes("json")) {
    return (await response.json()) as T;
  }

  return undefined as T;
}
