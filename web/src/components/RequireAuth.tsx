import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function RequireAuth() {
  const { ready, user } = useAuth();
  if (!ready) return <p className="muted pad">Loading session…</p>;
  if (!user) return <Navigate to="/login" replace />;
  return <Outlet />;
}

export function RequireStaff() {
  const { isStaff } = useAuth();
  if (!isStaff) return <Navigate to="/projects" replace />;
  return <Outlet />;
}

export function GuestOnly() {
  const { ready, user } = useAuth();
  if (!ready) return <p className="muted pad">Loading session…</p>;
  if (user) return <Navigate to="/projects" replace />;
  return <Outlet />;
}
