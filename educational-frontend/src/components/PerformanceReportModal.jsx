import { useState, useMemo } from "react";
import { Modal, DatePicker, Select, Button, Typography, Card, message, Table, Divider } from "antd";
import { generatePerformanceReport } from "../services/evaluationService";
import ProgressChart from "./ProgressChart";

const { RangePicker } = DatePicker;
const { Option } = Select;
const { Paragraph, Title } = Typography;

const ALL_KEY = "__ALL__";

const PerformanceReportModal = ({ open, onClose, token, submissions, allowStudentFilter = true }) => {
  const [range, setRange] = useState([]);
  const [studentId, setStudentId] = useState(null); // null => svi studenti
  const [loading, setLoading] = useState(false);
  const [report, setReport] = useState(null);

  // jedinstveni studenti iz submissions
  const students = useMemo(() => {
    const map = new Map();
    submissions.forEach(s => {
      if (s.studentId && s.studentName) map.set(s.studentId, s.studentName);
    });
    return Array.from(map, ([value, label]) => ({ value, label }));
  }, [submissions]);

  const handleGenerate = async () => {
    if (!range?.length) return message.warning("Izaberi period (od–do).");
    const body = {
      from: range[0].startOf("day").toISOString(),
      to: range[1].endOf("day").toISOString(),
      ...(studentId ? { studentId } : {}) // ako je null/undefined, ne šaljemo studentId → svi studenti
    };

    setLoading(true);
    try {
      const data = await generatePerformanceReport(body, token);

      // normalizuj timeline (Tuple -> imenovana polja)
      const normalizedTimeline = (data.gradeTimeline || []).map(p => ({
        date: p.date ?? p.item1,
        grade: p.grade ?? p.item2,
      }));
      setReport({ ...data, gradeTimeline: normalizedTimeline });
    } catch (e) {
      console.error(e);
      message.error("Neuspelo generisanje izveštaja.");
    } finally {
      setLoading(false);
    }
  };

  const exportCsv = () => {
    if (!report) return;
    const lines = [];
    lines.push("Metric,Value");
    lines.push(`TotalWorks,${report.totalWorks}`);
    lines.push(`AverageGrade,${report.averageGrade}`);
    lines.push(`Above9,${report.above9}`);
    lines.push(`Between7And8,${report.between7And8}`);
    lines.push(`Below7,${report.below7}`);
    lines.push("");
    lines.push("GradeDistribution");
    Object.entries(report.gradeDistribution || {}).forEach(([g, c]) => lines.push(`${g},${c}`));
    lines.push("");
    lines.push("Timeline (Date,Grade)");
    (report.gradeTimeline || []).forEach(pt => lines.push(`${pt.date},${pt.grade}`));
    lines.push("");
    lines.push("MostCommonIssues (Issue,Count)");
    Object.entries(report.mostCommonIssues || {}).forEach(([i, c]) =>
      lines.push(`"${String(i).replace(/"/g, '""')}",${c}`)
    );

    const blob = new Blob([lines.join("\n")], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "performance-report.csv";
    a.click();
    URL.revokeObjectURL(url);
  };

  // pomoćna: mapiranje vrijednosti iz Select-a
  const handleStudentChange = (val) => {
    if (val === ALL_KEY) setStudentId(null); // ALL ⇒ null ⇒ backend dobija grupni izvještaj
    else setStudentId(val);
  };

  // izračun trenutne vrijednosti Select-a (da prikaže ALL kad je studentId null)
  const currentSelectValue = studentId ?? ALL_KEY;

  // priprema tabela podataka
  const gradeDistData = Object.entries(report?.gradeDistribution || {}).map(([grade, count]) => ({
    key: String(grade),
    grade,
    count,
  }));

  const issuesData = Object.entries(report?.mostCommonIssues || {}).map(([issue, count], idx) => ({
    key: String(idx),
    issue,
    count,
  }));

  return (
    <Modal open={open} onCancel={onClose} footer={null} title="Performance Report" width={900}>
      <div style={{ display: "grid", gridTemplateColumns: allowStudentFilter ? "1fr 1fr" : "1fr", gap: 12, marginBottom: 12 }}>
        <RangePicker onChange={setRange} style={{ width: "100%" }} />
        {allowStudentFilter && (
          <Select
            value={currentSelectValue}
            onChange={handleStudentChange}
            showSearch
            filterOption={(input, option) =>
              (option?.children ?? "").toLowerCase().includes(input.toLowerCase())
            }
          >
            <Option value={ALL_KEY}>Svi studenti</Option>
            {students.map(s => (
              <Option key={s.value} value={s.value}>{s.label}</Option>
            ))}
          </Select>
        )}
      </div>

      <div style={{ display: "flex", gap: 8 }}>
        <Button type="primary" onClick={handleGenerate} loading={loading}>
          Generate
        </Button>
        <Button disabled={!report} onClick={exportCsv}>
          Export CSV
        </Button>
      </div>

      {report && (
        <div style={{ marginTop: 16, display: "grid", gap: 12 }}>
          {/* Osnovne metrike */}
          <Card type="inner" title="📊 Summary">
            <Paragraph style={{ marginBottom: 8 }}>
              <b>Total Works:</b> {report.totalWorks} &nbsp;|&nbsp;
              <b>Average Grade:</b> {report.averageGrade}
            </Paragraph>
            <Paragraph style={{ marginBottom: 0 }}>
              <b>≥9:</b> {report.above9} &nbsp;|&nbsp;
              <b>7–8:</b> {report.between7And8} &nbsp;|&nbsp;
              <b>&lt;7:</b> {report.below7}
            </Paragraph>
          </Card>

          {/* Distribucija ocjena */}
          <Card type="inner" title="📦 Grade Distribution">
            <Table
              size="small"
              pagination={false}
              dataSource={gradeDistData}
              columns={[
                { title: "Grade", dataIndex: "grade", key: "grade" },
                { title: "Count", dataIndex: "count", key: "count" },
              ]}
            />
          </Card>

          {/* Najčešći problemi */}
          <Card type="inner" title="🧩 Most Common Issues">
            <Table
              size="small"
              pagination={false}
              dataSource={issuesData}
              columns={[
                { title: "Issue", dataIndex: "issue", key: "issue" },
                { title: "Count", dataIndex: "count", key: "count" },
              ]}
            />
          </Card>

          {/* Timeline graf */}
          <Card type="inner" title="📈 Grade Evolution">
            <ProgressChart data={report} />
          </Card>
        </div>
      )}
    </Modal>
  );
};

export default PerformanceReportModal;
