import { defineStore } from 'pinia'
import { api, redirectToLoginWithLoading } from '../infrastructure/api'

type SessionProfile = { fullName: string; email: string; role: string }

const sessionKeys = ['stockflow_authenticated', 'stockflow_name', 'stockflow_email', 'stockflow_role']

export const useAuthStore = defineStore('auth', {
  state: () => ({
    authenticated: sessionStorage.getItem('stockflow_authenticated') === 'true',
    name: sessionStorage.getItem('stockflow_name') ?? '',
    email: sessionStorage.getItem('stockflow_email') ?? '',
    role: sessionStorage.getItem('stockflow_role') ?? '',
  }),
  getters: {
    isAdmin: (state) => state.role.trim().toLowerCase() === 'admin',
  },
  actions: {
    async login(email: string, password: string) {
      const { data } = await api.post<SessionProfile & { token: string }>('/auth/login', { email, password })
      this.setSession(data)
      localStorage.removeItem('stockflow_token')
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
      localStorage.removeItem('stockflow_token')
      this.authenticated = false
      this.name = ''
      this.email = ''
      this.role = ''
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
