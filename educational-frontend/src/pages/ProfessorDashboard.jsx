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
} from "antd";
import ProfessorFeedbackModal from "../components/ProfessorFeedbackModal";
import { useSignalR } from "../services/useSignalR";
import StudentProgressPanel from "../components/StudentProgressPanel";
import PerformanceReportModal from "../components/PerformanceReportModal";

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

  // student progress panel
  const ALL_KEY = "__ALL__";
  const [selectedStudentId, setSelectedStudentId] = useState(ALL_KEY); // 👈 odmah ALL
  const [refreshKey, setRefreshKey] = useState(0);

  // performance report modal
  const [reportOpen, setReportOpen] = useState(false);

  const openModal = (workId) => {
    setSelectedWorkId(workId);
    setIsModalVisible(true);
  };
  const closeModal = () => {
    setIsModalVisible(false);
    setSelectedWorkId(null);
  };

  useEffect(() => {
    const loadSubmissions = async () => {
      try {
        const data = await fetchAllSubmissions(token);
        setSubmissions(data);
      } catch {
        message.error("Failed to load submissions.");
      } finally {
        setLoading(false);
      }
    };
    loadSubmissions();
  }, [token]);

  // jedinstveni studenti (za inicijalni izbor i za report modal)
  const studentOptions = useMemo(() => {
    const map = new Map();
    submissions.forEach((s) => {
      if (s.studentId && s.studentName) map.set(s.studentId, s.studentName);
    });
    return Array.from(map, ([value, label]) => ({ value, label }));
  }, [submissions]);

  useEffect(() => {
    if (!selectedStudentId && studentOptions.length > 0) {
      setSelectedStudentId(studentOptions[0].value);
    }
  }, [studentOptions, selectedStudentId]);

  useSignalR(
    // status promjena
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
        }
        return [
          {
            id: data.workId,
            title: data.title,
            status: data.newStatus,
            estimatedAnalysisTime: data.estimatedAnalysisTime,
            submittedAt: data.submittedAt,
            studentName: data.studentName || "Unknown",
            studentId: data.studentId,
          },
          ...prev,
        ];
      });
    },
    // auto refresh napretka izabranog studenta
    async (updatedStudentId) => {
      if (selectedStudentId === ALL_KEY || updatedStudentId === selectedStudentId) {
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
      <div style={{ height: "60vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
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
      {/* lijevo: lista + toolbar */}
      <div>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <Title level={2} style={{ marginBottom: 16 }}>Professor Dashboard</Title>
          <Button onClick={() => setReportOpen(true)}>Performance Report</Button>
        </div>

        <Row gutter={16} style={{ marginBottom: "1.5rem" }} align="middle">
          <Col xs={24} sm={8}>
            <Search placeholder="Search by student name" allowClear onSearch={(v) => setSearchTerm(v)} />
          </Col>
          <Col xs={24} sm={8}>
            <Select
              placeholder="Filter by status"
              allowClear
              style={{ width: "100%" }}
              onChange={(v) => setStatusFilter(v)}
            >
              <Option value="Pending">Pending</Option>
              <Option value="InProgress">InProgress</Option>
              <Option value="Completed">Completed</Option>
              <Option value="Rejected">Rejected</Option>
            </Select>
          </Col>
        </Row>

        <List
          grid={{ gutter: 16, column: 1 }}
          dataSource={filteredSubmissions}
          renderItem={(item) => (
            <List.Item>
              <Card title={`📝 ${item.title}`}>
                <p><strong>Student:</strong> {item.studentName}</p>
                <p><strong>Status:</strong> {formatStatus(item.status)}</p>
                <p><strong>Submitted At:</strong> {new Date(item.submittedAt).toLocaleString()}</p>
                <Button type="primary" onClick={() => openModal(item.id)}>View Feedback</Button>
              </Card>
            </List.Item>
          )}
        />
      </div>

      {/* desno: napredak izabranog studenta */}
      <div>
        <StudentProgressPanel
          token={token}
          submissions={submissions}
          selectedId={selectedStudentId}
          onChangeSelectedId={setSelectedStudentId}
          refreshKey={refreshKey}
        />
      </div>

      {/* feedback modal */}
      <ProfessorFeedbackModal open={isModalVisible} onClose={closeModal} workId={selectedWorkId} />

      {/* performance report modal */}
      <PerformanceReportModal
        open={reportOpen}
        onClose={() => setReportOpen(false)}
        token={token}
        submissions={submissions}
        allowStudentFilter={true} // stavi false ako želiš samo "svi studenti"
      />
    </div>
  );
};

export default ProfessorDashboard;
