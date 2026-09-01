import axios from 'axios'
import { beginRequest, endRequest } from './requestActivity'

export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const sessionKeys = ['stockflow_token', 'stockflow_name', 'stockflow_role']
export const SESSION_REDIRECT_EVENT = 'stockflow:session-redirect'

const loginRedirectDelayMs = 900
let loginRedirectPending = false

export function redirectToLoginWithLoading() {
  if (window.location.pathname === '/login' || loginRedirectPending) return

  loginRedirectPending = true
  window.dispatchEvent(new Event(SESSION_REDIRECT_EVENT))
  window.setTimeout(() => window.location.replace('/login'), loginRedirectDelayMs)
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:8080/api',
})

api.interceptors.request.use((config) => {
  beginRequest()
  const token = localStorage.getItem('stockflow_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => {
    endRequest()
    return response
  },
  (error) => {
    endRequest()
    const status = error.response?.status
    const requestUrl = error.config?.url ?? ''
    const isAuthRequest = requestUrl.startsWith('/auth/')

    if (status === 401 && !isAuthRequest) {
      sessionKeys.forEach((key) => localStorage.removeItem(key))
      sessionStorage.clear()
      redirectToLoginWithLoading()
    }

    return Promise.reject(new Error(error.response?.data?.message ?? 'Layanan belum dapat dihubungi.'))
  },
)
