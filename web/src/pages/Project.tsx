import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError } from "../api/client";
import { createTask } from "../api/tasks";
import { deleteProject, getProject, getProjectTasks, updateProject } from "../api/projects";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";
import { userLabel, useDirectoryMap } from "../components/useDirectory";
import { useViewMode, ViewToggle } from "../components/ViewToggle";
import { rememberOwnRealtime } from "../realtime/ownRealtime";
import { statusLabels } from "../taskMeta";
import { TaskPriority, TaskState, type TaskPriorityValue, type TaskStateValue } from "../types";

export function ProjectPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isStaff, userId } = useAuth();
  const directory = useDirectoryMap();

  const project = useQuery({ queryKey: ["project", id], queryFn: () => getProject(id), enabled: !!id });
  const tasks = useQuery({
    queryKey: ["project-tasks", id],
    queryFn: () => getProjectTasks(id),
    enabled: !!id,
  });

  const canMutate = isStaff || project.data?.ownerId === userId;
  const canDelete = isStaff;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [ownerId, setOwnerId] = useState("");

  useEffect(() => {
    if (!project.data) return;
    setName(project.data.name);
    setDescription(project.data.description ?? "");
    setOwnerId(project.data.ownerId);
  }, [project.data]);

  const save = useMutation({
    mutationFn: () =>
      updateProject(id, {
        name,
        description,
        ownerId: isStaff ? ownerId : undefined,
      }),
    onSuccess: async () => {
      rememberOwnRealtime("project.updated", id);
      await queryClient.invalidateQueries({ queryKey: ["project", id] });
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
  });

  const remove = useMutation({
    mutationFn: () => deleteProject(id),
    onSuccess: () => {
      rememberOwnRealtime("project.deleted", id);
      navigate("/projects");
    },
  });

  const [title, setTitle] = useState("");
  const [taskDescription, setTaskDescription] = useState("");
  const [status, setStatus] = useState<TaskStateValue>(TaskState.New);
  const [priority, setPriority] = useState<TaskPriorityValue>(TaskPriority.Medium);
  const [deadline, setDeadline] = useState("");
  const [assignees, setAssignees] = useState<string[]>([]);
  const [view, setView] = useViewMode("project-tasks-view");

  const addTask = useMutation({
    mutationFn: () =>
      createTask({
        projectId: id,
        title,
        description: taskDescription || undefined,
        status,
        priority,
        deadline: deadline ? new Date(deadline).toISOString() : null,
        assigneeIds: assignees,
      }),
    onSuccess: async (task) => {
      rememberOwnRealtime("task.created", id, task.id);
      setTitle("");
      setTaskDescription("");
      setAssignees([]);
      setDeadline("");
      await queryClient.invalidateQueries({ queryKey: ["project-tasks", id] });
    },
  });

  function toggleAssignee(userKey: string) {
    setAssignees((current) =>
      current.includes(userKey) ? current.filter((item) => item !== userKey) : [...current, userKey],
    );
  }

  if (project.isError) {
    const statusCode = project.error instanceof ApiError ? project.error.status : 0;
    return (
      <section>
        <ErrorBanner error={project.error} />
        {statusCode === 403 && <p className="muted">You are not allowed to open this project.</p>}
        {statusCode === 404 && <p className="muted">Project was not found.</p>}
        <Link to="/projects">Back to projects</Link>
      </section>
    );
  }

  if (!project.data) return <p className="muted">Loading project…</p>;

  return (
    <section>
      <p>
        <Link to="/projects">← Projects</Link>
      </p>
      <h1>{project.data.name}</h1>
      <ErrorBanner error={save.error ?? remove.error ?? addTask.error ?? tasks.error} />

      {canMutate ? (
        <form
          className="card stack"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          <h2>Project details</h2>
          <label>
            Name
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </label>
          <label>
            Description
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          {isStaff && (
            <label>
              Owner
              <select value={ownerId} onChange={(e) => setOwnerId(e.target.value)}>
                {(directory.data ?? []).map((user) => (
                  <option key={user.id} value={user.id}>
                    {userLabel(directory.map, user.id)}
                  </option>
                ))}
              </select>
            </label>
          )}
          <div className="row">
            <button type="submit" disabled={save.isPending}>
              Save
            </button>
            {canDelete && (
              <button
                type="button"
                className="danger"
                disabled={remove.isPending}
                onClick={() => remove.mutate()}
              >
                Delete project
              </button>
            )}
          </div>
        </form>
      ) : (
        <p className="muted">{project.data.description || "No description."}</p>
      )}

      <div className="section-head">
        <h2>Tasks</h2>
        <ViewToggle value={view} onChange={setView} />
      </div>
      {tasks.isLoading ? (
        <p className="muted">Loading tasks…</p>
      ) : view === "tiles" ? (
        <div className="tile-grid">
          {(tasks.data ?? []).map((task) => (
            <Link key={task.id} className="tile" to={`/projects/${id}/tasks/${task.id}`}>
              <span className={`status-badge status-${task.status}`}>{statusLabels[task.status]}</span>
              <strong>{task.title}</strong>
            </Link>
          ))}
          {tasks.data?.length === 0 && <p className="muted">No tasks yet.</p>}
        </div>
      ) : (
        <ul className="list">
          {(tasks.data ?? []).map((task) => (
            <li key={task.id}>
              <Link to={`/projects/${id}/tasks/${task.id}`}>{task.title}</Link>
              <span className={`status-badge status-${task.status}`}>{statusLabels[task.status]}</span>
            </li>
          ))}
          {tasks.data?.length === 0 && <li className="muted">No tasks yet.</li>}
        </ul>
      )}

      {canMutate && (
        <form
          className="card stack"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            addTask.mutate();
          }}
        >
          <h2>New task</h2>
          <label>
            Title
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
          </label>
          <label>
            Description
            <textarea value={taskDescription} onChange={(e) => setTaskDescription(e.target.value)} />
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
            {(directory.data ?? []).map((user) => (
              <label key={user.id} className="check">
                <input
                  type="checkbox"
                  checked={assignees.includes(user.id)}
                  onChange={() => toggleAssignee(user.id)}
                />
                {userLabel(directory.map, user.id)}
              </label>
            ))}
          </fieldset>
          <button type="submit" disabled={addTask.isPending}>
            Create task
          </button>
        </form>
      )}
    </section>
  );
}
