import { defineStore } from 'pinia'
import { api, redirectToLoginWithLoading } from '../infrastructure/api'

const sessionKeys = ['stockflow_authenticated', 'stockflow_name', 'stockflow_role']

export const useAuthStore = defineStore('auth', {
  state: () => ({
    authenticated: sessionStorage.getItem('stockflow_authenticated') === 'true',
    name: sessionStorage.getItem('stockflow_name') ?? '',
    role: sessionStorage.getItem('stockflow_role') ?? '',
  }),
  getters: {
    isAdmin: (state) => state.role.trim().toLowerCase() === 'admin',
  },
  actions: {
    async login(email: string, password: string) {
      const { data } = await api.post('/auth/login', { email, password })
      this.authenticated = true
      this.name = data.fullName
      this.role = data.role
      sessionStorage.setItem('stockflow_authenticated', 'true')
      sessionStorage.setItem('stockflow_name', data.fullName)
      sessionStorage.setItem('stockflow_role', data.role)
      localStorage.removeItem('stockflow_token')
    },
    async logout() {
      try {
        await api.post('/auth/logout')
      } catch {
        // An expired server session still needs the same local cleanup.
      } finally {
        sessionKeys.forEach((key) => sessionStorage.removeItem(key))
        localStorage.removeItem('stockflow_token')
        this.authenticated = false
        this.name = ''
        this.role = ''
        redirectToLoginWithLoading()
      }
    },
  },
})
