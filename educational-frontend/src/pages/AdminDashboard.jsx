// src/pages/AdminDashboard.jsx
import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import {
    getAllUsers,
    createUser,
    updateUser,
    deleteUser,
    getSubmissionWindowSetting,
    setSubmissionWindowSetting,
} from "../services/adminService";
import {
    Table, Button, Modal, Input, Select, Form, message, Typography, Card,
} from "antd";

const { Title } = Typography;
const { Option } = Select;

const AdminDashboard = () => {
    const { token } = useAuth();
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);

    const [modalVisible, setModalVisible] = useState(false);
    const [editUserId, setEditUserId] = useState(null);

    // Sliding-window setting
    const [windowSetting, setWindowSetting] = useState({ maxPerWindow: 0, windowSizeDays: 0 });
    const [wsLoading, setWsLoading] = useState(false);
    const [wsForm, setWsForm] = useState({ maxPerWindow: "", windowSizeDays: "" });

    const [form] = Form.useForm();

    const loadUsers = async () => {
        setLoading(true);
        try {
            const data = await getAllUsers(token);
            setUsers(data);
        } catch {
            message.error("Failed to load users");
        } finally {
            setLoading(false);
        }
    };

    const loadWindowSetting = async () => {
        try {
            const s = await getSubmissionWindowSetting(token);
            setWindowSetting(s);
            setWsForm({
                maxPerWindow: String(s?.maxPerWindow ?? 0),
                windowSizeDays: String(s?.windowSizeDays ?? 0),
            });
        } catch {
            message.error("Failed to load submission window setting.");
        }
    };

    useEffect(() => {
        loadUsers();
        loadWindowSetting();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [token]);

    const handleCreateOrUpdate = async (values) => {
        try {
            if (editUserId) {
                await updateUser(editUserId, values, token);
                message.success("User updated");
            } else {
                await createUser(values, token);
                message.success("User created");
            }
            setModalVisible(false);
            setEditUserId(null);
            form.resetFields();
            loadUsers();
        } catch {
            message.error("Failed to save user");
        }
    };

    const handleDelete = async (id) => {
        try {
            await deleteUser(id, token);
            message.success("User deleted");
            loadUsers();
        } catch {
            message.error("Failed to delete user");
        }
    };

    const openEditModal = (user) => {
        setEditUserId(user.id);
        form.setFieldsValue({
            password: "",
            role: user.role,
        });
        setModalVisible(true);
    };

    return (
        <div style={{ padding: 20 }}>
            <Title level={2}>Admin Dashboard</Title>

            <Button
                type="primary"
                onClick={() => { setModalVisible(true); setEditUserId(null); form.resetFields(); }}
            >
                Create New User
            </Button>

            <Table
                dataSource={users}
                loading={loading}
                rowKey="id"
                style={{ marginTop: 16 }}
                columns={[
                    { title: "Username", dataIndex: "username" },
                    { title: "Role", dataIndex: "role" },
                    {
                        title: "Actions",
                        render: (_, record) => (
                            <>
                                <Button onClick={() => openEditModal(record)} style={{ marginRight: 8 }}>
                                    Edit
                                </Button>
                                <Button danger onClick={() => handleDelete(record.id)}>
                                    Delete
                                </Button>
                            </>
                        ),
                    },
                ]}
            />

            <Modal
                open={modalVisible}
                title={editUserId ? "Edit User" : "Create User"}
                onCancel={() => setModalVisible(false)}
                onOk={() => form.submit()}
                okText="Save"
            >
                <Form form={form} onFinish={handleCreateOrUpdate} layout="vertical">
                    <Form.Item
                        name="username"
                        label="Username"
                        rules={[{ required: !editUserId, message: "Username required" }]}
                    >
                        <Input disabled={!!editUserId} />
                    </Form.Item>

                    <Form.Item
                        name="password"
                        label="Password"
                        rules={[{ required: !editUserId, message: "Password required" }]}
                    >
                        <Input.Password />
                    </Form.Item>

                    <Form.Item name="role" label="Role" rules={[{ required: true }]}>
                        <Select>
                            <Option value="Admin">Admin</Option>
                            <Option value="Professor">Professor</Option>
                            <Option value="Student">Student</Option>
                        </Select>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Jedina aktivna postavka: sliding-window */}
            <Card style={{ marginTop: 32 }}>
                <Title level={4}>Submission Rate Limit (sliding window)</Title>
                <p>
                    Current: <strong>{windowSetting.maxPerWindow}</strong> in the last{" "}
                    <strong>{windowSetting.windowSizeDays}</strong> days
                </p>
                <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
                    <Input
                        type="number"
                        placeholder="Max per window"
                        style={{ width: 200 }}
                        value={wsForm.maxPerWindow}
                        onChange={(e) => setWsForm((p) => ({ ...p, maxPerWindow: e.target.value }))}
                    />
                    <Input
                        type="number"
                        placeholder="Window size (days)"
                        style={{ width: 200 }}
                        value={wsForm.windowSizeDays}
                        onChange={(e) => setWsForm((p) => ({ ...p, windowSizeDays: e.target.value }))}
                    />
                    <Button
                        type="primary"
                        loading={wsLoading}
                        onClick={async () => {
                            const max = parseInt(wsForm.maxPerWindow, 10);
                            const days = parseInt(wsForm.windowSizeDays, 10);
                            if (isNaN(max) || isNaN(days) || max < 0 || days < 0) {
                                return message.warning("Enter non-negative numbers.");
                            }
                            try {
                                setWsLoading(true);
                                await setSubmissionWindowSetting({ maxPerWindow: max, windowSizeDays: days }, token);
                                message.success("Submission window setting updated.");
                                setWindowSetting({ maxPerWindow: max, windowSizeDays: days });
                            } catch {
                                message.error("Failed to update submission window setting.");
                            } finally {
                                setWsLoading(false);
                            }
                        }}
                    >
                        Save Window Setting
                    </Button>

                    <Button
                        danger
                        onClick={async () => {
                            try {
                                setWsLoading(true);
                                await setSubmissionWindowSetting({ maxPerWindow: 0, windowSizeDays: 0 }, token);
                                setWindowSetting({ maxPerWindow: 0, windowSizeDays: 0 });
                                setWsForm({ maxPerWindow: "0", windowSizeDays: "0" });
                                message.success("Submission limit reset to unlimited.");
                            } catch {
                                message.error("Failed to reset submission limit.");
                            } finally {
                                setWsLoading(false);
                            }
                        }}
                    >
                        Reset to Unlimited
                    </Button>

                </div>
            </Card>
        </div>
    );
};

export default AdminDashboard;
