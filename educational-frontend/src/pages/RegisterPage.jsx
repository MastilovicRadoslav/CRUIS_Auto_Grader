import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Form, Input, Button, Select, message, Card } from "antd";
import { registerUser } from "../services/authService";
import "../styles/RegisterPage.css";

const { Option } = Select;

const RegisterPage = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values) => {
    setLoading(true);
    try {
      await registerUser(values); // slanje POST zahtjeva na backend
      message.success("Registration successful! You can now log in.");
      navigate("/");
    } catch (err) {
      message.error(err.response?.data?.error || "Registration failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-container">
      <Card
        title="Register"
        className="register-card"
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
            <Input size="large" placeholder="Enter your username" disabled={loading} allowClear />
          </Form.Item>

          <Form.Item name="password" label="Password" rules={[{ required: true }]}>
            <Input.Password size="large" placeholder="Enter your password" disabled={loading} />
          </Form.Item>

          <Form.Item name="role" label="Role" rules={[{ required: true }]}>
            <Select
              placeholder="Select a role"
              size="large"
              disabled={loading}
              allowClear
            >
              <Option value="Student">Student</Option>
              <Option value="Professor">Professor</Option>
            </Select>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button type="primary" htmlType="submit" loading={loading} block size="large">
              Register
            </Button>
          </Form.Item>

          <div className="register-footer">
            Already have an account? <Link to="/">Login</Link>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default RegisterPage;
