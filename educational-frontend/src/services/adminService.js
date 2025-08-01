import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

//Testirano
export const getAllUsers = async (token) => {
  const res = await axios.get(`${API_URL}/admin/users`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

export const createUser = async (data, token) => {
  const res = await axios.post(`${API_URL}/admin/create-user`, data, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

// Testirano
export const updateUser = async (id, data, token) => {
  const res = await axios.put(`${API_URL}/admin/user/${id}`, data, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

// Testirano
export const deleteUser = async (id, token) => {
  const res = await axios.delete(`${API_URL}/admin/user/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

// Testirano
export const getMaxSubmissions = async (token) => {
  const res = await axios.get(`${API_URL}/admin/settings/max-submissions`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};

// Testirano
export const setMaxSubmissions = async (value, token) => {
  const res = await axios.post(`${API_URL}/admin/settings/max-submissions`, { maxPerStudent: value }, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return res.data;
};
