// src/routes/ProtectedRoute.jsx
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const ProtectedRoute = ({ children, allowedRoles }) => {
  const { token, role, hydrated } = useAuth();

  // Dok ne učitamo stanje iz localStorage, ne renderujemo ništa (ili ubaci spinner po želji)
  if (!hydrated) return null;

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(role)) {
    // Nije dozvoljena uloga -> pošalji ga na svoj dashboard ako znamo koji je,
    // ili na /login kao fallback.
    const byRole = {
      Student: "/student",
      Professor: "/professor",
      Admin: "/admin",
    };
    return <Navigate to={byRole[role] || "/login"} replace />;
  }

  return children;
};

export default ProtectedRoute;
