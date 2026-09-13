import { ref } from 'vue'
import { api } from './infrastructure/api'

export type InventorySettings = {
  defaultReorderLevel: number
  defaultUnit: string
  allowNegativeStock: boolean
  globalLowStockThreshold: number
}

const defaults: InventorySettings = {
  defaultReorderLevel: 5,
  defaultUnit: 'pcs',
  allowNegativeStock: false,
  globalLowStockThreshold: 0,
}

const settings = ref<InventorySettings>({ ...defaults })
const loaded = ref(false)
let pendingLoad: Promise<InventorySettings> | null = null

async function loadSettings(force = false) {
  if (loaded.value && !force) return settings.value
  if (pendingLoad) return pendingLoad

  pendingLoad = api.get<InventorySettings>('/inventory-settings')
    .then(({ data }) => {
      settings.value = data
      loaded.value = true
      return data
    })
    .finally(() => {
      pendingLoad = null
    })
  return pendingLoad
}

async function saveSettings(next: InventorySettings) {
  const { data } = await api.put<InventorySettings>('/inventory-settings', next)
  settings.value = data
  loaded.value = true
  return data
}

export function useInventorySettings() {
  return { settings, loaded, loadSettings, saveSettings }
}
