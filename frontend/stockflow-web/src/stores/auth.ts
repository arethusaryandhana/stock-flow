import { defineStore } from 'pinia'
import { hasPermission } from '../accessPolicy'
import { api, clearAccessToken, getAccessToken, redirectToLoginWithLoading, setAccessToken } from '../infrastructure/api'

type SessionProfile = { fullName: string; email: string; role: string }
type AccessSnapshot = { role: string; permissions: string[] }

const sessionKeys = ['stockflow_authenticated', 'stockflow_name', 'stockflow_email', 'stockflow_role']

export const useAuthStore = defineStore('auth', {
  state: () => ({
    authenticated: Boolean(getAccessToken()) && sessionStorage.getItem('stockflow_authenticated') === 'true',
    name: sessionStorage.getItem('stockflow_name') ?? '',
    email: sessionStorage.getItem('stockflow_email') ?? '',
    role: sessionStorage.getItem('stockflow_role') ?? '',
    permissions: [] as string[],
  }),
  getters: {
    isAdmin: (state) => state.role.trim().toLowerCase() === 'admin',
    can: (state) => (code: string) => hasPermission(state.permissions, code),
  },
  actions: {
    async login(email: string, password: string, rememberMe: boolean) {
      const { data } = await api.post<SessionProfile & { token: string }>('/auth/login', { email, password, rememberMe })
      setAccessToken(data.token, rememberMe)
      this.setSession(data)
      try {
        await this.refreshAccess()
      } catch (error) {
        this.clearSession()
        throw error
      }
    },
    async refreshAccess() {
      const { data } = await api.get<AccessSnapshot>('/auth/access')
      this.role = data.role
      this.permissions = data.permissions
      this.authenticated = true
      sessionStorage.setItem('stockflow_role', data.role)
    },
    setSession(profile: SessionProfile) {
      this.authenticated = true
      this.name = profile.fullName
      this.email = profile.email
      this.role = profile.role
      sessionStorage.setItem('stockflow_authenticated', 'true')
      sessionStorage.setItem('stockflow_name', profile.fullName)
      sessionStorage.setItem('stockflow_email', profile.email)
      sessionStorage.setItem('stockflow_role', profile.role)
    },
    clearSession() {
      sessionKeys.forEach((key) => sessionStorage.removeItem(key))
      clearAccessToken()
      this.authenticated = false
      this.name = ''
      this.email = ''
      this.role = ''
      this.permissions = []
    },
    async logout() {
      try {
        await api.post('/auth/logout')
      } catch {
        // An expired server session still needs the same local cleanup.
      } finally {
        this.clearSession()
        redirectToLoginWithLoading()
      }
    },
    async logoutAll() {
      await api.post('/auth/logout-all')
      this.clearSession()
      redirectToLoginWithLoading()
    },
  },
})
