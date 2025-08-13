// components/Navbar.jsx
import { Layout, Button, Typography, Avatar } from "antd";
import { UserOutlined } from "@ant-design/icons";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/Navbar.css";

const { Header } = Layout;
const { Text } = Typography;

const Navbar = () => {
  const { token, username, role, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  const displayName = username || "User";
  const displayRole = role ? `${role}:` : ""; // dodaj ":" samo ako ima role

  const initials =
    displayName
      .split(" ")
      .map((p) => p[0])
      .join("")
      .slice(0, 2)
      .toUpperCase() || "U";

  return (
    <Header className="navbar-header">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          EduAnalyzer
        </Link>

        <div className="navbar-actions">
          {token ? (
            <>
              <div className="navbar-user">
                <Avatar
                  className="navbar-avatar"
                  size={32}
                  icon={!username ? <UserOutlined /> : null}
                >
                  {username ? initials : null}
                </Avatar>
                <Text className="navbar-username" ellipsis>
                  {displayRole} {displayName}
                </Text>
              </div>
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
