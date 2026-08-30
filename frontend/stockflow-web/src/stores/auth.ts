import { defineStore } from 'pinia'
import { api, redirectToLoginWithLoading } from '../infrastructure/api'

const sessionKeys = ['stockflow_token', 'stockflow_name', 'stockflow_role']

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('stockflow_token'),
    name: localStorage.getItem('stockflow_name') ?? '',
    role: localStorage.getItem('stockflow_role') ?? '',
  }),
  getters: {
    isAdmin: (state) => state.role.trim().toLowerCase() === 'admin',
  },
  actions: {
    async login(email: string, password: string) {
      const { data } = await api.post('/auth/login', { email, password })
      this.token = data.token
      this.name = data.fullName
      this.role = data.role
      localStorage.setItem('stockflow_token', data.token)
      localStorage.setItem('stockflow_name', data.fullName)
      localStorage.setItem('stockflow_role', data.role)
    },
    logout() {
      sessionKeys.forEach((key) => localStorage.removeItem(key))
      sessionStorage.clear()
      this.token = null
      this.name = ''
      this.role = ''
      redirectToLoginWithLoading()
    },
  },
})
