import type { CommentDto, TaskDto, TaskPriorityValue, TaskStateValue } from "../types";
import { apiRequest } from "./client";

export type TaskWrite = {
  title: string;
  description?: string;
  status: TaskStateValue;
  priority: TaskPriorityValue;
  deadline?: string | null;
  assigneeIds: string[];
};

export function getTask(id: string) {
  return apiRequest<TaskDto>(`/api/tasks/${id}`);
}

export function getTaskComments(taskId: string) {
  return apiRequest<CommentDto[]>(`/api/tasks/${taskId}/comments`);
}

export function createTask(body: TaskWrite & { projectId: string }) {
  return apiRequest<TaskDto>("/api/tasks", { method: "POST", body });
}

export function updateTask(id: string, body: TaskWrite) {
  return apiRequest<void>(`/api/tasks/${id}`, { method: "PUT", body });
}

export function deleteTask(id: string) {
  return apiRequest<void>(`/api/tasks/${id}`, { method: "DELETE" });
}
