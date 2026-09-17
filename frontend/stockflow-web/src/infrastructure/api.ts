import axios from 'axios'
import { beginRequest, endRequest } from './requestActivity'

export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

const sessionKeys = ['stockflow_authenticated', 'stockflow_name', 'stockflow_email', 'stockflow_role']
const tokenKey = 'stockflow_token'
export const SESSION_REDIRECT_EVENT = 'stockflow:session-redirect'

export function getAccessToken() {
  return sessionStorage.getItem(tokenKey) ?? localStorage.getItem(tokenKey)
}

export function setAccessToken(token: string, rememberMe: boolean) {
  clearAccessToken()
  const storage = rememberMe ? localStorage : sessionStorage
  storage.setItem(tokenKey, token)
}

export function clearAccessToken() {
  sessionStorage.removeItem(tokenKey)
  localStorage.removeItem(tokenKey)
}

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
  const token = getAccessToken()
  if (token) config.headers.set('Authorization', `Bearer ${token}`)
  return config
})

api.interceptors.response.use(
  (response) => {
    endRequest()
    return response
  },
  async (error) => {
    endRequest()
    const status = error.response?.status
    const requestUrl = error.config?.url ?? ''
    const requestPath = requestUrl.split('?')[0]
    const isPublicAuthRequest = ['/auth/login', '/auth/forgot-password', '/auth/reset-password']
      .includes(requestPath)
    const isSessionProbe = requestPath === '/auth/session'

    if (status === 401 && !isPublicAuthRequest) {
      sessionKeys.forEach((key) => sessionStorage.removeItem(key))
      clearAccessToken()
      if (!isSessionProbe) redirectToLoginWithLoading()
    }

    let message = error.response?.data?.message as string | undefined
    if (!message && error.response?.data instanceof Blob) {
      try {
        const payload = JSON.parse(await error.response.data.text()) as { message?: string }
        message = payload.message
      } catch {
        // Keep the generic fallback when a binary response is not a JSON error.
      }
    }

    return Promise.reject(new Error(message ?? 'Layanan belum dapat dihubungi.'))
  },
)
