// App.jsx
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./components/Navbar";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import StudentDashboard from "./pages/StudentDashboard";
import ProfessorDashboard from "./pages/ProfessorDashboard";
import AdminDashboard from "./pages/AdminDashboard";
import ProtectedRoute from "./components/ProtectedRoute";
import PublicRoute from "./components/PublicRoute";
import { Layout } from "antd";

function App() {
  return (
    <Router>
      <Navbar />
      <Layout style={{ marginTop: 64, minHeight: "100vh", backgroundColor: "#dde1e8ff" }}>
        <div style={{ maxWidth: 1200, margin: "0 auto", padding: "20px" }}>
          <Routes>
            {/* Landing -> preusmjeri na /login */}
            <Route path="/" element={<Navigate to="/login" replace />} />

            {/* Javne rute */}
            <Route
              path="/login"
              element={
                <PublicRoute>
                  <LoginPage />
                </PublicRoute>
              }
            />
            <Route
              path="/register"
              element={
                <PublicRoute>
                  <RegisterPage />
                </PublicRoute>
              }
            />

            {/* Zaštićene rute po ulogama */}
            <Route
              path="/student"
              element={
                <ProtectedRoute allowedRoles={["Student"]}>
                  <StudentDashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/professor"
              element={
                <ProtectedRoute allowedRoles={["Professor"]}>
                  <ProfessorDashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin"
              element={
                <ProtectedRoute allowedRoles={["Admin"]}>
                  <AdminDashboard />
                </ProtectedRoute>
              }
            />

            {/* Catch-all */}
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </div>
      </Layout>
    </Router>
  );
}

export default App;
