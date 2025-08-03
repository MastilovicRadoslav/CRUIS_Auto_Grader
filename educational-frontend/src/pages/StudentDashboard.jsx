import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { fetchMySubmissions } from "../services/submissionService";
import { List, Card, Typography, Spin, message, Button, Tag } from "antd";
import SubmitWorkModal from "../components/SubmitWorkModal";
import FeedbackModal from "../components/FeedbackModal";
import { useSignalR } from "../services/useSignalR"; // ili tvoja tačna putanja


const { Title } = Typography;

const StudentDashboard = () => {
  const { token } = useAuth();
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedWorkId, setSelectedWorkId] = useState(null); // 👈 za feedback modal

  useEffect(() => {
    const loadSubmissions = async () => {
      try {
        const data = await fetchMySubmissions(token);
        setSubmissions(data);
      } catch (err) {
        message.error("Failed to load your submissions.");
      } finally {
        setLoading(false);
      }
    };

    loadSubmissions();
  }, [token]);

  useSignalR((data) => {
    setSubmissions((prev) => {
      const found = prev.find((s) => s.id === data.workId);
      if (found) {
        return prev.map((s) =>
          s.id === data.workId
            ? {
              ...s,
              status: data.newStatus,
              estimatedAnalysisTime: data.estimatedAnalysisTime,
              submittedAt: data.submittedAt
            }
            : s
        );
      } else {
        return [{
          id: data.workId,
          title: data.title,
          status: data.newStatus,
          estimatedAnalysisTime: data.estimatedAnalysisTime,
          submittedAt: data.submittedAt
        }, ...prev];
      }
    });
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
      Pending: "Pendign",
    };
    return statusMap[status] || status;
  };



  return (
    <div style={{ padding: "2rem" }}>
      <Title level={2}>My Submissions</Title>

      <Button
        type="primary"
        onClick={() => setIsModalOpen(true)}
        style={{ marginBottom: "1rem" }}
      >
        Submit New Work
      </Button>

      <List
        grid={{ gutter: 16, column: 1 }}
        dataSource={submissions}
        renderItem={(item) => (
          <List.Item>
            <Card title={item.title}>
              <p><strong>Status:</strong> {formatStatus(item.status)}</p>
              <p>
                <strong>Estimated Analysis Time:</strong>{" "}
                {item.estimatedAnalysisTime
                  ? `${parseInt(item.estimatedAnalysisTime.match(/\d+/g)?.[1] || "0", 10)} minutes`
                  : "Unknown"}
              </p>
              <p>
                <strong>Submitted At:</strong>{" "}
                {item.submittedAt
                  ? new Date(item.submittedAt).toLocaleString()
                  : "Unknown"}
              </p>

              <Button onClick={() => setSelectedWorkId(item.id)}>
                View Feedback
              </Button>
            </Card>
          </List.Item>
        )}
      />

      <SubmitWorkModal
        visible={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />




      <FeedbackModal
        visible={!!selectedWorkId}
        workId={selectedWorkId}
        onClose={() => setSelectedWorkId(null)}
      />
    </div>
  );
};

export default StudentDashboard;
