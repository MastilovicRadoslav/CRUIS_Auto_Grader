import { useEffect, useState, useCallback } from "react";
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

  const loadSubmissions = useCallback(async () => {
    try {
      const data = await fetchMySubmissions(token);
      setSubmissions(Array.isArray(data) ? data : []);
    } catch (err) {
      message.error("Failed to load your submissions.");
    }
  }, [token]);

  const loadProgress = useCallback(async () => {
    try {
      const data = await fetchStudentProgress(userId, token);
      setProgress(data || null);
    } catch (err) {
      message.error("Failed to load your progress statistics.");
    }
  }, [userId, token]);

  useEffect(() => {
    const run = async () => {
      setLoading(true);
      await Promise.all([loadSubmissions(), loadProgress()]);
      setLoading(false);
    };
    if (token) run();
  }, [token, userId, loadSubmissions, loadProgress]);

  // helper: detekcija "brisanja" iz različitih payload varijanti
  const isDeletedEvent = (data) =>
    data?.deleted === true ||
    data?.action === "Deleted" ||
    data?.newStatus === "Deleted" ||
    data?.newStatus === 4; // ako koristite enum

  useSignalR(
    async (data) => {
      if (isDeletedEvent(data)) {
        await Promise.all([loadSubmissions(), loadProgress()]);
        return;
      }
      setSubmissions((prev) => {
        const found = prev.find((s) => s.id === data.workId);
        if (found) {
          return prev.map((s) =>
            s.id === data.workId
              ? {
                ...s,
                title: data.title ?? s.title,
                status: data.newStatus ?? s.status,
                estimatedAnalysisTime: data.estimatedAnalysisTime ?? s.estimatedAnalysisTime,
                submittedAt: data.submittedAt ?? s.submittedAt,
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
          },
          ...prev,
        ];
      });
    },
    async (updatedStudentId) => {
      if (updatedStudentId === userId) {
        try {
          const updatedProgress = await fetchStudentProgress(userId, token);
          setProgress(updatedProgress || { totalWorks: 0, gradeTimeline: [] });
        } catch {
          message.warning("Couldn't refresh progress stats");
        }
      }
    },
    async (purgedStudentId) => {
      if (purgedStudentId === userId) {
        setSubmissions([]);
        setProgress({ totalWorks: 0, gradeTimeline: [] });
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

  const hasStats = !!progress && Number(progress?.totalWorks) > 0;
  const hasChart =
    hasStats &&
    Array.isArray(progress?.gradeTimeline) &&
    progress.gradeTimeline.length > 0;

  return (
    <div className="student-dashboard">
      <div className="sd-left">
        <div className="sd-header">
          <Title level={2} className="sd-title">My Submissions</Title>
          <Button
            type="primary"
            onClick={() => setIsModalOpen(true)}
            className="sd-submit-btn"
          >
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
        <Card title="📊 Your Progress Statistics" className="sd-stats-card">
          {hasStats ? (
            <>
              <p className="sd-row"><strong>Total Works:</strong> {progress.totalWorks}</p>
              <p className="sd-row"><strong>Average Grade:</strong> {progress.averageGrade}</p>
              <p className="sd-row"><strong>Above 9:</strong> {progress.above9}</p>
              <p className="sd-row"><strong>Between 7 and 8:</strong> {progress.between7And8}</p>
              <p className="sd-row"><strong>Below 7:</strong> {progress.below7}</p>
            </>
          ) : (
            <div className="sd-row" style={{ opacity: 0.7 }}>No stats yet.</div>
          )}
        </Card>

        <Card title="📈 Grade Evolution Over Time" className="sd-chart-card">
          {hasChart ? (
            <ProgressChart data={progress} />
          ) : (
            <div style={{ opacity: 0.7 }}>No data for chart.</div>
          )}
        </Card>
      </div>

      <SubmitWorkModal
        visible={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={async () => {
          await Promise.all([loadSubmissions(), loadProgress()]);
        }}
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
