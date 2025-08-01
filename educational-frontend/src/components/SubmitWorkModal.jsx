import { useState } from "react";
import { Modal, Form, Input, Button, message } from "antd";
import { submitWork } from "../services/submissionService";
import { useAuth } from "../context/AuthContext";

const SubmitWorkModal = ({ visible, onClose, onSuccess }) => {
  const [form] = Form.useForm();
  const { token , userId} = useAuth(); // 
  const [loading, setLoading] = useState(false);

const handleSubmit = async (values) => {
  setLoading(true);
  try {
    const payload = {
      ...values,
      studentId: userId, // dodaj userId u payload
    };
    await submitWork(payload, token); 
    message.success("Work submitted successfully!");
    form.resetFields();
    onSuccess();
    onClose();
  } catch (err) {
    message.error("Submission failed.");
  } finally {
    setLoading(false);
  }
};

  return (
    <Modal
      title="Submit New Work"
      open={visible}
      onCancel={onClose}
      footer={null}
      destroyOnClose
    >
      <Form form={form} layout="vertical" onFinish={handleSubmit}>
        <Form.Item
          label="Title"
          name="title"
          rules={[{ required: true, message: "Please enter a title" }]}
        >
          <Input />
        </Form.Item>
        <Form.Item
          label="Content"
          name="content"
          rules={[{ required: true, message: "Please enter content" }]}
        >
          <Input.TextArea rows={4} />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={loading}>
            Submit
          </Button>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default SubmitWorkModal;
