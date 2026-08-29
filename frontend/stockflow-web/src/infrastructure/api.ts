import axios from 'axios'

export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const sessionKeys = ['stockflow_token', 'stockflow_name', 'stockflow_role']

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:8080/api',
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('stockflow_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status
    const requestUrl = error.config?.url ?? ''
    const isAuthRequest = requestUrl.startsWith('/auth/')

    if (status === 401 && !isAuthRequest) {
      sessionKeys.forEach((key) => localStorage.removeItem(key))
      sessionStorage.clear()

      if (window.location.pathname !== '/login') {
        window.location.replace('/login')
      }
    }

    return Promise.reject(new Error(error.response?.data?.message ?? 'Layanan belum dapat dihubungi.'))
  },
)
