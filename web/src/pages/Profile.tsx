import { useMutation } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { updateUser } from "../api/users";
import { useAuth } from "../auth/AuthContext";
import { ErrorBanner } from "../components/ErrorBanner";

export function ProfilePage() {
  const { user, reloadMe } = useAuth();
  const [email, setEmail] = useState(user?.email ?? "");
  const [username, setUsername] = useState(user?.username ?? "");

  useEffect(() => {
    setEmail(user?.email ?? "");
    setUsername(user?.username ?? "");
  }, [user]);

  const save = useMutation({
    mutationFn: () => updateUser(user!.id, { email, username }),
    onSuccess: () => reloadMe(),
  });

  if (!user) return null;

  return (
    <section>
      <h1>Profile</h1>
      <ErrorBanner error={save.error} />
      <form
        className="card stack"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          save.mutate();
        }}
      >
        <label>
          Username
          <input value={username} onChange={(e) => setUsername(e.target.value)} required />
        </label>
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <button type="submit" disabled={save.isPending}>
          Save
        </button>
      </form>
    </section>
  );
}
