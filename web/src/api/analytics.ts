import { apiRequest } from "./client";

export type AnalyticsSummary = {
  total: number;
  new: number;
  inProgress: number;
  done: number;
  cancelled: number;
  averageCompletionDays: number | null;
};

export type ProjectAnalytics = {
  projectId: string;
  name: string;
  total: number;
  new: number;
  inProgress: number;
  done: number;
  cancelled: number;
};

export function getAnalyticsSummary() {
  return apiRequest<AnalyticsSummary>("/api/analytics/summary");
}

export function getProjectAnalytics() {
  return apiRequest<ProjectAnalytics[]>("/api/analytics/projects");
}
