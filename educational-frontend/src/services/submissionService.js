import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

export const fetchMySubmissions = async (token) => {
  const response = await axios.get(`${API_URL}/submission/my`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return response.data;
};

export const submitWork = async (payload, token) => {
  const response = await axios.post(`${API_URL}/submission/submit`, payload, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return response.data;
};

export const getFeedbackByWorkId = async (workId) => {
  const token = localStorage.getItem("token");
  const res = await axios.get(`${API_URL}/evaluation/feedback/my/${workId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return res.data;
};