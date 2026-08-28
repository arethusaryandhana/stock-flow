import axios from 'axios';

export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export const api=axios.create({baseURL:import.meta.env.VITE_API_URL??'http://localhost:8080/api'});api.interceptors.request.use(c=>{const token=localStorage.getItem('stockflow_token');if(token)c.headers.Authorization=`Bearer ${token}`;return c});api.interceptors.response.use(r=>r,e=>{if(e.response?.status===401){for(const key of ['stockflow_token','stockflow_name','stockflow_role'])localStorage.removeItem(key);sessionStorage.clear();if(window.location.pathname!=='/login')window.location.replace('/login')}return Promise.reject(new Error(e.response?.data?.message??'Layanan belum dapat dihubungi.'))});
