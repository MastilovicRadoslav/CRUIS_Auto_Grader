import { useEffect, useMemo, useState } from "react";
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
import { useSignalR } from "../services/useSignalR";
import StudentProgressPanel from "../components/StudentProgressPanel";

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

  // ➕ za panel napretka
  const [selectedStudentId, setSelectedStudentId] = useState(null);
  const [refreshKey, setRefreshKey] = useState(0); // okida refetch u panelu kad stigne SignalR

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

  // iz submissions izvedi jedinstvene studente (za inicijalni izbor u panelu)
  const studentOptions = useMemo(() => {
    const map = new Map();
    submissions.forEach((s) => {
      if (s.studentId && s.studentName) {
        map.set(s.studentId, s.studentName);
      }
    });
    return Array.from(map, ([value, label]) => ({ value, label }));
  }, [submissions]);

  useEffect(() => {
    if (!selectedStudentId && studentOptions.length > 0) {
      setSelectedStudentId(studentOptions[0].value);
    }
  }, [studentOptions, selectedStudentId]);

  useSignalR(
    // 1) status radova (već imaš)
    (data) => {
      setSubmissions((prev) => {
        const found = prev.find((s) => s.id === data.workId);
        if (found) {
          return prev.map((s) =>
            s.id === data.workId
              ? {
                  ...s,
                  status: data.newStatus,
                  estimatedAnalysisTime: data.estimatedAnalysisTime,
                  submittedAt: data.submittedAt,
                }
              : s
          );
        } else {
          return [
            {
              id: data.workId,
              title: data.title,
              status: data.newStatus,
              estimatedAnalysisTime: data.estimatedAnalysisTime,
              submittedAt: data.submittedAt,
              studentName: data.studentName || "Unknown",
              studentId: data.studentId, // ako backend šalje; ako ne, ostaje undefined
            },
            ...prev,
          ];
        }
      });
    },
    // 2) napredak (ProgressUpdated sa StudentId) -> auto refresh samo za trenutno izabranog
    async (updatedStudentId) => {
      if (selectedStudentId && updatedStudentId === selectedStudentId) {
        // okini refetch u panelu
        setRefreshKey((k) => k + 1);
      }
    }
  );

  const filteredSubmissions = submissions.filter((item) => {
    const name = (item.studentName || "").toLowerCase();
    const nameMatch = name.includes(searchTerm.toLowerCase());
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

  const formatStatus = (status) => {
    const statusMap = {
      0: "Pending",
      1: "InProgress",
      2: "Completed",
      3: "Rejected",
      InProgress: "In Progress",
      Completed: "Completed",
      Rejected: "Rejected",
      Pending: "Pending",
    };
    return statusMap[status] || status;
  };

  return (
    <div
      style={{
        padding: "2rem",
        display: "grid",
        gridTemplateColumns: "2fr 1fr",
        gap: "1.5rem",
      }}
    >
      {/* lijevo: lista radova */}
      <div>
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
                <p>
                  <strong>Student:</strong> {item.studentName}
                </p>
                <p>
                  <strong>Status:</strong> {formatStatus(item.status)}
                </p>
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
      </div>

      {/* desno: panel napretka izabranog studenta */}
      <div>
        <StudentProgressPanel
          token={token}
          submissions={submissions}
          selectedId={selectedStudentId}
          onChangeSelectedId={setSelectedStudentId}
          refreshKey={refreshKey}
        />
      </div>

      {/* Feedback modal */}
      <ProfessorFeedbackModal
        open={isModalVisible}
        onClose={closeModal}
        workId={selectedWorkId}
      />

      {/* Modal za globalne statistike (date range) */}
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
