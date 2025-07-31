import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Form, Input, Button, message, Card } from "antd";
import { loginUser } from "../services/authService";
import { useAuth } from "../context/AuthContext";
import { Link } from "react-router-dom";


const LoginPage = () => {
    const { login } = useAuth();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(false);

    const onFinish = async (values) => {
        setLoading(true);
        try {
            const response = await loginUser(values); //slanje post zahtjeva na backend
            login(response.data.Token);
            message.success(response.data.Message);
            if (response.data.Role === "Student") navigate("/student");
            else if (response.data.Role === "Professor") navigate("/professor");
            else navigate("/admin");
        } catch (err) {
            message.error("Login failed: " + (err.response?.data || "Unexpected error"));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-container" style={{ display: "flex", justifyContent: "center", marginTop: "10%" }}>
            <Card title="Login" style={{ width: 400 }}>
                <Form layout="vertical" onFinish={onFinish}>
                    <Form.Item name="username" label="Username" rules={[{ required: true }]}>
                        <Input />
                    </Form.Item>
                    <Form.Item name="password" label="Password" rules={[{ required: true }]}>
                        <Input.Password />
                    </Form.Item>
                    <Form.Item>
                        <Button type="primary" htmlType="submit" loading={loading} block>
                            Log in
                        </Button>
                    </Form.Item>
                    <div style={{ marginTop: "1rem", textAlign: "center" }}>
                        Don't have an account? <Link to="/register">Register here</Link>
                    </div>
                </Form>
            </Card>
        </div>
    );
};

export default LoginPage;
