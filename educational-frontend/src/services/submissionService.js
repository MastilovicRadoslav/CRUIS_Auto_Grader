import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

// Dobavljanje svih radova za student a
export const fetchMySubmissions = async (token) => {
  const response = await axios.get(`${API_URL}/submission/my`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return response.data;
};

// Slanje rada studenta
export const submitWork = async (formData, token) => {
  try {
    const response = await axios.post(`${API_URL}/submission/submit`, formData, {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "multipart/form-data",
      },
    });
    return response.data;
  } catch (err) {
    // Ovde vraćamo backend poruku kad je error
    if (err.response && err.response.data) {
      throw err.response.data; // npr. { error: "Submission limit reached..." }
    } else {
      throw { error: "Unknown error occurred" };
    }
  }
};

// Dobavljanje evaluacije za odredjeni rad
export const getFeedbackByWorkId = async (workId) => {
  const token = localStorage.getItem("token");
  const res = await axios.get(`${API_URL}/evaluation/feedback/${workId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return res.data;
};

//Dobavljanje svih radova za sve studente - Professor
export const fetchAllSubmissions = async (token) => {
  const response = await axios.get(`${API_URL}/submission/all`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
  return response.data;
};
