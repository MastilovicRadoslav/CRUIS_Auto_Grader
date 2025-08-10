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
  getAnalysisSettings,          // <-- NEW
  setAnalysisSettings,          // <-- NEW
} from "../services/adminService";
import {
  Table, Button, Modal, Input, InputNumber, Select, Form, message, Typography, Card,
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

  // Admin analysis settings
  const [analysis, setAnalysis] = useState({ minGrade: 1, maxGrade: 10, methods: [] });
  const [analysisForm, setAnalysisForm] = useState({ minGrade: 1, maxGrade: 10, methods: [] });
  const [analysisLoading, setAnalysisLoading] = useState(false);

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

  const loadAnalysis = async () => {
    try {
      const s = await getAnalysisSettings(token);
      setAnalysis(s);
      setAnalysisForm({
        minGrade: s?.minGrade ?? 1,
        maxGrade: s?.maxGrade ?? 10,
        methods: s?.methods ?? [],
      });
    } catch {
      message.error("Failed to load analysis settings.");
    }
  };

  useEffect(() => {
    loadUsers();
    loadWindowSetting();
    loadAnalysis();
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

      {/* Submission Rate Limit (sliding window) */}
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

      {/* Admin Analysis Settings */}
      <Card style={{ marginTop: 16 }}>
        <Title level={4}>Admin Analysis Settings</Title>
        <p style={{ marginBottom: 12 }}>
          Current: grade range <strong>{analysis.minGrade}</strong> – <strong>{analysis.maxGrade}</strong>
          {analysis.methods?.length ? (
            <> | methods: <strong>{analysis.methods.join(", ")}</strong></>
          ) : (
            <> | methods: <em>default</em></>
          )}
        </p>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 220px)", gap: 12, alignItems: "center" }}>
          <div>
            <div style={{ marginBottom: 6 }}>Min grade</div>
            <InputNumber
              min={0}
              max={100}
              value={analysisForm.minGrade}
              onChange={(v) => setAnalysisForm((p) => ({ ...p, minGrade: Number(v ?? 0) }))}
              style={{ width: "100%" }}
            />
          </div>

          <div>
            <div style={{ marginBottom: 6 }}>Max grade</div>
            <InputNumber
              min={0}
              max={100}
              value={analysisForm.maxGrade}
              onChange={(v) => setAnalysisForm((p) => ({ ...p, maxGrade: Number(v ?? 0) }))}
              style={{ width: "100%" }}
            />
          </div>

          <div>
            <div style={{ marginBottom: 6 }}>Methods (optional)</div>
            <Select
              mode="tags"
              allowClear
              placeholder="e.g. grammar, plagiarism, code-style"
              value={analysisForm.methods}
              onChange={(vals) => setAnalysisForm((p) => ({ ...p, methods: vals }))}
              style={{ width: "100%" }}
              tokenSeparators={[',']}
              options={[
                { value: "grammar", label: "grammar" },
                { value: "plagiarism", label: "plagiarism" },
                { value: "code-style", label: "code-style" },
                { value: "complexity", label: "complexity" },
                { value: "test-coverage", label: "test-coverage" },
              ]}
            />
          </div>
        </div>

        <div style={{ display: "flex", gap: 8, marginTop: 12, flexWrap: "wrap" }}>
          <Button
            type="primary"
            loading={analysisLoading}
            onClick={async () => {
              const { minGrade, maxGrade, methods } = analysisForm;
              if (minGrade < 0 || maxGrade <= 0 || minGrade >= maxGrade) {
                return message.warning("Invalid grade range. Ensure min < max and both are non-negative.");
              }
              try {
                setAnalysisLoading(true);
                await setAnalysisSettings({ minGrade, maxGrade, methods }, token);
                setAnalysis({ minGrade, maxGrade, methods });
                message.success("Analysis settings updated.");
              } catch {
                message.error("Failed to update analysis settings.");
              } finally {
                setAnalysisLoading(false);
              }
            }}
          >
            Save Analysis Settings
          </Button>

          <Button
            onClick={() =>
              setAnalysisForm({
                minGrade: analysis.minGrade,
                maxGrade: analysis.maxGrade,
                methods: analysis.methods,
              })
            }
          >
            Revert Changes
          </Button>

          <Button
            danger
            onClick={async () => {
              try {
                setAnalysisLoading(true);
                await setAnalysisSettings({ minGrade: 1, maxGrade: 10, methods: [] }, token);
                setAnalysis({ minGrade: 1, maxGrade: 10, methods: [] });
                setAnalysisForm({ minGrade: 1, maxGrade: 10, methods: [] });
                message.success("Analysis settings reset to defaults.");
              } catch {
                message.error("Failed to reset analysis settings.");
              } finally {
                setAnalysisLoading(false);
              }
            }}
          >
            Reset to Defaults
          </Button>
        </div>
      </Card>
    </div>
  );
};

export default AdminDashboard;
