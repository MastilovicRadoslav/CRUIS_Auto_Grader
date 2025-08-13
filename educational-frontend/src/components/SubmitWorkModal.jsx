import { useState } from "react";
import { Modal, Form, Button, message, Upload, Typography } from "antd";
import { UploadOutlined } from "@ant-design/icons";
import { submitWork } from "../services/submissionService";
import { useAuth } from "../context/AuthContext";
import "../styles/SubmitWorkModal.css";

const { Text } = Typography;

const SubmitWorkModal = ({ visible, onClose, onSuccess }) => {
  const [form] = Form.useForm();
  const { token, userId } = useAuth();
  const [loading, setLoading] = useState(false);
  const [fileName, setFileName] = useState("");

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      const file = values.file;
      if (!file) {
        message.warning("Please upload a file.");
        return;
      }

      const formData = new FormData();
      formData.append("file", file);
      formData.append("title", file.name.split(".")[0]);
      formData.append("studentId", userId);

      // Zadržavam tvoj postojeći flow: zatvaramo modal odmah
      onClose?.();

      await submitWork(formData, token);

      message.success("Successfully submitted work!");
      form.resetFields();
      setFileName("");
      onSuccess?.();
    } catch (err) {
      message.error(err?.error || "Failed to submit work.");
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
      className="swm-modal"
    >
      <Form form={form} layout="vertical" onFinish={handleSubmit}>
        <Form.Item
          label="File"
          name="file"
          valuePropName="file"
          getValueFromEvent={(e) => {
            // očekujemo 1 fajl; vrati čist File objekat
            const f = e?.fileList?.[0]?.originFileObj;
            if (f) setFileName(f.name);
            return f;
          }}
          rules={[{ required: true, message: "Please upload a file" }]}
        >
          <Upload
            beforeUpload={() => false}
            maxCount={1}
            accept=".txt,.pdf,.doc,.docx,.zip"
            showUploadList={false}
          >
            <Button icon={<UploadOutlined />} size="large">
              Click to Upload
            </Button>
          </Upload>
        </Form.Item>

        {fileName && (
          <div className="swm-filehint">
            <Text type="secondary">Selected file:</Text>{" "}
            <Text className="swm-filename">{fileName}</Text>
          </div>
        )}

        <Form.Item style={{ marginBottom: 0 }}>
          <Button
            type="primary"
            htmlType="submit"
            block
            loading={loading}
            size="large"
          >
            Submit
          </Button>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default SubmitWorkModal;
