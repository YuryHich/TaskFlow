type OwnMark = {
  key: string;
  at: number;
};

const recent: OwnMark[] = [];
const TTL_MS = 8000;

function keyOf(eventName: string, projectId: string, taskId?: string | null) {
  return `${eventName}:${projectId}:${taskId ?? ""}`;
}

export function rememberOwnRealtime(eventName: string, projectId: string, taskId?: string | null) {
  recent.push({ key: keyOf(eventName, projectId, taskId), at: Date.now() });
}

export function consumeOwnRealtime(eventName: string, projectId: string, taskId?: string | null) {
  const key = keyOf(eventName, projectId, taskId);
  const now = Date.now();
  const index = recent.findIndex((item) => item.key === key && now - item.at < TTL_MS);
  if (index === -1) return false;
  recent.splice(index, 1);
  return true;
}
