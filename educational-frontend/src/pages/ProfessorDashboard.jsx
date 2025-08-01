import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { fetchAllSubmissions } from "../services/submissionService";
import {
  List,
  Card,
  Typography,
  Spin,
  message,
  Button,
  Input,
  Select,
  Row,
  Col,
  Modal,
} from "antd";
import ProfessorFeedbackModal from "../components/ProfessorFeedbackModal";
import DateRangeStats from "../components/DateRangeStats";

const { Title } = Typography;
const { Search } = Input;
const { Option } = Select;

const ProfessorDashboard = () => {
  const { token } = useAuth();
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedWorkId, setSelectedWorkId] = useState(null);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [isStatsModalVisible, setIsStatsModalVisible] = useState(false);

  const openModal = (workId) => {
    setSelectedWorkId(workId);
    setIsModalVisible(true);
  };

  const closeModal = () => {
    setIsModalVisible(false);
    setSelectedWorkId(null);
  };

  const openStatsModal = () => setIsStatsModalVisible(true);
  const closeStatsModal = () => setIsStatsModalVisible(false);

  useEffect(() => {
    const loadSubmissions = async () => {
      try {
        const data = await fetchAllSubmissions(token);
        setSubmissions(data);
      } catch (err) {
        message.error("Failed to load submissions.");
      } finally {
        setLoading(false);
      }
    };

    loadSubmissions();
  }, [token]);

  const filteredSubmissions = submissions.filter((item) => {
    const nameMatch = item.studentName.toLowerCase().includes(searchTerm.toLowerCase());
    const statusMatch = statusFilter ? item.status === statusFilter : true;
    return nameMatch && statusMatch;
  });

  if (loading) {
    return (
      <div
        style={{
          height: "60vh",
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
        }}
      >
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div style={{ padding: "2rem" }}>
      <Title level={2}>All Student Submissions</Title>

      <Row gutter={16} style={{ marginBottom: "1.5rem" }} align="middle">
        <Col xs={24} sm={8}>
          <Search
            placeholder="Search by student name"
            allowClear
            onSearch={(value) => setSearchTerm(value)}
          />
        </Col>
        <Col xs={24} sm={8}>
          <Select
            placeholder="Filter by status"
            allowClear
            style={{ width: "100%" }}
            onChange={(value) => setStatusFilter(value)}
          >
            <Option value="Pending">Pending</Option>
            <Option value="InProgress">InProgress</Option>
            <Option value="Completed">Completed</Option>
            <Option value="Rejected">Rejected</Option>
          </Select>
        </Col>
        <Col xs={24} sm={8}>
          <Button
            type="primary"
            onClick={openStatsModal}
            style={{ float: "right", width: "100%" }}
          >
            Get Statistics
          </Button>
        </Col>
      </Row>

      <List
        grid={{ gutter: 16, column: 1 }}
        dataSource={filteredSubmissions}
        renderItem={(item) => (
          <List.Item>
            <Card title={`📝 ${item.title}`}>
              <p><strong>Student:</strong> {item.studentName}</p>
              <p><strong>Status:</strong> {item.status}</p>
              <p><strong>Content:</strong> {item.content}</p>
              <p>
                <strong>Submitted At:</strong>{" "}
                {new Date(item.submittedAt).toLocaleString()}
              </p>
              <Button type="primary" onClick={() => openModal(item.id)}>
                View Feedback
              </Button>
            </Card>
          </List.Item>
        )}
      />

      {/* Modal za Feedback */}
      <ProfessorFeedbackModal
        open={isModalVisible}
        onClose={closeModal}
        workId={selectedWorkId}
      />

      {/* Modal za Statistiku */}
      <Modal
        open={isStatsModalVisible}
        onCancel={closeStatsModal}
        footer={null}
        title="Submission Statistics"
        width={700}
      >
        <DateRangeStats token={token} />
      </Modal>
    </div>
  );
};

export default ProfessorDashboard;
