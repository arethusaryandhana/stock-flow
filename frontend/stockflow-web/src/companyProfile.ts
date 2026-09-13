import { ref } from 'vue'
import { api } from './infrastructure/api'

export type CompanyProfile = {
  name: string
  address: string | null
  email: string | null
  phone: string | null
  currency: string
  logoUrl: string | null
  updatedAt: string | null
}

const defaults: CompanyProfile = {
  name: 'StockFlow Demo',
  address: null,
  email: null,
  phone: null,
  currency: 'IDR',
  logoUrl: null,
  updatedAt: null,
}

const profile = ref<CompanyProfile>({ ...defaults })
const loaded = ref(false)
let pendingLoad: Promise<CompanyProfile> | null = null

async function loadProfile(force = false) {
  if (loaded.value && !force) return profile.value
  if (pendingLoad) return pendingLoad
  pendingLoad = api.get<CompanyProfile>('/company-profile')
    .then(({ data }) => {
      profile.value = data
      loaded.value = true
      return data
    })
    .finally(() => { pendingLoad = null })
  return pendingLoad
}

async function saveProfile(next: Omit<CompanyProfile, 'updatedAt'>) {
  const { data } = await api.put<CompanyProfile>('/company-profile', next)
  profile.value = data
  loaded.value = true
  return data
}

export function useCompanyProfile() {
  return { profile, loaded, loadProfile, saveProfile }
}
