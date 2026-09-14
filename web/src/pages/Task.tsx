import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { createComment, deleteComment, updateComment } from "../api/comments";
import { ApiError } from "../api/client";
import { getProject } from "../api/projects";
import { deleteTask, getTask, getTaskComments, updateTask } from "../api/tasks";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";
import { userLabel, useDirectoryMap } from "../components/useDirectory";
import { TaskPriority, TaskState, type TaskPriorityValue, type TaskStateValue } from "../types";

const statusLabels: Record<TaskStateValue, string> = {
  0: "New",
  1: "In progress",
  2: "Done",
  3: "Cancelled",
};

function toLocalInput(iso?: string | null): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function TaskPage() {
  const { id = "", taskId = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isStaff, userId, user } = useAuth();
  const directory = useDirectoryMap();

  const project = useQuery({ queryKey: ["project", id], queryFn: () => getProject(id), enabled: !!id });
  const task = useQuery({ queryKey: ["task", taskId], queryFn: () => getTask(taskId), enabled: !!taskId });
  const comments = useQuery({
    queryKey: ["task-comments", taskId],
    queryFn: () => getTaskComments(taskId),
    enabled: !!taskId,
  });

  const canMutateTask = isStaff || project.data?.ownerId === userId;

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [status, setStatus] = useState<TaskStateValue>(TaskState.New);
  const [priority, setPriority] = useState<TaskPriorityValue>(TaskPriority.Medium);
  const [deadline, setDeadline] = useState("");
  const [assignees, setAssignees] = useState<string[]>([]);
  const [comment, setComment] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingText, setEditingText] = useState("");

  useEffect(() => {
    if (!task.data) return;
    setTitle(task.data.title);
    setDescription(task.data.description ?? "");
    setStatus(task.data.status);
    setPriority(task.data.priority);
    setDeadline(toLocalInput(task.data.deadline));
    setAssignees([...task.data.assigneeIds]);
  }, [task.data]);

  const save = useMutation({
    mutationFn: () =>
      updateTask(taskId, {
        title,
        description: description || undefined,
        status,
        priority,
        deadline: deadline ? new Date(deadline).toISOString() : null,
        assigneeIds: assignees,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["task", taskId] });
      await queryClient.invalidateQueries({ queryKey: ["project-tasks", id] });
    },
  });

  const remove = useMutation({
    mutationFn: () => deleteTask(taskId),
    onSuccess: () => navigate(`/projects/${id}`),
  });

  const addComment = useMutation({
    mutationFn: () => createComment(taskId, comment, user?.id ?? ""),
    onSuccess: async () => {
      setComment("");
      await queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] });
    },
  });

  const saveComment = useMutation({
    mutationFn: () => updateComment(editingId!, editingText),
    onSuccess: async () => {
      setEditingId(null);
      await queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] });
    },
  });

  const removeComment = useMutation({
    mutationFn: (commentId: string) => deleteComment(commentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["task-comments", taskId] });
    },
  });

  if (task.isError) {
    const statusCode = task.error instanceof ApiError ? task.error.status : 0;
    return (
      <section>
        <ErrorBanner error={task.error} />
        {statusCode === 403 && <p className="muted">You are not allowed to open this task.</p>}
        {statusCode === 404 && <p className="muted">Task was not found.</p>}
        <Link to={`/projects/${id}`}>Back to project</Link>
      </section>
    );
  }

  if (!task.data) return <p className="muted">Loading task…</p>;

  return (
    <section>
      <p>
        <Link to={`/projects/${id}`}>← {project.data?.name ?? "Project"}</Link>
      </p>
      <h1>{task.data.title}</h1>
      <ErrorBanner error={save.error ?? remove.error ?? addComment.error ?? saveComment.error} />

      {canMutateTask ? (
        <form
          className="card stack"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          <label>
            Title
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
          </label>
          <label>
            Description
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label>
            Status
            <select value={status} onChange={(e) => setStatus(Number(e.target.value) as TaskStateValue)}>
              {Object.entries(statusLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Priority
            <select
              value={priority}
              onChange={(e) => setPriority(Number(e.target.value) as TaskPriorityValue)}
            >
              <option value={TaskPriority.Low}>Low</option>
              <option value={TaskPriority.Medium}>Medium</option>
              <option value={TaskPriority.High}>High</option>
              <option value={TaskPriority.Critical}>Critical</option>
            </select>
          </label>
          <label>
            Deadline
            <input type="datetime-local" value={deadline} onChange={(e) => setDeadline(e.target.value)} />
          </label>
          <fieldset>
            <legend>Assignees</legend>
            {(directory.data ?? []).map((person) => (
              <label key={person.id} className="check">
                <input
                  type="checkbox"
                  checked={assignees.includes(person.id)}
                  onChange={() =>
                    setAssignees((current) =>
                      current.includes(person.id)
                        ? current.filter((item) => item !== person.id)
                        : [...current, person.id],
                    )
                  }
                />
                {userLabel(directory.map, person.id)}
              </label>
            ))}
          </fieldset>
          <div className="row">
            <button type="submit" disabled={save.isPending}>
              Save task
            </button>
            <button type="button" className="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
              Delete task
            </button>
          </div>
        </form>
      ) : (
        <div className="card">
          <p>{task.data.description || "No description."}</p>
          <p className="muted">
            {statusLabels[task.data.status]} · assignees:{" "}
            {task.data.assigneeIds.length
              ? task.data.assigneeIds.map((assignee) => userLabel(directory.map, assignee)).join(", ")
              : "none"}
          </p>
        </div>
      )}

      <h2>Comments</h2>
      <ul className="list">
        {(comments.data ?? []).map((item) => (
          <li key={item.id}>
            {editingId === item.id ? (
              <form
                className="stack"
                onSubmit={(event: FormEvent) => {
                  event.preventDefault();
                  saveComment.mutate();
                }}
              >
                <textarea value={editingText} onChange={(e) => setEditingText(e.target.value)} required />
                <div className="row">
                  <button type="submit">Save</button>
                  <button type="button" className="ghost" onClick={() => setEditingId(null)}>
                    Cancel
                  </button>
                </div>
              </form>
            ) : (
              <>
                <p>{item.content}</p>
                <span className="muted">{userLabel(directory.map, item.authorId)}</span>
                <div className="row">
                  <button
                    type="button"
                    className="ghost"
                    onClick={() => {
                      setEditingId(item.id);
                      setEditingText(item.content);
                    }}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="ghost"
                    onClick={() => removeComment.mutate(item.id)}
                  >
                    Delete
                  </button>
                </div>
              </>
            )}
          </li>
        ))}
      </ul>

      <form
        className="card stack"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          addComment.mutate();
        }}
      >
        <label>
          New comment
          <textarea value={comment} onChange={(e) => setComment(e.target.value)} required />
        </label>
        <button type="submit" disabled={addComment.isPending}>
          Add comment
        </button>
      </form>
    </section>
  );
}
