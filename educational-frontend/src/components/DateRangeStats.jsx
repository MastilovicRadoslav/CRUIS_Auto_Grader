import { useState } from "react";
import { Card, DatePicker, Button, Typography, message } from "antd";
import { getStatisticsByDateRange } from "../services/statisticsService";

const { RangePicker } = DatePicker;
const { Title, Paragraph } = Typography;

const DateRangeStats = ({ token }) => {
  const [range, setRange] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);

  const fetchStats = async () => {
    if (range.length !== 2) {
      return message.warning("Please select a date range.");
    }

    const payload = {
      from: range[0].toISOString(),
      to: range[1].toISOString(),
    };

    try {
      setLoading(true);
      const result = await getStatisticsByDateRange(payload, token);
      setStats(result);
    } catch (err) {
      message.error("Failed to fetch statistics.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card style={{ marginBottom: "2rem" }}>
      <Title level={4}>Statistics by Date Range</Title>
      <RangePicker
        onChange={(values) => setRange(values)}
        style={{ marginRight: "1rem" }}
      />
      <Button type="primary" onClick={fetchStats} loading={loading}>
        Get Statistics
      </Button>

      {stats && (
        <div style={{ marginTop: "1.5rem" }}>
          <Paragraph><strong>Total Works:</strong> {stats.totalWorks}</Paragraph>
          <Paragraph><strong>Average Grade:</strong> {stats.averageGrade}</Paragraph>
          <Paragraph><strong>Above 90:</strong> {stats.above90}</Paragraph>
          <Paragraph><strong>Between 70 and 89:</strong> {stats.between70And89}</Paragraph>
          <Paragraph><strong>Below 70:</strong> {stats.below70}</Paragraph>

          <Title level={5}>Most Common Issues</Title>
          {stats.mostCommonIssues && Object.keys(stats.mostCommonIssues).length > 0 ? (
            <ul>
              {Object.entries(stats.mostCommonIssues).map(([issue, count]) => (
                <li key={issue}>
                  <strong>{issue}:</strong> {count}
                </li>
              ))}
            </ul>
          ) : (
            <Paragraph>No issues recorded.</Paragraph>
          )}
        </div>
      )}
    </Card>
  );
};

export default DateRangeStats;
