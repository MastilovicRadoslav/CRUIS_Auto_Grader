import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { Form, Input, Button, Select, message, Card } from "antd";
import axios from "axios";
import { registerUser } from "../services/authService";

const { Option } = Select;

const RegisterPage = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);

  const onFinish = async (values) => {
    setLoading(true);
    try {
      registerUser(values); //slanje post zahtjeva na backend
      message.success("Registration successful! You can now log in.");
      navigate("/");
    } catch (err) {
      message.error(err.response?.data?.error || "Registration failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="centered-page">
      <Card title="Register" style={{ width: 400 }}>
        <Form layout="vertical" onFinish={onFinish}>
          <Form.Item name="username" label="Username" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item name="password" label="Password" rules={[{ required: true }]}>
            <Input.Password />
          </Form.Item>
          <Form.Item name="role" label="Role" rules={[{ required: true }]}>
            <Select placeholder="Select a role">
              <Option value="Student">Student</Option>
              <Option value="Professor">Professor</Option>
            </Select>
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} block>
              Register
            </Button>
          </Form.Item>
          <div style={{ textAlign: "center" }}>
            Already have an account? <Link to="/">Login</Link>
          </div>
        </Form>
      </Card>
    </div>
  );
};

export default RegisterPage;
