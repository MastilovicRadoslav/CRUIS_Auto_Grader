import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { fetchMySubmissions } from "../services/submissionService";
import { List, Card, Typography, Spin, message, Button } from "antd";
import SubmitWorkModal from "../components/SubmitWorkModal";
import FeedbackModal from "../components/FeedbackModal";
import { useSignalR } from "../services/useSignalR";
import { fetchStudentProgress } from "../services/progressService";
import ProgressChart from "../components/ProgressChart";
import "../styles/StudentDashboard.css";

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
  }, [token]); // ostavljeno kao i kod tebe da ne mijenjamo ponašanje

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
      <div className="sd-loading">
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

  const getStatusClass = (normalized) => {
    switch (normalized) {
      case "Completed":
        return "sd-status sd-status--success";
      case "In Progress":
      case "InProgress":
        return "sd-status sd-status--processing";
      case "Rejected":
        return "sd-status sd-status--danger";
      case "Pending":
      default:
        return "sd-status sd-status--warning";
    }
  };

  return (
    <div className="student-dashboard">
      <div className="sd-left">
        <div className="sd-header">
          <Title level={2} className="sd-title">My Submissions</Title>
          <Button type="primary" onClick={() => setIsModalOpen(true)} className="sd-submit-btn">
            Submit New Work
          </Button>
        </div>

        <List
          grid={{ gutter: 16, column: 1 }}
          dataSource={submissions}
          renderItem={(item) => {
            const normalizedStatus = formatStatus(item.status);
            return (
              <List.Item>
                <Card title={`📝 ${item.title}`} className="sd-card">
                  <p className="sd-row">
                    <strong>Status:</strong>{" "}
                    <span className={getStatusClass(normalizedStatus)}>
                      {normalizedStatus}
                    </span>
                  </p>

                  <p className="sd-row">
                    <strong>Estimated Analysis Time:</strong>{" "}
                    {item.estimatedAnalysisTime
                      ? `${parseInt(item.estimatedAnalysisTime.match(/\d+/g)?.[1] || "0", 10)} minutes`
                      : "Unknown"}
                  </p>

                  <p className="sd-row">
                    <strong>Submitted At:</strong>{" "}
                    {item.submittedAt
                      ? new Date(item.submittedAt).toLocaleString()
                      : "Unknown"}
                  </p>

                  <Button type="primary" onClick={() => setSelectedWorkId(item.id)}>
                    View Feedback
                  </Button>
                </Card>
              </List.Item>
            );
          }}
        />
      </div>

      <div className="sd-right">
        {progress && (
          <Card title="📊 Your Progress Statistics" className="sd-stats-card">
            <p className="sd-row"><strong>Total Works:</strong> {progress.totalWorks}</p>
            <p className="sd-row"><strong>Average Grade:</strong> {progress.averageGrade}</p>
            <p className="sd-row"><strong>Above 9:</strong> {progress.above9}</p>
            <p className="sd-row"><strong>Between 7 and 8:</strong> {progress.between7And8}</p>
            <p className="sd-row"><strong>Below 7:</strong> {progress.below7}</p>
          </Card>
        )}
        {progress && (
          <Card title="📈 Grade Evolution Over Time" className="sd-chart-card">
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
