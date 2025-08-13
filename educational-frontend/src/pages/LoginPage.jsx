import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Form, Input, Button, message, Card } from "antd";
import { loginUser } from "../services/authService";
import { useAuth } from "../context/AuthContext";
import "../styles/LoginPage.css";

const LoginPage = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values) => {
    setLoading(true);
    try {
      const response = await loginUser(values);
      const usernameFromApi = response?.data?.username;
      const username = usernameFromApi || values.username;

      login(response.data.token, response.data.userId, response.data.role, username);
      message.success(response.data.Message || "Login successful");

      const role = response.data.role.toLowerCase();
      if (role === "student") navigate("/student");
      else if (role === "professor") navigate("/professor");
      else navigate("/admin");
    } catch (err) {
      message.error("Login failed: " + (err.response?.data || "Unexpected error"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <Card
        title="Login"
        className="login-card"
        headStyle={{
          textAlign: "center",
          fontWeight: 700,
          fontSize: 18,
        }}
        bodyStyle={{
          paddingTop: 20,
        }}
      >
        <Form layout="vertical" onFinish={onFinish} autoComplete="off">
          <Form.Item name="username" label="Username" rules={[{ required: true }]}>
            <Input
              size="large"
              placeholder="Enter your username"
              disabled={loading}
              allowClear
            />
          </Form.Item>

          <Form.Item name="password" label="Password" rules={[{ required: true }]}>
            <Input.Password
              size="large"
              placeholder="Enter your password"
              disabled={loading}
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
              size="large"
            >
              Log in
            </Button>
          </Form.Item>

          <div className="login-footer">
            Don&apos;t have an account? <Link to="/register">Register here</Link>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPage;
