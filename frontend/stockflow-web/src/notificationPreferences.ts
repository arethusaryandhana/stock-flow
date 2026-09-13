import { ref } from 'vue'
import { api } from './infrastructure/api'

export type NotificationPollingInterval = 15 | 30 | 60 | 300

export type NotificationPreferences = {
  inAppEnabled: boolean
  lowStockEnabled: boolean
  reportReadyEnabled: boolean
  systemEnabled: boolean
  soundEnabled: boolean
  pollingIntervalSeconds: NotificationPollingInterval
}

const defaults: NotificationPreferences = {
  inAppEnabled: true,
  lowStockEnabled: true,
  reportReadyEnabled: true,
  systemEnabled: true,
  soundEnabled: false,
  pollingIntervalSeconds: 30,
}

const preferences = ref<NotificationPreferences>({ ...defaults })
const loaded = ref(false)
let pendingLoad: Promise<NotificationPreferences> | null = null

async function loadPreferences(force = false) {
  if (loaded.value && !force) return preferences.value
  if (pendingLoad) return pendingLoad

  pendingLoad = api.get<NotificationPreferences>('/notifications/preferences')
    .then(({ data }) => {
      preferences.value = data
      loaded.value = true
      return data
    })
    .finally(() => {
      pendingLoad = null
    })
  return pendingLoad
}

async function savePreferences(next: NotificationPreferences) {
  const { data } = await api.put<NotificationPreferences>('/notifications/preferences', next)
  preferences.value = data
  loaded.value = true
  return data
}

export function useNotificationPreferences() {
  return { preferences, loaded, loadPreferences, savePreferences }
}
