import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { fetchMySubmissions } from "../services/submissionService";
import { List, Card, Typography, Spin, message, Button } from "antd";
import SubmitWorkModal from "../components/SubmitWorkModal";
import FeedbackModal from "../components/FeedbackModal";
import { useSignalR } from "../services/useSignalR";
import { fetchStudentProgress } from "../services/progressService";
import ProgressChart from "../components/ProgressChart";

const { Title } = Typography;

const StudentDashboard = () => {
  const { token, userId } = useAuth();
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedWorkId, setSelectedWorkId] = useState(null);
  const [progress, setProgress] = useState(null);

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

    const loadProgress = async () => {
      try {
        const data = await fetchStudentProgress(userId, token);
        setProgress(data);
      } catch (err) {
        message.error("Failed to load your progress statistics.");
      }
    };

    loadSubmissions();
    loadProgress();
  }, [token]);

  useSignalR(
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
            },
            ...prev,
          ];
        }
      });
    },
    async (updatedStudentId) => {
      if (updatedStudentId === userId) {
        try {
          const updatedProgress = await fetchStudentProgress(userId, token);
          setProgress(updatedProgress);
        } catch {
          message.warning("Couldn't refresh progress stats");
        }
      }
    }
  );

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
    <div style={{ display: "flex", gap: "2rem", padding: "2rem" }}>
      <div style={{ flex: 2 }}>
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
      </div>

      <div style={{ flex: 1 }}>
        {progress && (
          <Card title="📊 Your Progress Statistics" style={{ marginBottom: "2rem" }}>
            <p><strong>Total Works:</strong> {progress.totalWorks}</p>
            <p><strong>Average Grade:</strong> {progress.averageGrade}</p>
            <p><strong>Above 9:</strong> {progress.above9}</p>
            <p><strong>Between 7 and 8:</strong> {progress.between7And8}</p>
            <p><strong>Below 7:</strong> {progress.below7}</p>
          </Card>
        )}
        {progress && (
          <Card title="📈 Grade Evolution Over Time">
            <ProgressChart data={progress} />
          </Card>
        )}
      </div>

      <SubmitWorkModal visible={isModalOpen} onClose={() => setIsModalOpen(false)} />
      <FeedbackModal visible={!!selectedWorkId} workId={selectedWorkId} onClose={() => setSelectedWorkId(null)} />
    </div>
  );
};

export default StudentDashboard;
