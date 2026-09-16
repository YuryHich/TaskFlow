import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getProject } from "../api/projects";
import { getTask } from "../api/tasks";
import { useAuth } from "../auth/AuthContext";
import { apiBase, getAccessToken } from "../auth/session";
import { useToasts } from "../components/Toasts";
import type { ProjectDto, RealtimeNotification, TaskDto } from "../types";
import { describeNotify } from "./describeNotify";
import { consumeOwnRealtime } from "./ownRealtime";

function findProject(queryClient: ReturnType<typeof useQueryClient>, projectId: string) {
  return (
    queryClient.getQueryData<ProjectDto>(["project", projectId]) ??
    queryClient.getQueryData<ProjectDto[]>(["projects"])?.find((item) => item.id === projectId)
  );
}

function findTask(queryClient: ReturnType<typeof useQueryClient>, projectId: string, taskId?: string | null) {
  if (!taskId) return undefined;
  return (
    queryClient.getQueryData<TaskDto>(["task", taskId]) ??
    queryClient.getQueryData<TaskDto[]>(["project-tasks", projectId])?.find((item) => item.id === taskId)
  );
}

export function HubSubscriber() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const { pushToast } = useToasts();
  const { userId } = useAuth();
  const token = getAccessToken();
  const pathRef = useRef(location.pathname);
  pathRef.current = location.pathname;

  useEffect(() => {
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBase()}/hubs/notifications`, {
        accessTokenFactory: () => getAccessToken() ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("Notify", (notification: RealtimeNotification) => {
      void (async () => {
        const { eventName, projectId, taskId } = notification;
        const before = {
          project: findProject(queryClient, projectId),
          task: findTask(queryClient, projectId, taskId),
        };
        const skipToast = consumeOwnRealtime(eventName, projectId, taskId);

        if (eventName.startsWith("project.")) {
          await Promise.all([
            queryClient.invalidateQueries({ queryKey: ["projects"] }),
            queryClient.invalidateQueries({ queryKey: ["project", projectId] }),
            queryClient.invalidateQueries({ queryKey: ["project-tasks", projectId] }),
          ]);
          if (eventName === "project.deleted" && pathRef.current.startsWith(`/projects/${projectId}`)) {
            navigate("/projects");
          }
        }
        if (eventName.startsWith("task.")) {
          await Promise.all([
            queryClient.invalidateQueries({ queryKey: ["project-tasks", projectId] }),
            taskId ? queryClient.invalidateQueries({ queryKey: ["task", taskId] }) : Promise.resolve(),
          ]);
        }
        if (eventName.startsWith("comment.")) {
          if (taskId) await queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] });
        }

        if (skipToast) return;

        const after = {
          project:
            eventName.startsWith("project.") && eventName !== "project.deleted"
              ? await queryClient
                  .fetchQuery({ queryKey: ["project", projectId], queryFn: () => getProject(projectId) })
                  .catch(() => before.project)
              : before.project,
          task:
            taskId && eventName !== "task.deleted"
              ? await queryClient
                  .fetchQuery({ queryKey: ["task", taskId], queryFn: () => getTask(taskId) })
                  .catch(() => before.task)
              : before.task,
        };

        const toast = describeNotify(notification, userId, before, after);
        if (toast) pushToast(toast);
      })();
    });

    void connection.start();

    return () => {
      connection.off("Notify");
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [token, queryClient, navigate, pushToast, userId]);

  return null;
}
