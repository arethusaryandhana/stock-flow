<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { SESSION_REDIRECT_EVENT } from './infrastructure/api'
import { useAuthStore } from './stores/auth'
import { useToastStore } from './stores/toast'
import { useI18n } from './i18n'
import ThemeSwitcher from './components/ThemeSwitcher.vue'

const route = useRoute()
const auth = useAuthStore()
const toast = useToastStore()
const { language, t, toggleLanguage } = useI18n()
const mobileOpen = ref(false)
const sidebarCollapsed = ref(localStorage.getItem('stockflow_sidebar_collapsed') === 'true')
const menuGroupsStorageKey = 'stockflow_open_menu_groups'
const search = ref('')
const profileOpen = ref(false)
const profileWrap = ref<HTMLElement | null>(null)
const sessionRedirecting = ref(false)

const groups = [
  {
    labelKey: 'app.workspace',
    adminOnly: false,
    items: [{ labelKey: 'app.dashboard', path: '/', icon: '⌂', badge: '' }],
  },
  {
    labelKey: 'app.administration',
    adminOnly: true,
    items: [
      { labelKey: 'app.masterCategories', path: '/master-data/categories', icon: '◫', badge: '' },
      { labelKey: 'app.masterProducts', path: '/master-data/products', icon: '▦', badge: '' },
      { labelKey: 'app.masterSuppliers', path: '/master-data/suppliers', icon: '◎', badge: '' },
      { labelKey: 'app.masterCustomers', path: '/master-data/customers', icon: '◌', badge: '' },
    ],
  },
  {
    labelKey: 'app.inventory',
    adminOnly: false,
    items: [
      { labelKey: 'app.products', path: '/products', icon: '▦', badge: '' },
      { labelKey: 'app.movements', path: '/inventory/movements', icon: '↕', badge: '' },
      { labelKey: 'app.adjustments', path: '/inventory/adjustments', icon: '△', badge: '' },
    ],
  },
  {
    labelKey: 'app.operations',
    adminOnly: false,
    items: [
      { labelKey: 'app.purchaseOrders', path: null, icon: '▤', badge: 'app.soon' },
      { labelKey: 'app.receiving', path: null, icon: '↓', badge: 'app.soon' },
      { labelKey: 'app.suppliers', path: null, icon: '◎', badge: 'app.soon' },
    ],
  },
  {
    labelKey: 'app.insight',
    adminOnly: false,
    items: [
      { labelKey: 'app.reports', path: null, icon: '◷', badge: 'app.soon' },
      { labelKey: 'app.settings', path: null, icon: '⚙', badge: 'app.soon' },
    ],
  },
]

function findActiveGroupKey() {
  return groups.find((group) => group.items.some((item) => item.path === route.path))?.labelKey
}

function loadOpenMenuGroups() {
  const activeGroupKey = findActiveGroupKey()

  try {
    const stored = JSON.parse(localStorage.getItem(menuGroupsStorageKey) ?? '[]')
    const validKeys = new Set(groups.map((group) => group.labelKey))
    const openKeys = Array.isArray(stored)
      ? stored.filter((key): key is string => typeof key === 'string' && validKeys.has(key))
      : []

    if (activeGroupKey && !openKeys.includes(activeGroupKey)) openKeys.push(activeGroupKey)
    return new Set(openKeys)
  } catch {
    return new Set(activeGroupKey ? [activeGroupKey] : [])
  }
}

const openMenuGroups = ref(loadOpenMenuGroups())

const initials = computed(() =>
  (auth.name || t('app.defaultName'))
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase(),
)

function notify(message: string) {
  toast.info(message)
}

function closeMobileNav() {
  mobileOpen.value = false
}

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('stockflow_sidebar_collapsed', String(sidebarCollapsed.value))
}

function isMenuGroupOpen(labelKey: string) {
  return openMenuGroups.value.has(labelKey)
}

function toggleMenuGroup(labelKey: string) {
  const nextOpenGroups = new Set(openMenuGroups.value)
  if (nextOpenGroups.has(labelKey)) nextOpenGroups.delete(labelKey)
  else nextOpenGroups.add(labelKey)

  openMenuGroups.value = nextOpenGroups
  localStorage.setItem(menuGroupsStorageKey, JSON.stringify([...nextOpenGroups]))
}

watch(
  () => route.path,
  () => {
    const activeGroupKey = findActiveGroupKey()
    if (!activeGroupKey || openMenuGroups.value.has(activeGroupKey)) return

    openMenuGroups.value = new Set([...openMenuGroups.value, activeGroupKey])
    localStorage.setItem(menuGroupsStorageKey, JSON.stringify([...openMenuGroups.value]))
  },
)

