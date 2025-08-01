import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

export const getStatisticsByDateRange = async (payload, token) => {
  const res = await axios.post(`${API_URL}/evaluation/statistics/date-range`, payload, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return res.data;
};
