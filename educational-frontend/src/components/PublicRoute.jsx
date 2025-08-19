// src/routes/PublicRoute.jsx
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const PublicRoute = ({ children }) => {
  const { token, role, hydrated } = useAuth();
  if (!hydrated) return null;

  if (token) {
    const byRole = {
      Student: "/student",
      Professor: "/professor",
      Admin: "/admin",
    };
    return <Navigate to={byRole[role] || "/"} replace />;
  }

  return children;
};

export default PublicRoute;
