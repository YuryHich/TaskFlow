import type { CommentDto } from "../types";
import { apiRequest } from "./client";

export function createComment(taskId: string, content: string, authorId: string) {
  return apiRequest<CommentDto>("/api/comments", {
    method: "POST",
    body: { taskId, content, authorId },
  });
}

export function updateComment(id: string, content: string) {
  return apiRequest<void>(`/api/comments/${id}`, { method: "PUT", body: { content } });
}

export function deleteComment(id: string) {
  return apiRequest<void>(`/api/comments/${id}`, { method: "DELETE" });
}
