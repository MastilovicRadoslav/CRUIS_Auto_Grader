import { createContext, useContext, useEffect, useState } from "react";

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [authState, setAuthState] = useState({
    token: null,
    userId: null,
    role: null,
  });

  useEffect(() => {
    const token = localStorage.getItem("token");
    const userId = localStorage.getItem("userId");
    const role = localStorage.getItem("role");

    if (token && userId && role) {
      setAuthState({ token, userId, role });
    }
  }, []);

  const login = (token, userId, role) => {
    localStorage.setItem("token", token);
    localStorage.setItem("userId", userId);
    localStorage.setItem("role", role);

    setAuthState({ token, userId, role });
  };

  const logout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("role");

    setAuthState({ token: null, userId: null, role: null });
  };

  return (
    <AuthContext.Provider
      value={{
        token: authState.token,
        userId: authState.userId,
        role: authState.role,
        isAuthenticated: !!authState.token,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => useContext(AuthContext);
