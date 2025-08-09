import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

// Statistika za pojedinacnog studenta 
export const fetchStudentProgress = async (studentId, token) => {
  const response = await axios.get(
    `${API_URL}/evaluation/statistics/student/${studentId}`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );
  return response.data;
};

// Statistika za sve studente 
export const fetchAllStats = async (token) => {
  const res = await axios.get("http://localhost:8285/api/evaluation/statistics", {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};