import axios from "axios";

export const fetchStudentProgress = async (studentId, token) => {
  const response = await axios.get(
    `http://localhost:8285/api/evaluation/statistics/student/${studentId}`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );
  return response.data;
};
