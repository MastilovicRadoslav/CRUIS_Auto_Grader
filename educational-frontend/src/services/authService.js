import axios from "axios";

const API_URL = import.meta.env.VITE_API_URL;

export const loginUser = async (credentials) => {
  return await axios.post(`${API_URL}/users/login`, credentials);
};

export const registerUser = async (credentials) => {
  return await axios.post(`${API_URL}/users/register`, credentials);
};
