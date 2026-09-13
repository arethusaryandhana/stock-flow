import { ref } from 'vue'
import { api, type PagedResponse } from './infrastructure/api'

export type ManagedUser = {
  id: string
  fullName: string
  email: string
  role: string
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export type ManagedUserRequest = {
  fullName: string
  email: string
  password: string | null
  role: string
  isActive: boolean
}

export type RoleOption = { name: string }

const users = ref<PagedResponse<ManagedUser>>({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 })
const roles = ref<RoleOption[]>([])

async function loadUsers(params: { page: number; pageSize: number; search?: string; role?: string; status?: string }) {
  const { data } = await api.get<PagedResponse<ManagedUser>>('/users', { params })
  users.value = data
  return data
}

async function loadRoles() {
  const { data } = await api.get<RoleOption[]>('/users/roles')
  roles.value = data
  return data
}

async function createUser(request: ManagedUserRequest) {
  const { data } = await api.post<ManagedUser>('/users', request)
  return data
}

async function updateUser(id: string, request: ManagedUserRequest) {
  const { data } = await api.put<ManagedUser>(`/users/${id}`, request)
  return data
}

export function useUserManagement() {
  return { users, roles, loadUsers, loadRoles, createUser, updateUser }
}
