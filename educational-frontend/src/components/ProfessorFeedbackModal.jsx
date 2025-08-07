import { useEffect, useState } from "react";
import { Modal, Typography, Input, Button, Spin, Alert, message } from "antd";
import { getFeedbackByWorkId } from "../services/submissionService";
import { addProfessorComment, reanalyzeSubmission } from "../services/evaluationService";
import { useAuth } from "../context/AuthContext";

const { Title, Paragraph } = Typography;
const { TextArea } = Input;

const ProfessorFeedbackModal = ({ workId, open, onClose }) => {
    const { token } = useAuth();
    const [feedback, setFeedback] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");
    const [comment, setComment] = useState("");
    const [instruction, setInstruction] = useState(""); // ⬅️ instrukcija za re-analizu
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        if (!open || !workId) return;

        const fetchFeedback = async () => {
            setLoading(true);
            try {
                const data = await getFeedbackByWorkId(workId);
                setFeedback(data);
                setComment(data.professorComment || "");
                setInstruction(""); // resetuj instrukciju pri svakom otvaranju
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
        setSubmitting(true);
        try {
            await addProfessorComment({ workId, comment }, token);
            message.success("Comment saved successfully.");
            onClose();
        } catch (error) {
            console.error(error);
            message.error("Failed to save comment.");
        } finally {
            setSubmitting(false);
        }
    };

    const handleReanalyze = async () => {
        setSubmitting(true);
        try {
            const reanalysisData = {
                workId: workId,
                instructions: instruction, // ✅ mora biti "instructions"
            };
            await reanalyzeSubmission(reanalysisData, token);
            message.success("Re-analysis triggered successfully.");
            onClose();
        } catch (error) {
            console.error(error);
            message.error("Failed to trigger re-analysis.");
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

                    <Title level={5}>Identified Errors</Title>
                    <Paragraph>
                        {feedback.identifiedErrors?.length
                            ? feedback.identifiedErrors.join(", ")
                            : "No identified errors."}
                    </Paragraph>

                    <Title level={5}>Improvement Suggestions</Title>
                    <Paragraph>
                        {feedback.improvementSuggestions?.length
                            ? feedback.improvementSuggestions.join(", ")
                            : "No suggestions."}
                    </Paragraph>

                    <Title level={5}>Further Recommendations</Title>
                    <Paragraph>
                        {feedback.furtherRecommendations?.length
                            ? feedback.furtherRecommendations.join(", ")
                            : "No recommendations."}
                    </Paragraph>

                    <Title level={5}>Professor's Comment</Title>
                    <TextArea
                        rows={4}
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        placeholder="Add or update your general comment"
                    />

                    <Title level={5} style={{ marginTop: "1.5rem" }}>Instruction for Re-analysis</Title>
                    <TextArea
                        rows={4}
                        value={instruction}
                        onChange={(e) => setInstruction(e.target.value)}
                        placeholder="Enter custom instruction for LLM re-analysis"
                    />

                    <Button
                        type="primary"
                        danger
                        style={{ marginTop: "1rem" }}
                        onClick={handleReanalyze}
                        loading={submitting}
                    >
                        Re-analyze with Instruction
                    </Button>

                    <Title level={5} style={{ marginTop: "1.5rem" }}>Evaluated At</Title>
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
