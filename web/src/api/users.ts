import type { UserDto } from "../types";
import { apiRequest } from "./client";

export function getMe() {
  return apiRequest<UserDto>("/api/users/me");
}

export function getDirectory() {
  return apiRequest<UserDto[]>("/api/users/directory");
}

export function getUsers() {
  return apiRequest<UserDto[]>("/api/users");
}

export function updateUser(id: string, body: { email: string; username: string }) {
  return apiRequest<void>(`/api/users/${id}`, { method: "PUT", body });
}

export function deleteUser(id: string) {
  return apiRequest<void>(`/api/users/${id}`, { method: "DELETE" });
}