function handleSearch() {
  if (search.value.trim()) notify(t('app.searchToast', { term: search.value.trim() }))
}

function logout() {
  profileOpen.value = false
  auth.logout()
}

function closeProfileOnOutsideClick(event: MouseEvent) {
  if (profileWrap.value && !profileWrap.value.contains(event.target as Node)) profileOpen.value = false
}

function closeProfileOnEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') profileOpen.value = false
}

function showSessionRedirectLoading() {
  sessionRedirecting.value = true
}

onMounted(() => {
  document.addEventListener('click', closeProfileOnOutsideClick)
  document.addEventListener('keydown', closeProfileOnEscape)
  window.addEventListener(SESSION_REDIRECT_EVENT, showSessionRedirectLoading)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', closeProfileOnOutsideClick)
  document.removeEventListener('keydown', closeProfileOnEscape)
  window.removeEventListener(SESSION_REDIRECT_EVENT, showSessionRedirectLoading)
})
</script>

<template>
  <Transition name="session-redirect">
    <div v-if="sessionRedirecting" class="session-redirect-overlay" role="alert" aria-live="assertive">
      <div class="session-redirect-card">
        <div class="session-redirect-mark" aria-hidden="true">
          <span class="session-redirect-spinner" />
          <img src="/stockflow-logo.svg?v=20260827" alt="">
        </div>
        <strong>{{ t('app.sessionExpiredTitle') }}</strong>
        <p>{{ t('app.sessionExpiredMessage') }}</p>
      </div>
    </div>
  </Transition>

  <router-view v-if="route.path === '/login'" />

  <div v-else class="app-shell">
    <div v-if="mobileOpen" class="mobile-backdrop" @click="closeMobileNav" />
    <aside class="sidebar" :class="{ 'sidebar-open': mobileOpen, 'sidebar-collapsed': sidebarCollapsed }">
      <div class="sidebar-head">
        <router-link class="brand-lockup" to="/" :aria-label="t('app.brandAria')" @click="closeMobileNav">
          <img class="brand-logo" src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true">
          <div>
            <strong>StockFlow</strong>
            <span>Inventory OS</span>
          </div>
        </router-link>
      </div>
      <button
        class="sidebar-toggle"
        type="button"
        :aria-label="sidebarCollapsed ? t('app.openSidebar') : t('app.closeSidebar')"
        :aria-expanded="!sidebarCollapsed"
        :title="sidebarCollapsed ? t('app.openSidebar') : t('app.closeSidebar')"
        @click="toggleSidebar"
      >
        <span class="sidebar-toggle-icon" aria-hidden="true">{{ sidebarCollapsed ? '>' : '<' }}</span>
      </button>

      <button class="workspace-switcher" type="button" :aria-label="t('app.workspaceName')" @click="notify(t('app.workspaceToast'))">
        <span class="workspace-symbol" aria-hidden="true">▦</span>
        <span class="workspace-copy"><small>{{ t('app.workspaceLabel') }}</small><strong>{{ t('app.workspaceName') }}</strong></span>
        <span class="workspace-chevron chevron-icon" aria-hidden="true" />
      </button>

      <nav class="sidebar-nav" :aria-label="t('app.mainNav')">
        <template v-for="group in groups" :key="group.labelKey">
          <section
            v-if="!group.adminOnly || auth.isAdmin"
            class="nav-group"
            :class="{ 'nav-group-collapsed': !isMenuGroupOpen(group.labelKey) }"
          >
            <button
              class="nav-group-toggle"
              type="button"
              :aria-expanded="isMenuGroupOpen(group.labelKey)"
              :aria-label="t(isMenuGroupOpen(group.labelKey) ? 'app.collapseMenuGroup' : 'app.expandMenuGroup', { group: t(group.labelKey) })"
              @click="toggleMenuGroup(group.labelKey)"
            >
              <span class="nav-group-label">{{ t(group.labelKey) }}</span>
              <span class="nav-group-chevron chevron-icon" aria-hidden="true" />
            </button>

            <div class="nav-group-items">
              <template v-for="item in group.items" :key="item.labelKey">
                <router-link
                  v-if="item.path"
                  :to="item.path"
                  class="nav-link"
                  :class="{ active: route.path === item.path }"
                  :title="sidebarCollapsed ? t(item.labelKey) : undefined"
                  @click="closeMobileNav"
                >
                  <span class="nav-icon">{{ item.icon }}</span>
                  <span>{{ t(item.labelKey) }}</span>
                </router-link>
                <button v-else class="nav-link nav-placeholder" type="button" :title="sidebarCollapsed ? t(item.labelKey) : undefined" @click="notify(t('app.comingSoonToast', { label: t(item.labelKey) }))">
                  <span class="nav-icon">{{ item.icon }}</span>
                  <span>{{ t(item.labelKey) }}</span>
                  <em>{{ t(item.badge) }}</em>
                </button>
              </template>
            </div>
          </section>
        </template>
      </nav>

      <div class="sidebar-bottom">
        <div class="sync-pill"><span class="status-dot" /><span class="sync-copy">{{ t('app.synced') }}</span></div>
        <button class="sidebar-help" type="button" :title="sidebarCollapsed ? t('app.help') : undefined" @click="notify(t('app.supportToast'))">
          <span class="help-icon">?</span>
          <span class="help-copy"><strong>{{ t('app.help') }}</strong><small>{{ t('app.learnStockflow') }}</small></span>
          <span class="arrow">↗</span>
        </button>
      </div>
    </aside>

    <div class="app-main">
      <header class="topbar">
        <div class="topbar-left">
          <button class="mobile-menu" type="button" :aria-label="t('app.mainNav')" @click="mobileOpen = true">☰</button>
          <router-link class="mobile-brand" to="/" :aria-label="t('app.brandAria')">
            <img src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true">
            <strong>StockFlow</strong>
          </router-link>
          <div class="breadcrumbs"><span>{{ t('app.workspaceName') }}</span><b>/</b><strong>{{ route.path === '/' ? t('app.dashboardBreadcrumb') : route.path.startsWith('/master-data') ? t('app.masterDataBreadcrumb') : t('app.inventoryBreadcrumb') }}</strong></div>
        </div>
        <div class="topbar-actions">
          <form class="global-search" @submit.prevent="handleSearch">
            <span class="search-glyph">⌕</span>
            <input v-model="search" :aria-label="t('app.searchAria')" :placeholder="t('app.searchPlaceholder')">
            <kbd>⌘ K</kbd>
          </form>
          <ThemeSwitcher />
          <button class="language-switcher" type="button" :aria-label="language === 'id' ? t('app.switchToEnglish') : t('app.switchToIndonesian')" @click="toggleLanguage"><span :class="{ active: language === 'en' }">EN</span><span :class="{ active: language === 'id' }">ID</span></button>
          <button class="topbar-icon help-button" type="button" :aria-label="t('app.helpAria')" @click="notify(t('app.helpToast'))">?</button>
          <button class="topbar-icon notification-button" type="button" :aria-label="t('app.notificationsAria')" @click="notify(t('app.notificationToast'))"><span />◔</button>
          <div ref="profileWrap" class="profile-wrap">
            <button class="profile-menu profile-trigger" type="button" :aria-expanded="profileOpen" :aria-label="t('app.profileAria')" @click="profileOpen = !profileOpen">
              <div class="profile-avatar">{{ initials }}</div>
              <div class="profile-copy"><strong>{{ auth.name || t('app.defaultName') }}</strong><small>{{ auth.role || t('app.defaultRole') }}</small></div>
              <span class="profile-chevron chevron-icon" aria-hidden="true" />
            </button>
            <div v-if="profileOpen" class="profile-dropdown">
              <div class="profile-dropdown-meta"><span class="profile-dropdown-label">{{ t('app.signedInAs') }}</span><strong>{{ auth.name || t('app.defaultName') }}</strong><small>{{ auth.role || t('app.defaultRole') }}</small></div>
              <button class="profile-logout" type="button" @click="logout"><span>↪</span> {{ t('app.logout') }}</button>
            </div>
          </div>
        </div>
      </header>

      <router-view />
    </div>

    <TransitionGroup name="toast" tag="div" class="toast-region" aria-live="polite">
      <div v-for="item in toast.items" :key="item.id" class="toast-message" :class="item.type" :role="item.type === 'error' ? 'alert' : 'status'">
        <span class="toast-icon" aria-hidden="true">{{ item.type === 'success' ? '✓' : item.type === 'error' ? '!' : 'i' }}</span>
        <div class="toast-copy">
          <strong>{{ t(`toast.${item.type}Title`) }}</strong>
          <p>{{ item.message }}</p>
        </div>
        <button type="button" :aria-label="t('toast.dismiss')" @click="toast.dismiss(item.id)">×</button>
      </div>
    </TransitionGroup>
  </div>
</template>
