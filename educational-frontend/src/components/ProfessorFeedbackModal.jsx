import { useEffect, useState } from "react";
import { Modal, Typography, Input, Button, Spin, Alert, message } from "antd";
import { getFeedbackByWorkId } from "../services/submissionService";
import { addProfessorComment } from "../services/evaluationService";
import { useAuth } from "../context/AuthContext";

const { Title, Paragraph } = Typography;
const { TextArea } = Input;

const ProfessorFeedbackModal = ({ workId, open, onClose }) => {
    const { token } = useAuth();
    const [feedback, setFeedback] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [comment, setComment] = useState("");
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        if (!open || !workId) return;

        const fetchFeedback = async () => {
            setLoading(true);
            try {
                const data = await getFeedbackByWorkId(workId);
                setFeedback(data);
                setComment(data.professorComment || "");
                setError("");
            } catch (err) {
                setError("Failed to load feedback.");
            } finally {
                setLoading(false);
            }
        };

        fetchFeedback();
    }, [open, workId, token]);

    const handleAddComment = async () => {
        console.log("Kliknut Save"); // <--- Dodaj ovo
        setSubmitting(true);
        try {
            await addProfessorComment({ workId, comment }, token);
            message.success("Comment saved successfully.");
            onClose();
        } catch (error) {
            console.error(error); // <--- Loguj grešku
            message.error("Failed to save comment.");
        } finally {
            setSubmitting(false);
        }
    };


    return (
        <Modal
            open={open}
            onCancel={onClose}
            onOk={handleAddComment}
            okText="Save Comment"
            confirmLoading={submitting}
            title="Feedback Details"
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
                    <TextArea
                        rows={4}
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        placeholder="Add or update your comment here"
                    />

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

export default ProfessorFeedbackModal;
