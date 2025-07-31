import { Layout, Button, Typography } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/Navbar.css";

const { Header } = Layout;
const { Text } = Typography;

const Navbar = () => {
  const { token, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  return (
    <Header className="navbar-header">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          EduAnalyzer
        </Link>

        <div className="navbar-actions">
          {token ? (
            <>
              <Button danger onClick={handleLogout}>
                Logout
              </Button>
            </>
          ) : (
            <>
              <Button onClick={() => navigate("/")}>Login</Button>
              <Button type="primary" onClick={() => navigate("/register")}>
                Register
              </Button>
            </>
          )}
        </div>
      </div>
    </Header>
  );
};

export default Navbar;
