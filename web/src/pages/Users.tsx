import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { deleteUser, getUsers, updateUser } from "../api/users";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";

export function UsersPage() {
  const { role, userId } = useAuth();
  const queryClient = useQueryClient();
  const users = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");

  const save = useMutation({
    mutationFn: () => updateUser(editingId!, { email, username }),
    onSuccess: async () => {
      setEditingId(null);
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      await queryClient.invalidateQueries({ queryKey: ["directory"] });
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => deleteUser(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      await queryClient.invalidateQueries({ queryKey: ["directory"] });
    },
  });

  return (
    <section>
      <h1>Users</h1>
      <ErrorBanner error={users.error ?? save.error ?? remove.error} />
      <ul className="list">
        {(users.data ?? []).map((person) => (
          <li key={person.id}>
            {editingId === person.id ? (
              <form
                className="stack"
                onSubmit={(event: FormEvent) => {
                  event.preventDefault();
                  save.mutate();
                }}
              >
                <input value={username} onChange={(e) => setUsername(e.target.value)} required />
                <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
                <div className="row">
                  <button type="submit">Save</button>
                  <button type="button" className="ghost" onClick={() => setEditingId(null)}>
                    Cancel
                  </button>
                </div>
              </form>
            ) : (
              <>
                <strong>{person.username}</strong>
                <span className="muted">{person.email}</span>
                <div className="row">
                  {(role === "Admin" || person.id === userId) && (
                  <button
                    type="button"
                    className="ghost"
                    onClick={() => {
                      setEditingId(person.id);
                      setEmail(person.email);
                      setUsername(person.username);
                    }}
                  >
                    Edit
                  </button>
                  )}
                  {role === "Admin" && (
                    <button type="button" className="ghost" onClick={() => remove.mutate(person.id)}>
                      Delete
                    </button>
                  )}
                </div>
              </>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
