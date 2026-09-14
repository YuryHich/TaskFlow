import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { HubSubscriber } from "../realtime/hub";

export function Layout() {
  const { user, role, isStaff, logout } = useAuth();

  return (
    <div className="app">
      <HubSubscriber />
      <header className="topbar">
        <strong className="brand">TaskFlow</strong>
        <nav>
          <NavLink to="/projects" end>Projects</NavLink>
          <NavLink to="/tags">Tags</NavLink>
          {isStaff && <NavLink to="/users">Users</NavLink>}
          <NavLink to="/profile">Profile</NavLink>
        </nav>
        <div className="who">
          <span>
            {user?.username} <em>{role}</em>
          </span>
          <button type="button" className="ghost" onClick={() => void logout()}>
            Logout
          </button>
        </div>
      </header>
      <main className="page">
        <Outlet />
      </main>
    </div>
  );
}
