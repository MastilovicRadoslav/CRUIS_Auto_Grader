import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { fetchMySubmissions } from "../services/submissionService";
import { List, Card, Typography, Spin, message, Button } from "antd";
import SubmitWorkModal from "../components/SubmitWorkModal";
import FeedbackModal from "../components/FeedbackModal";

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
        console.log("Fetched submissions:", data);
        setSubmissions(data);
      } catch (err) {
        message.error("Failed to load your submissions.");
      } finally {
        setLoading(false);
      }
    };

    loadSubmissions();
  }, [token]);

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
              <p><strong>Status:</strong> {item.status}</p>
              <p><strong>Content:</strong> {item.content}</p>
              <p>
                <strong>Submitted At:</strong>{" "}
                {new Date(item.submittedAt).toLocaleString()}
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
        onSuccess={async () => {
          setLoading(true);
          try {
            const data = await fetchMySubmissions(token);
            setSubmissions(data);
          } catch (err) {
            message.error("Failed to reload submissions.");
          } finally {
            setLoading(false);
          }
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
