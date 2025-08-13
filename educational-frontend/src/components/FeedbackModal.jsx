import { useEffect, useState } from "react";
import { Modal, Typography, Spin, Alert } from "antd";
import { getFeedbackByWorkId } from "../services/submissionService";
import { useAuth } from "../context/AuthContext";
import "../styles/FeedbackModal.css";

const { Title, Paragraph, Text } = Typography;

const FeedbackModal = ({ visible, onClose, workId }) => {
  const { token } = useAuth();
  const [feedback, setFeedback] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!workId || !visible) return;

    const fetchFeedback = async () => {
      setLoading(true);
      try {
        const result = await getFeedbackByWorkId(workId, token);
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

  const renderChips = (arr) => {
    if (!arr || !arr.length) return <Text type="secondary">None</Text>;
    return (
      <div className="fbm-chips">
        {arr.map((x, idx) => (
          <span className="fbm-chip" key={idx}>
            {x}
          </span>
        ))}
      </div>
    );
  };

  return (
    <Modal
      title="Feedback Details"
      open={visible}
      onCancel={onClose}
      footer={null}
      className="fbm-modal"
    >
      {loading ? (
        <div className="fbm-center">
          <Spin />
        </div>
      ) : error ? (
        <Alert type="error" message={error} />
      ) : feedback ? (
        <div className="fbm-grid">
          <div className="fbm-block">
            <Title level={5}>Work Title</Title>
            <Paragraph>{feedback.title || "Untitled"}</Paragraph>
          </div>

          <div className="fbm-block">
            <Title level={5}>Student Name</Title>
            <Paragraph>{feedback.studentName || "Unknown"}</Paragraph>
          </div>

          <div className="fbm-inline">
            <div>
              <Title level={5}>Grade</Title>
              <Paragraph className="fbm-grade">
                {feedback.grade ?? "—"}
              </Paragraph>
            </div>
            <div>
              <Title level={5}>Evaluated At</Title>
              <Paragraph>
                {feedback.evaluatedAt
                  ? new Date(feedback.evaluatedAt).toLocaleString()
                  : "Not evaluated yet."}
              </Paragraph>
            </div>
          </div>

          <div className="fbm-block">
            <Title level={5}>Identified Errors</Title>
            {renderChips(feedback.identifiedErrors)}
          </div>

          <div className="fbm-block">
            <Title level={5}>Improvement Suggestions</Title>
            {renderChips(feedback.improvementSuggestions)}
          </div>

          <div className="fbm-block">
            <Title level={5}>Further Recommendations</Title>
            {renderChips(feedback.furtherRecommendations)}
          </div>

          <div className="fbm-block">
            <Title level={5}>Professor&apos;s Comment</Title>
            <Paragraph>
              {feedback.professorComment || "No comment yet."}
            </Paragraph>
          </div>
        </div>
      ) : (
        <Paragraph>No feedback found.</Paragraph>
      )}
    </Modal>
  );
};

export default FeedbackModal;
