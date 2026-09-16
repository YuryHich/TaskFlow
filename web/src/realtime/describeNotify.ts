import type { ToastItem } from "../components/Toasts";
import type { ProjectDto, RealtimeNotification, TaskDto } from "../types";

type Snapshot = {
  project?: ProjectDto;
  task?: TaskDto;
};

export function describeNotify(
  notification: RealtimeNotification,
  userId: string | null,
  before: Snapshot,
  after: Snapshot,
): Omit<ToastItem, "id"> | null {
  const { eventName, projectId, taskId } = notification;
  const projectName = after.project?.name ?? before.project?.name;
  const taskTitle = after.task?.title ?? before.task?.title;
  const projectHref = `/projects/${projectId}`;
  const taskHref = taskId ? `/projects/${projectId}/tasks/${taskId}` : projectHref;

  const becameOwner =
    !!userId && after.project?.ownerId === userId && before.project?.ownerId !== userId;
  const isAssignee = !!userId && !!after.task?.assigneeIds.includes(userId);
  const wasAssignee = !!userId && !!before.task?.assigneeIds.includes(userId);
  const becameAssignee = isAssignee && !wasAssignee;

  switch (eventName) {
    case "project.created":
      if (becameOwner || after.project?.ownerId === userId) {
        return {
          title: "You were assigned as project owner",
          body: projectName,
          href: projectHref,
        };
      }
      return { title: "Project created", body: projectName, href: projectHref };
    case "project.updated":
      if (becameOwner) {
        return {
          title: "You were assigned as project owner",
          body: projectName,
          href: projectHref,
        };
      }
      return { title: "Project updated", body: projectName, href: projectHref };
    case "project.deleted":
      return { title: "Project deleted", body: projectName, tone: "warn" };
    case "task.created":
      if (becameAssignee) {
        return { title: "You were assigned to a task", body: taskTitle, href: taskHref };
      }
      return { title: "Task created", body: taskTitle, href: taskHref };
    case "task.updated":
      if (becameAssignee) {
        return { title: "You were assigned to a task", body: taskTitle, href: taskHref };
      }
      return { title: "Task updated", body: taskTitle, href: taskHref };
    case "task.deleted":
      return { title: "Task deleted", body: taskTitle, href: projectHref, tone: "warn" };
    case "comment.added":
      return { title: "New comment", body: taskTitle ? `On “${taskTitle}”` : undefined, href: taskHref };
    case "comment.updated":
      return { title: "Comment updated", body: taskTitle, href: taskHref };
    case "comment.deleted":
      return { title: "Comment deleted", body: taskTitle, href: taskHref, tone: "warn" };
    default:
      return { title: "Update", body: eventName, href: projectHref };
  }
}
