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

// Dodavanje rada od nekog studenta
export const submitWork = async (payload, token) => {
  const response = await axios.post(`${API_URL}/submission/submit`, payload, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  return response.data;
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
