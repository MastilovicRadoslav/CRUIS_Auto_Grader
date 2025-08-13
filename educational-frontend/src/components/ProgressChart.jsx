import React from "react";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid
} from "recharts";
import "../styles/ProgressChart.css";

const ProgressChart = ({ data }) => {
  // data.gradeTimeline: [{date, grade}] ili [{item1, item2}]
  const points = (data?.gradeTimeline || [])
    .map((p) => {
      const dateStr = p.date ?? p.item1;
      const gradeNum = Number(p.grade ?? p.item2);
      const ts = dateStr ? new Date(dateStr).getTime() : 0;
      return {
        ts,
        dateLabel: dateStr ? new Date(dateStr).toLocaleString() : "",
        grade: Number.isFinite(gradeNum) ? gradeNum : 0,
      };
    })
    .filter((p) => p.ts > 0)
    .sort((a, b) => a.ts - b.ts);

  if (!points.length) {
    return <div className="pc-empty">No data for chart.</div>;
  }

  return (
    <div className="pc-wrap">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={points} margin={{ top: 10, right: 20, bottom: 0, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="dateLabel" tick={{ fontSize: 12 }} minTickGap={20} />
          <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
          <Tooltip />
          <Line type="monotone" dataKey="grade" dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
};

export default ProgressChart;
