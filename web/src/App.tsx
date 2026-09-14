import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { GuestOnly, RequireAuth, RequireStaff } from "./components/RequireAuth";
import { Layout } from "./components/Layout";
import { LoginPage } from "./pages/Login";
import { ProfilePage } from "./pages/Profile";
import { ProjectPage } from "./pages/Project";
import { ProjectsPage } from "./pages/Projects";
import { RegisterPage } from "./pages/Register";
import { TagsPage } from "./pages/Tags";
import { TaskPage } from "./pages/Task";
import { UsersPage } from "./pages/Users";

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<GuestOnly />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>
        <Route element={<RequireAuth />}>
          <Route element={<Layout />}>
            <Route path="/projects" element={<ProjectsPage />} />
            <Route path="/projects/:id" element={<ProjectPage />} />
            <Route path="/projects/:id/tasks/:taskId" element={<TaskPage />} />
            <Route path="/tags" element={<TagsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route element={<RequireStaff />}>
              <Route path="/users" element={<UsersPage />} />
            </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/projects" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
