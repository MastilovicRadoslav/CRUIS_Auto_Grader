import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import Navbar from "./components/Navbar";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import StudentDashboard from "./pages/StudentDashboard";
import { Layout } from "antd";

function App() {
  return (
    <AuthProvider>
      <Router>
        <Navbar />
        <Layout style={{ marginTop: 64, minHeight: "100vh", backgroundColor: "#f0f2f5" }}>
          <div style={{ maxWidth: 1200, margin: "0 auto", padding: "20px" }}>
            <Routes>
              <Route path="/" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/student" element={<StudentDashboard />} />
              {/* Ostale rute za profesora i admina dolaze kasnije */}
            </Routes>
          </div>
        </Layout>
      </Router>
    </AuthProvider>
  );
}

export default App;
