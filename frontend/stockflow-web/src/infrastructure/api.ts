import axios from 'axios';
export const api=axios.create({baseURL:import.meta.env.VITE_API_URL??'http://localhost:8080/api'});api.interceptors.request.use(c=>{const token=localStorage.getItem('stockflow_token');if(token)c.headers.Authorization=`Bearer ${token}`;return c});api.interceptors.response.use(r=>r,e=>Promise.reject(new Error(e.response?.data?.message??'Layanan belum dapat dihubungi.')));
