import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { createTag, deleteTag, getTags, updateTag } from "../api/tags";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";

export function TagsPage() {
  const { isStaff } = useAuth();
  const queryClient = useQueryClient();
  const tags = useQuery({ queryKey: ["tags"], queryFn: getTags });
  const [name, setName] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState("");

  const create = useMutation({
    mutationFn: () => createTag(name),
    onSuccess: async () => {
      setName("");
      await queryClient.invalidateQueries({ queryKey: ["tags"] });
    },
  });

  const save = useMutation({
    mutationFn: () => updateTag(editingId!, editingName),
    onSuccess: async () => {
      setEditingId(null);
      await queryClient.invalidateQueries({ queryKey: ["tags"] });
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => deleteTag(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tags"] });
    },
  });

  return (
    <section>
      <h1>Tags</h1>
      <p className="muted">Catalog only — tasks do not store tag links in the current API.</p>
      <ErrorBanner error={tags.error ?? create.error ?? save.error} />
      <ul className="list">
        {(tags.data ?? []).map((tag) => (
          <li key={tag.id}>
            {editingId === tag.id ? (
              <form
                className="row"
                onSubmit={(event: FormEvent) => {
                  event.preventDefault();
                  save.mutate();
                }}
              >
                <input value={editingName} onChange={(e) => setEditingName(e.target.value)} required />
                <button type="submit">Save</button>
                <button type="button" className="ghost" onClick={() => setEditingId(null)}>
                  Cancel
                </button>
              </form>
            ) : (
              <>
                <strong>{tag.name}</strong>
                {isStaff && (
                  <div className="row">
                    <button
                      type="button"
                      className="ghost"
                      onClick={() => {
                        setEditingId(tag.id);
                        setEditingName(tag.name);
                      }}
                    >
                      Edit
                    </button>
                    <button type="button" className="ghost" onClick={() => remove.mutate(tag.id)}>
                      Delete
                    </button>
                  </div>
                )}
              </>
            )}
          </li>
        ))}
      </ul>
      {isStaff && (
        <form
          className="card row"
          onSubmit={(event: FormEvent) => {
            event.preventDefault();
            create.mutate();
          }}
        >
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New tag" required />
          <button type="submit" disabled={create.isPending}>
            Add
          </button>
        </form>
      )}
    </section>
  );
}
