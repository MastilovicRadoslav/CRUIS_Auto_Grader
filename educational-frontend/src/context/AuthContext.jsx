// context/AuthContext.jsx
import { createContext, useContext, useEffect, useState } from "react";

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [authState, setAuthState] = useState({
    token: null,
    userId: null,
    role: null,
    username: null,
  });

  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    const userId = localStorage.getItem("userId");
    const role = localStorage.getItem("role");
    const username = localStorage.getItem("username");

    if (token && userId && role) {
      setAuthState({ token, userId, role, username: username || null });
    }
    setHydrated(true);
  }, []);

  const login = (token, userId, role, username) => {
    localStorage.setItem("token", token);
    localStorage.setItem("userId", userId);
    localStorage.setItem("role", role);
    if (username) localStorage.setItem("username", username);

    setAuthState({ token, userId, role, username: username || null });
  };

  const logout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("role");
    localStorage.removeItem("username");

    setAuthState({ token: null, userId: null, role: null, username: null });
  };

  return (
    <AuthContext.Provider
      value={{
        token: authState.token,
        userId: authState.userId,
        role: authState.role,
        username: authState.username,
        isAuthenticated: !!authState.token,
        hydrated,           // <-- NOVO
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
