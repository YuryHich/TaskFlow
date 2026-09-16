import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { login as loginApi, logout as logoutApi, register as registerApi } from "../api/auth";
import { getMe } from "../api/users";
import type { Role, UserDto } from "../types";
import { parseAccessToken } from "./jwt";
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  onLoggedOut,
  refreshSession,
  setSession,
} from "./session";

type AuthState = {
  ready: boolean;
  user: UserDto | null;
  role: Role | null;
  userId: string | null;
  isStaff: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  reloadMe: () => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

async function loadProfile(): Promise<{ user: UserDto; role: Role; userId: string } | null> {
  const token = getAccessToken();
  if (!token) return null;
  const claims = parseAccessToken(token);
  const user = await getMe();
  return { user, role: claims.role, userId: claims.userId };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [user, setUser] = useState<UserDto | null>(null);
  const [role, setRole] = useState<Role | null>(null);
  const [userId, setUserId] = useState<string | null>(null);

  const applyProfile = useCallback(async () => {
    const profile = await loadProfile();
    if (!profile) {
      setUser(null);
      setRole(null);
      setUserId(null);
      return;
    }
    setUser(profile.user);
    setRole(profile.role);
    setUserId(profile.userId);
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken();
    try {
      if (refreshToken) await logoutApi(refreshToken);
    } catch {
      /* still clear locally */
    }
    clearSession();
    setUser(null);
    setRole(null);
    setUserId(null);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        if (!getAccessToken() && getRefreshToken()) {
          await refreshSession();
        }
        if (!cancelled) await applyProfile();
      } catch {
        clearSession();
        if (!cancelled) {
          setUser(null);
          setRole(null);
          setUserId(null);
        }
      } finally {
        if (!cancelled) setReady(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [applyProfile]);

  useEffect(() => onLoggedOut(() => {
    setUser(null);
    setRole(null);
    setUserId(null);
  }), []);

  const login = useCallback(async (email: string, password: string) => {
    const tokens = await loginApi(email, password);
    setSession(tokens);
    await applyProfile();
  }, [applyProfile]);

  const register = useCallback(async (email: string, username: string, password: string) => {
    const tokens = await registerApi(email, username, password);
    setSession(tokens);
    await applyProfile();
  }, [applyProfile]);

  const value = useMemo<AuthState>(
    () => ({
      ready,
      user,
      role,
      userId,
      isStaff: role === "Admin" || role === "Manager",
      login,
      register,
      logout,
      reloadMe: applyProfile,
    }),
    [ready, user, role, userId, login, register, logout, applyProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
