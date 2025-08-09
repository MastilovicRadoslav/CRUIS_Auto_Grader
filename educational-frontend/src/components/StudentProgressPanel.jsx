import { useEffect, useMemo, useState } from "react";
import { Card, Select, Typography, message, Spin } from "antd";
import { fetchStudentProgress } from "../services/progressService";
import ProgressChart from "./ProgressChart";

const { Title, Paragraph } = Typography;
const { Option } = Select;

const StudentProgressPanel = ({ token, submissions, selectedId, onChangeSelectedId, refreshKey }) => {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);

  // jedinstvena lista studenata izvedena iz submissions
  const students = useMemo(() => {
    const map = new Map();
    submissions.forEach((s) => {
      if (s.studentId && s.studentName) {
        map.set(s.studentId, s.studentName);
      }
    });
    return Array.from(map, ([value, label]) => ({ value, label }));
  }, [submissions]);

  // učitaj statistiku kad se promijeni student ili stigne refreshKey
  useEffect(() => {
    const load = async () => {
      if (!selectedId) return;
      setLoading(true);
      try {
        const data = await fetchStudentProgress(selectedId, token);
        setStats(data);
      } catch {
        message.error("Failed to load student's progress.");
      } finally {
        setLoading(false);
      }
    };
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId, token, refreshKey]);

  return (
    <Card title="📚 Student Progress">
      <div style={{ marginBottom: 12 }}>
        <Select
          showSearch
          placeholder="Select a student"
          value={selectedId || undefined}
          onChange={onChangeSelectedId}
          style={{ width: "100%" }}
          filterOption={(input, option) =>
            (option?.children ?? "").toLowerCase().includes(input.toLowerCase())
          }
        >
          {students.map((s) => (
            <Option key={s.value} value={s.value}>
              {s.label}
            </Option>
          ))}
        </Select>
      </div>

      {loading ? (
        <Spin />
      ) : stats ? (
        <>
          <Paragraph style={{ marginBottom: 8 }}>
            <b>Total Works:</b> {stats.totalWorks} &nbsp;|&nbsp; <b>Average Grade:</b> {stats.averageGrade}
          </Paragraph>
          <Paragraph style={{ marginBottom: 8 }}>
            <b>≥9:</b> {stats.above9} &nbsp;|&nbsp; <b>7–8:</b> {stats.between7And8} &nbsp;|&nbsp; <b>&lt;7:</b> {stats.below7}
          </Paragraph>

          <Card type="inner" title="📈 Grade Evolution">
            <ProgressChart data={stats} />
          </Card>
        </>
      ) : (
        <Paragraph>No stats for selected student.</Paragraph>
      )}
    </Card>
  );
};

export default StudentProgressPanel;
