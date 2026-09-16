import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { createProject, getProjects } from "../api/projects";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";
import { userLabel, useDirectoryMap } from "../components/useDirectory";
import { useViewMode, ViewToggle } from "../components/ViewToggle";
import { rememberOwnRealtime } from "../realtime/ownRealtime";

export function ProjectsPage() {
  const { isStaff, userId } = useAuth();
  const queryClient = useQueryClient();
  const projects = useQuery({ queryKey: ["projects"], queryFn: getProjects });
  const directory = useDirectoryMap();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [ownerId, setOwnerId] = useState("");
  const [view, setView] = useViewMode("projects-view");

  const create = useMutation({
    mutationFn: () =>
      createProject({
        name,
        description: description || undefined,
        ownerId: ownerId || undefined,
      }),
    onSuccess: async (project) => {
      rememberOwnRealtime("project.created", project.id);
      setName("");
      setDescription("");
      setOwnerId("");
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
  });

  function onCreate(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <section>
      <div className="section-head">
        <h1>Projects</h1>
        <ViewToggle value={view} onChange={setView} />
      </div>
      <ErrorBanner error={projects.error ?? create.error} />
      {isStaff && (
        <form className="card stack" onSubmit={onCreate}>
          <h2>Create project</h2>
          <label>
            Name
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </label>
          <label>
            Description
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label>
            Owner
            <select value={ownerId} onChange={(e) => setOwnerId(e.target.value)}>
              <option value="">Me (current user)</option>
              {(directory.data ?? []).map((user) => (
                <option key={user.id} value={user.id}>
                  {userLabel(directory.map, user.id)}
                </option>
              ))}
            </select>
          </label>
          <button type="submit" disabled={create.isPending}>
            Create
          </button>
        </form>
      )}
      {projects.isLoading ? (
        <p className="muted">Loading…</p>
      ) : view === "tiles" ? (
        <div className="tile-grid">
          {(projects.data ?? []).map((project) => (
            <Link key={project.id} className="tile" to={`/projects/${project.id}`}>
              <strong>{project.name}</strong>
              <span className="muted">
                {project.ownerId === userId ? "You own this" : `Owner ${userLabel(directory.map, project.ownerId)}`}
              </span>
              {project.description && <span className="muted tile-excerpt">{project.description}</span>}
            </Link>
          ))}
          {projects.data?.length === 0 && <p className="muted">No projects yet.</p>}
        </div>
      ) : (
        <ul className="list">
          {(projects.data ?? []).map((project) => (
            <li key={project.id}>
              <Link to={`/projects/${project.id}`}>
                <strong>{project.name}</strong>
              </Link>
              <span className="muted">
                {project.ownerId === userId ? "You own this" : `Owner ${userLabel(directory.map, project.ownerId)}`}
              </span>
            </li>
          ))}
          {projects.data?.length === 0 && <li className="muted">No projects yet.</li>}
        </ul>
      )}
    </section>
  );
}
