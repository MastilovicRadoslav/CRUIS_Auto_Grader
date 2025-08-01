import { useEffect, useState } from "react";
import { Modal, Typography, Spin, Alert } from "antd";
import { getFeedbackByWorkId } from "../services/submissionService";
import { useAuth } from "../context/AuthContext"; // Dodato

const { Title, Paragraph } = Typography;

const FeedbackModal = ({ visible, onClose, workId }) => {
  const { token, userId } = useAuth(); // 👈 dodaj userId
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!workId || !visible) return;

    const fetchFeedback = async () => {
      setLoading(true);
      try {
        const result = await getFeedbackByWorkId(workId); 
        setFeedback(result);
        setError("");
      } catch (err) {
        setError("Failed to load feedback.");
      } finally {
        setLoading(false);
      }
    };

    fetchFeedback();
  }, [workId, visible, token]);

  return (
    <Modal
      title="Feedback Details"
      open={visible}
      onCancel={onClose}
      footer={null}
    >
      {loading ? (
        <Spin />
      ) : error ? (
        <Alert type="error" message={error} />
      ) : feedback ? (
        <div>
          <Title level={5}>Work Title</Title>
          <Paragraph>{feedback.title || "Untitled"}</Paragraph>

          <Title level={5}>Student Name</Title>
          <Paragraph>{feedback.studentName || "Unknown"}</Paragraph>

          <Title level={5}>Grade</Title>
          <Paragraph>{feedback.grade}</Paragraph>

          <Title level={5}>Summary</Title>
          <Paragraph>{feedback.summary}</Paragraph>

          <Title level={5}>Issues</Title>
          <Paragraph>
            {feedback.issues && feedback.issues.length > 0
              ? feedback.issues.join(", ")
              : "No issues."}
          </Paragraph>

          <Title level={5}>Suggestions</Title>
          <Paragraph>
            {feedback.suggestions && feedback.suggestions.length > 0
              ? feedback.suggestions.join(", ")
              : "No suggestions."}
          </Paragraph>

          <Title level={5}>Professor's Comment</Title>
          <Paragraph>
            {feedback.professorComment || "No comment yet."}
          </Paragraph>

          <Title level={5}>Evaluated At</Title>
          <Paragraph>
            {feedback.evaluatedAt
              ? new Date(feedback.evaluatedAt).toLocaleString()
              : "Not evaluated yet."}
          </Paragraph>
        </div>
      ) : (
        <Paragraph>No feedback found.</Paragraph>
      )}
    </Modal>
  );
};

export default FeedbackModal;
