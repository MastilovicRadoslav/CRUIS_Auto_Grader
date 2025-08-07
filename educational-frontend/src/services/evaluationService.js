import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

export const addProfessorComment = async (requestBody, token) => {
  const response = await axios.post(`${API_URL}/evaluation/professor-comment`, requestBody,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );
  return response.data;
};

export const reanalyzeSubmission = async (data, token) => {
  const response = await axios.post(
    "http://localhost:8285/api/evaluation/reanalyze",
    data,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );
  return response.data;
};
