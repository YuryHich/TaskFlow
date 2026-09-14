import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { apiBase, getAccessToken } from "../auth/session";
import type { RealtimeNotification } from "../types";

export function HubSubscriber() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const token = getAccessToken();

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
      const { eventName, projectId, taskId } = notification;
      if (eventName.startsWith("project.")) {
        void queryClient.invalidateQueries({ queryKey: ["projects"] });
        void queryClient.invalidateQueries({ queryKey: ["project", projectId] });
        void queryClient.invalidateQueries({ queryKey: ["project-tasks", projectId] });
        if (eventName === "project.deleted" && location.pathname.startsWith(`/projects/${projectId}`)) {
          navigate("/projects");
        }
      }
      if (eventName.startsWith("task.")) {
        void queryClient.invalidateQueries({ queryKey: ["project-tasks", projectId] });
        if (taskId) void queryClient.invalidateQueries({ queryKey: ["task", taskId] });
      }
      if (eventName.startsWith("comment.")) {
        if (taskId) void queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] });
      }
    });

    void connection.start();

    return () => {
      connection.off("Notify");
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [token, queryClient, navigate, location.pathname]);

  return null;
}
