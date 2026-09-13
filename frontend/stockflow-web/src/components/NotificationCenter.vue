<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useNotificationPreferences } from '../notificationPreferences'
import { useDisplayPreferences } from '../preferences'

type NotificationItem = {
  id: string
  type: string
  title: string
  message: string
  link: string | null
  isRead: boolean
  readAt: string | null
  createdAt: string
}

type NotificationPage = {
  items: NotificationItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  unreadCount: number
}

const displayPreferences = useDisplayPreferences()
const { preferences, loadPreferences } = useNotificationPreferences()
const items = ref<NotificationItem[]>([])
const unreadCount = ref(0)
const open = ref(false)
const loading = ref(false)
const markingAll = ref(false)
const error = ref('')
const root = ref<HTMLElement | null>(null)
const router = useRouter()
const { t } = useI18n()
let pollTimer: number | undefined
let mounted = false
let hasLoadedNotifications = false

const unreadLabel = computed(() => unreadCount.value > 99 ? '99+' : String(unreadCount.value))
const notificationsEnabled = computed(() => preferences.value.inAppEnabled)

function dateTime(value: string) {
  return displayPreferences.formatDate(value, { includeYear: false, includeTime: true })
}

function icon(type: string) {
  if (type === 'LowStock') return '!'
  if (type === 'ReportReady') return '↓'
  return 'i'
}

function playNotificationSound() {
  try {
    const context = new AudioContext()
    const oscillator = context.createOscillator()
    const gain = context.createGain()
    oscillator.type = 'sine'
    oscillator.frequency.setValueAtTime(660, context.currentTime)
    oscillator.frequency.exponentialRampToValueAtTime(880, context.currentTime + 0.08)
    gain.gain.setValueAtTime(0.0001, context.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.12, context.currentTime + 0.01)
    gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.12)
    oscillator.connect(gain)
    gain.connect(context.destination)
    oscillator.addEventListener('ended', () => { void context.close() })
    oscillator.start()
    oscillator.stop(context.currentTime + 0.13)
  } catch {
    // Browsers can deny audio before the user has interacted with the page.
  }
}

async function load(showLoading = false) {
  if (!notificationsEnabled.value) {
    items.value = []
    unreadCount.value = 0
    return
  }

  if (showLoading) loading.value = true
  try {
    const previousUnreadCount = unreadCount.value
    const { data } = await api.get<NotificationPage>('/notifications', {
      params: { page: 1, pageSize: 10 },
    })
    items.value = data.items
    unreadCount.value = data.unreadCount
    if (hasLoadedNotifications && preferences.value.soundEnabled && data.unreadCount > previousUnreadCount) {
      playNotificationSound()
    }
    hasLoadedNotifications = true
    error.value = ''
  } catch (requestError) {
    if (showLoading) error.value = (requestError as Error).message
  } finally {
    if (showLoading) loading.value = false
  }
}

async function toggle() {
  open.value = !open.value
  if (open.value && notificationsEnabled.value) await load(true)
}

async function openNotification(item: NotificationItem) {
  if (!item.isRead) {
    try {
      await api.patch(`/notifications/${item.id}/read`)
      item.isRead = true
      item.readAt = new Date().toISOString()
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch (requestError) {
      error.value = (requestError as Error).message
      return
    }
  }

  open.value = false
  if (item.link) await router.push(item.link)
}

async function markAllRead() {
  markingAll.value = true
  try {
    await api.post('/notifications/read-all')
    const now = new Date().toISOString()
    items.value = items.value.map((item) => ({ ...item, isRead: true, readAt: item.readAt ?? now }))
    unreadCount.value = 0
    error.value = ''
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    markingAll.value = false
  }
}

function closeOnOutsideClick(event: MouseEvent) {
  if (root.value && !root.value.contains(event.target as Node)) open.value = false
}

function closeOnEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') open.value = false
}

function schedulePolling() {
  if (pollTimer !== undefined) window.clearInterval(pollTimer)
  pollTimer = undefined
  if (!notificationsEnabled.value) return

  pollTimer = window.setInterval(() => {
    if (document.visibilityState === 'visible') void load()
  }, preferences.value.pollingIntervalSeconds * 1000)
}

watch(() => [
  preferences.value.inAppEnabled,
  preferences.value.lowStockEnabled,
  preferences.value.reportReadyEnabled,
  preferences.value.systemEnabled,
  preferences.value.pollingIntervalSeconds,
], () => {
  if (!mounted) return
  schedulePolling()
  if (notificationsEnabled.value) void load()
  else {
    items.value = []
    unreadCount.value = 0
  }
})

onMounted(async () => {
  try {
    await loadPreferences()
  } catch {
    // Keep the safe defaults so the notification inbox remains usable.
  }
  mounted = true
  await load()
  schedulePolling()
  document.addEventListener('click', closeOnOutsideClick)
  document.addEventListener('keydown', closeOnEscape)
})

onBeforeUnmount(() => {
  mounted = false
  document.removeEventListener('click', closeOnOutsideClick)
  document.removeEventListener('keydown', closeOnEscape)
  if (pollTimer !== undefined) window.clearInterval(pollTimer)
})
</script>

