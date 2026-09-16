export type Role = "Admin" | "Manager" | "Developer";

export const TaskState = {
  New: 0,
  InProgress: 1,
  Done: 2,
  Cancelled: 3,
} as const;

export const TaskPriority = {
  Low: 0,
  Medium: 1,
  High: 2,
  Critical: 3,
} as const;

export type TaskStateValue = (typeof TaskState)[keyof typeof TaskState];
export type TaskPriorityValue = (typeof TaskPriority)[keyof typeof TaskPriority];

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
};

export type UserDto = {
  id: string;
  email: string;
  username: string;
  createdAt: string;
};

export type ProjectDto = {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
  ownerId: string;
};

export type TaskDto = {
  id: string;
  projectId: string;
  assigneeIds: string[];
  title: string;
  description?: string | null;
  status: TaskStateValue;
  priority: TaskPriorityValue;
  createdAt: string;
  deadline?: string | null;
};

export type CommentDto = {
  id: string;
  taskId: string;
  authorId: string;
  content: string;
  createdAt: string;
};

export type TagDto = {
  id: string;
  name: string;
};

export type RealtimeNotification = {
  eventName: string;
  projectId: string;
  taskId?: string | null;
  commentId?: string | null;
};

export type ProblemDetails = {
  status?: number;
  title?: string;
  detail?: string;
};
