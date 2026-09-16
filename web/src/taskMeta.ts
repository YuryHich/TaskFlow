import type { TaskStateValue } from "./types";

export const statusLabels: Record<TaskStateValue, string> = {
  0: "New",
  1: "In progress",
  2: "Done",
  3: "Cancelled",
};