<template>
  <div ref="root" class="notification-center">
    <button
      class="topbar-icon notification-button"
      :class="{ paused: !notificationsEnabled }"
      type="button"
      :aria-label="t('app.notificationsAria')"
      :aria-expanded="open"
      aria-haspopup="dialog"
      @click="toggle"
    >
      <span aria-hidden="true">◔</span>
      <strong v-if="notificationsEnabled && unreadCount" class="notification-badge" aria-hidden="true">{{ unreadLabel }}</strong>
    </button>

    <section v-if="open" class="notification-dropdown" role="dialog" :aria-label="t('notifications.title')">
      <header>
        <div>
          <strong>{{ t('notifications.title') }}</strong>
          <small>{{ notificationsEnabled ? t('notifications.unreadCount', { count: unreadCount }) : t('notifications.pausedStatus') }}</small>
        </div>
        <button v-if="notificationsEnabled && unreadCount" type="button" :disabled="markingAll" @click="markAllRead">
          {{ markingAll ? t('notifications.markingAll') : t('notifications.markAll') }}
        </button>
      </header>

      <p v-if="error" class="notification-error" role="alert">{{ error }}</p>
      <div v-if="!notificationsEnabled" class="notification-empty paused">
        <span aria-hidden="true">—</span>
        <strong>{{ t('notifications.pausedTitle') }}</strong>
        <small>{{ t('notifications.pausedHint') }}</small>
      </div>
      <div v-else-if="loading" class="notification-empty">{{ t('notifications.loading') }}</div>
      <div v-else-if="!items.length" class="notification-empty">
        <span aria-hidden="true">✓</span>
        <strong>{{ t('notifications.emptyTitle') }}</strong>
        <small>{{ t('notifications.emptyHint') }}</small>
      </div>
      <div v-else class="notification-list">
        <button
          v-for="item in items"
          :key="item.id"
          class="notification-item"
          :class="{ unread: !item.isRead }"
          type="button"
          @click="openNotification(item)"
        >
          <span class="notification-type" :class="item.type.toLowerCase()" aria-hidden="true">{{ icon(item.type) }}</span>
          <span class="notification-copy">
            <span><strong>{{ item.title }}</strong><i v-if="!item.isRead" /></span>
            <small>{{ item.message }}</small>
            <time :datetime="item.createdAt">{{ dateTime(item.createdAt) }}</time>
          </span>
        </button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.notification-center { position: relative; }
.notification-button { font-size: 1.15rem; }
.notification-button.paused { opacity: .72; }
.notification-button > span { line-height: 1; }
.notification-badge { position: absolute; top: -7px; right: -8px; display: grid; min-width: 19px; height: 19px; padding: 0 5px; place-items: center; border: 2px solid var(--surface-raised); border-radius: 999px; color: #fff; background: #dc6672; font-size: .55rem; line-height: 1; }
.notification-dropdown { position: absolute; z-index: 40; top: calc(100% + 11px); right: -8px; width: min(390px, calc(100vw - 24px)); overflow: hidden; border: 1px solid var(--line); border-radius: 14px; background: var(--surface-raised); box-shadow: var(--shadow-lg, 0 18px 48px rgba(30, 42, 70, .2)); }
.notification-dropdown header { display: flex; min-height: 62px; align-items: center; justify-content: space-between; gap: 16px; padding: 12px 16px; border-bottom: 1px solid var(--line); }
.notification-dropdown header div { display: grid; gap: 3px; }
.notification-dropdown header strong { color: var(--text); font-size: .82rem; }
.notification-dropdown header small { color: var(--muted); font-size: .63rem; }
.notification-dropdown header button { padding: 6px 8px; border: 0; color: var(--blue); background: transparent; font-size: .62rem; font-weight: 800; }
.notification-dropdown header button:hover:not(:disabled) { border-radius: 7px; background: var(--blue-soft); }
.notification-dropdown header button:disabled { opacity: .55; }
.notification-error { margin: 10px 12px 0; padding: 8px 10px; border-radius: 8px; color: var(--red); background: var(--red-soft); font-size: .65rem; line-height: 1.4; }
.notification-list { max-height: min(470px, calc(100vh - 120px)); overflow-y: auto; }
.notification-item { display: flex; width: 100%; gap: 11px; padding: 13px 15px; border: 0; border-bottom: 1px solid var(--line); color: inherit; background: transparent; text-align: left; }
.notification-item:last-child { border-bottom: 0; }
.notification-item:hover { background: var(--surface-muted, rgba(87, 105, 130, .05)); }
.notification-item.unread { background: color-mix(in srgb, var(--blue-soft) 44%, var(--surface-raised)); }
.notification-type { display: grid; width: 32px; height: 32px; flex: 0 0 32px; place-items: center; border-radius: 9px; color: var(--blue); background: var(--blue-soft); font-size: .75rem; font-style: normal; font-weight: 900; }
.notification-type.lowstock { color: #a76c16; background: var(--amber-soft); }
.notification-type.reportready { color: #13816e; background: var(--teal-soft); }
.notification-copy { display: grid; min-width: 0; flex: 1; gap: 4px; }
.notification-copy > span { display: flex; align-items: center; gap: 7px; }
.notification-copy strong { overflow: hidden; color: var(--text); font-size: .7rem; text-overflow: ellipsis; white-space: nowrap; }
.notification-copy i { width: 6px; height: 6px; flex: 0 0 6px; border-radius: 50%; background: var(--blue); }
.notification-copy small { display: -webkit-box; overflow: hidden; color: var(--muted); font-size: .64rem; line-height: 1.45; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.notification-copy time { color: var(--muted); font-size: .56rem; font-weight: 700; }
.notification-empty { display: grid; min-height: 175px; place-items: center; align-content: center; gap: 5px; padding: 24px; color: var(--muted); text-align: center; }
.notification-empty > span { display: grid; width: 36px; height: 36px; margin-bottom: 4px; place-items: center; border-radius: 50%; color: #13816e; background: var(--teal-soft); }
.notification-empty.paused > span { color: var(--muted); background: var(--surface-hover); }
.notification-empty strong { color: var(--text); font-size: .72rem; }
.notification-empty small { font-size: .62rem; }
@media (max-width: 700px) { .notification-dropdown { position: fixed; top: 62px; right: 12px; left: 12px; width: auto; } }
</style>
