import { useState } from "react";
import { Modal, Form, Button, message, Upload } from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { submitWork } from "../services/submissionService";
import { useAuth } from "../context/AuthContext";

const SubmitWorkModal = ({ visible, onClose, onSuccess }) => {
  const [form] = Form.useForm();
  const { token, userId } = useAuth();
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      const file = values.file;

      const formData = new FormData();
      formData.append("file", file);
      formData.append("title", file.name.split(".")[0]); // ime fajla bez ekstenzije
      formData.append("studentId", userId);

      await submitWork(formData, token);

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
          label="File"
          name="file"
          valuePropName="file"
          getValueFromEvent={(e) => {
            if (Array.isArray(e)) return e;
            return e?.fileList?.[0]?.originFileObj;
          }}
          rules={[{ required: true, message: "Please upload a file" }]}
        >
          <Upload beforeUpload={() => false} maxCount={1}>
            <Button icon={<UploadOutlined />}>Click to Upload</Button>
          </Upload>
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
