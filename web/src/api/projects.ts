import type { ProjectDto } from "../types";
import { apiRequest } from "./client";

export function getProjects() {
  return apiRequest<ProjectDto[]>("/api/projects");
}

export function getProject(id: string) {
  return apiRequest<ProjectDto>(`/api/projects/${id}`);
}

export function getProjectTasks(projectId: string) {
  return apiRequest<import("../types").TaskDto[]>(`/api/projects/${projectId}/tasks`);
}

export function createProject(body: { name: string; description?: string; ownerId?: string }) {
  return apiRequest<ProjectDto>("/api/projects", { method: "POST", body });
}

export function updateProject(
  id: string,
  body: { name: string; description?: string; ownerId?: string },
) {
  return apiRequest<void>(`/api/projects/${id}`, { method: "PUT", body });
}

export function deleteProject(id: string) {
  return apiRequest<void>(`/api/projects/${id}`, { method: "DELETE" });
}
