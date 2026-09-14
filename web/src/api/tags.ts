import type { TagDto } from "../types";
import { apiRequest } from "./client";

export function getTags() {
  return apiRequest<TagDto[]>("/api/tags");
}

export function createTag(name: string) {
  return apiRequest<TagDto>("/api/tags", { method: "POST", body: { name } });
}

export function updateTag(id: string, name: string) {
  return apiRequest<void>(`/api/tags/${id}`, { method: "PUT", body: { name } });
}

export function deleteTag(id: string) {
  return apiRequest<void>(`/api/tags/${id}`, { method: "DELETE" });
}
