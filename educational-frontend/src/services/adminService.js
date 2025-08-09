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

// Sliding-window setting
export async function getSubmissionWindowSetting(token) {
  const res = await fetch(`${API_URL}/admin/settings/submission-window`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to fetch window setting");
  return res.json(); // { maxPerWindow, windowSizeDays }
}

export async function setSubmissionWindowSetting(payload, token) {
  const res = await fetch(`${API_URL}/admin/settings/submission-window`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to save window setting");
  return res.text();
}

