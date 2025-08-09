// src/components/StudentProgressPanel.jsx
import { useEffect, useMemo, useState } from "react";
import { Card, Select, Typography, message, Spin } from "antd";
import { fetchStudentProgress, fetchAllStats } from "../services/progressService";
import ProgressChart from "./ProgressChart";

const { Paragraph } = Typography;
const { Option } = Select;

const ALL_KEY = "__ALL__";

const StudentProgressPanel = ({ token, submissions, selectedId, onChangeSelectedId, refreshKey }) => {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(false);

    // jedinstvena lista studenata (iz submissions)
    const studentOptions = useMemo(() => {
        const map = new Map();
        submissions.forEach((s) => {
            if (s.studentId && s.studentName) map.set(s.studentId, s.studentName);
        });
        return Array.from(map, ([value, label]) => ({ value, label }));
    }, [submissions]);

    // već imaš:
    useEffect(() => {
        if (!selectedId) onChangeSelectedId?.(ALL_KEY);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    // dodaj i ovo:
    useEffect(() => {
        if (!selectedId && submissions?.length >= 0) {
            onChangeSelectedId?.(ALL_KEY);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [submissions]);

    // učitaj statistiku kad se promijeni izbor ili stigne refresh
    useEffect(() => {
        const load = async () => {
            if (!selectedId) return;
            setLoading(true);
            try {
                const data =
                    selectedId === ALL_KEY
                        ? await fetchAllStats(token) // GET /api/evaluation/statistics
                        : await fetchStudentProgress(selectedId, token); // GET /api/evaluation/statistics/student/{id}

                // normalizuj timeline (podržava i Tuple i {date, grade})
                const normalizedTimeline = (data.gradeTimeline || []).map((p) => ({
                    date: p.date ?? p.item1,
                    grade: p.grade ?? p.item2,
                }));

                setStats({ ...data, gradeTimeline: normalizedTimeline });
            } catch (e) {
                console.error(e);
                message.error("Failed to load statistics.");
            } finally {
                setLoading(false);
            }
        };
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedId, token, refreshKey]);

    return (
        <Card title="📚 Student / Group Performance">
            <div style={{ marginBottom: 12 }}>
                <Select
                    showSearch
                    placeholder="Select student or All"
                    value={selectedId || undefined}
                    onChange={onChangeSelectedId}
                    style={{ width: "100%" }}
                    filterOption={(input, option) =>
                        (option?.children ?? "").toLowerCase().includes(input.toLowerCase())
                    }
                >
                    {/* Uvek prisutna ALL opcija */}
                    <Option key={ALL_KEY} value={ALL_KEY}>
                        Svi studenti
                    </Option>

                    {/* Dinamični studenti */}
                    {studentOptions.map((s) => (
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
                <Paragraph>No stats.</Paragraph>
            )}
        </Card>
    );
};

export default StudentProgressPanel;
