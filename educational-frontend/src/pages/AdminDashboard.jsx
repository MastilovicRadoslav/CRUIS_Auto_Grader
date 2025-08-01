import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { getAllUsers, createUser, updateUser, deleteUser, getMaxSubmissions, setMaxSubmissions, } from "../services/adminService";
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
    const [maxSubmissions, setMaxSubmissionsValue] = useState(0);
    const [maxLoading, setMaxLoading] = useState(false);
    const [newMax, setNewMax] = useState("");

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

    useEffect(() => {
        loadUsers();
        loadMaxSubmissions();
    }, [token]);

    const loadMaxSubmissions = async () => {
        try {
            const res = await getMaxSubmissions(token);
            setMaxSubmissionsValue(res.maxPerStudent);
        } catch {
            message.error("Failed to load max submissions setting.");
        }
    };

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

            <Button type="primary" onClick={() => { setModalVisible(true); setEditUserId(null); form.resetFields(); }}>
                Create New User
            </Button>

            <Table
                dataSource={users}
                loading={loading}
                rowKey="id"
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
            <Card style={{ marginTop: 32 }}>
                <Title level={4}>Submission Settings</Title>
                <p>
                    Current max submissions per student: <strong>{maxSubmissions}</strong>
                </p>
                <Input
                    type="number"
                    value={newMax}
                    onChange={(e) => setNewMax(e.target.value)}
                    placeholder="Enter new max"
                    style={{ width: 200, marginRight: 10 }}
                />
                <Button
                    type="primary"
                    onClick={async () => {
                        if (!newMax || isNaN(newMax)) {
                            return message.warning("Please enter a valid number.");
                        }
                        try {
                            setMaxLoading(true);
                            await setMaxSubmissions(parseInt(newMax), token);
                            message.success("Max submissions updated.");
                            setMaxSubmissionsValue(parseInt(newMax));
                            setNewMax("");
                        } catch {
                            message.error("Failed to update setting.");
                        } finally {
                            setMaxLoading(false);
                        }
                    }}
                    loading={maxLoading}
                >
                    Save Setting
                </Button>
            </Card>

        </div>
    );
};

export default AdminDashboard;
