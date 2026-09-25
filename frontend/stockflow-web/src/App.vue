<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { SESSION_REDIRECT_EVENT } from './infrastructure/api'
import { isRequestPending } from './infrastructure/requestActivity'
import { useAuthStore } from './stores/auth'
import { useToastStore } from './stores/toast'
import { useI18n } from './i18n'
import { visibleMenuGroups } from './accessPolicy'
import ThemeSwitcher from './components/ThemeSwitcher.vue'
import ChangePasswordModal from './components/ChangePasswordModal.vue'
import NotificationCenter from './components/NotificationCenter.vue'

const route = useRoute()
const auth = useAuthStore()
const toast = useToastStore()
const { language, t, toggleLanguage } = useI18n()
const mobileOpen = ref(false)
const sidebarCollapsed = ref(localStorage.getItem('stockflow_sidebar_collapsed') === 'true')
const menuGroupsStorageKey = 'stockflow_open_menu_groups'
const search = ref('')
const profileOpen = ref(false)
const changePasswordOpen = ref(false)
const profileWrap = ref<HTMLElement | null>(null)
const sessionRedirecting = ref(false)
const topbarScrolled = ref(false)

const groups = [
  {
    labelKey: 'app.workspace',
    items: [{ labelKey: 'app.dashboard', path: '/', icon: '⌂', badge: '', permission: 'menu.dashboard' }],
  },
  {
    labelKey: 'app.administration',
    items: [
      { labelKey: 'app.masterCategories', path: '/master-data/categories', icon: '◫', badge: '', permission: 'menu.master.categories' },
      { labelKey: 'app.masterProducts', path: '/master-data/products', icon: '▦', badge: '', permission: 'menu.master.products' },
      { labelKey: 'app.masterSuppliers', path: '/master-data/suppliers', icon: '◎', badge: '', permission: 'menu.master.suppliers' },
      { labelKey: 'app.masterCustomers', path: '/master-data/customers', icon: '◌', badge: '', permission: 'menu.master.customers' },
      { labelKey: 'app.auditHistory', path: '/admin/audit-history', icon: '≡', badge: '', permission: 'menu.audit' },
    ],
  },
  {
    labelKey: 'app.accessManagement',
    items: [
      { labelKey: 'app.users', path: '/admin/users', icon: '♙', badge: '', permission: 'menu.users' },
      { labelKey: 'app.roles', path: '/admin/roles', icon: '◇', badge: '', permission: 'menu.roles' },
      { labelKey: 'app.menuAccess', path: '/admin/access', icon: '◈', badge: '', permission: 'menu.access' },
      { labelKey: 'app.accessHistory', path: '/admin/access-history', icon: '≡', badge: '', permission: 'menu.access-history' },
    ],
  },
  {
    labelKey: 'app.inventory',
    items: [
      { labelKey: 'app.products', path: '/products', icon: '▦', badge: '', permission: 'menu.products' },
      { labelKey: 'app.movements', path: '/inventory/movements', icon: '↕', badge: '', permission: 'menu.movements' },
      { labelKey: 'app.adjustments', path: '/inventory/adjustments', icon: '△', badge: '', permission: 'menu.adjustments' },
    ],
  },
  {
    labelKey: 'app.operations',
    items: [
      { labelKey: 'app.purchaseOrders', path: '/operations/purchase-orders', icon: '▤', badge: '', permission: 'menu.purchase-orders' },
      { labelKey: 'app.salesOrders', path: '/operations/sales-orders', icon: '↑', badge: '', permission: 'menu.sales-orders' },
      { labelKey: 'app.receiving', path: '/operations/receiving', icon: '↓', badge: '', permission: 'menu.receiving' },
      { labelKey: 'app.suppliers', path: '/operations/suppliers', icon: '◎', badge: '', permission: 'menu.suppliers' },
    ],
  },
  {
    labelKey: 'app.insight',
    items: [
      { labelKey: 'app.reports', path: '/reports', icon: '◷', badge: '', permission: 'menu.reports' },
      { labelKey: 'app.settings', path: '/settings', icon: '⚙', badge: '', permission: 'menu.settings' },
    ],
  },
]
const visibleGroups = computed(() => visibleMenuGroups(groups, auth.permissions))

const breadcrumbGroupLabels: Record<string, string> = {
  'app.administration': 'app.masterDataBreadcrumb',
  'app.accessManagement': 'app.accessManagement',
  'app.inventory': 'app.inventoryBreadcrumb',
  'app.operations': 'app.operationsBreadcrumb',
  'app.insight': 'app.insight',
}

const breadcrumbs = computed(() => {
  const items = [{ labelKey: 'app.workspaceName', to: '/' }]
  const group = visibleGroups.value.find((entry) => entry.items.some((item) => item.path === route.path))
  if (!group) return items

  const groupHome = group.items[0]
  if (groupHome.path !== route.path && breadcrumbGroupLabels[group.labelKey]) {
    items.push({ labelKey: breadcrumbGroupLabels[group.labelKey], to: groupHome.path })
  }

  const currentPage = group.items.find((item) => item.path === route.path)!
  items.push({ labelKey: currentPage.labelKey, to: currentPage.path })
  return items
})

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

function openChangePassword() {
  profileOpen.value = false
  changePasswordOpen.value = true
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

function updateTopbarScrollState() {
  topbarScrolled.value = window.scrollY > 8
}

onMounted(() => {
  document.addEventListener('click', closeProfileOnOutsideClick)
  document.addEventListener('keydown', closeProfileOnEscape)
  window.addEventListener(SESSION_REDIRECT_EVENT, showSessionRedirectLoading)
  window.addEventListener('scroll', updateTopbarScrollState, { passive: true })
  updateTopbarScrollState()
})

onBeforeUnmount(() => {
  document.removeEventListener('click', closeProfileOnOutsideClick)
  document.removeEventListener('keydown', closeProfileOnEscape)
  window.removeEventListener(SESSION_REDIRECT_EVENT, showSessionRedirectLoading)
  window.removeEventListener('scroll', updateTopbarScrollState)
})
</script>

<template>
  <Transition name="request-loading">
    <div v-if="isRequestPending && !sessionRedirecting" class="request-loading-indicator" role="status" aria-live="polite">
      <span class="request-loading-spinner" aria-hidden="true"><i /><i /><i /></span>
      <span>{{ t('app.processingRequest') }}</span>
    </div>
  </Transition>

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
        <svg
          class="sidebar-toggle-icon"
          :class="{ 'sidebar-toggle-icon-collapsed': sidebarCollapsed }"
          viewBox="0 0 16 16"
          fill="none"
          aria-hidden="true"
        >
          <path d="M10 3.5 5.5 8l4.5 4.5" />
        </svg>
      </button>

      <button class="workspace-switcher" type="button" :aria-label="t('app.workspaceName')" @click="notify(t('app.workspaceToast'))">
        <span class="workspace-symbol" aria-hidden="true">▦</span>
        <span class="workspace-copy"><small>{{ t('app.workspaceLabel') }}</small><strong>{{ t('app.workspaceName') }}</strong></span>
        <span class="workspace-chevron chevron-icon" aria-hidden="true" />
      </button>

      <nav class="sidebar-nav" :aria-label="t('app.mainNav')">
        <template v-for="group in visibleGroups" :key="group.labelKey">
          <section
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
      <header class="topbar" :class="{ 'topbar-scrolled': topbarScrolled }">
        <div class="topbar-left">
          <button class="mobile-menu" type="button" :aria-label="t('app.mainNav')" @click="mobileOpen = true">☰</button>
          <router-link class="mobile-brand" to="/" :aria-label="t('app.brandAria')">
            <img src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true">
            <strong>StockFlow</strong>
          </router-link>
          <nav class="breadcrumbs" :aria-label="t('app.breadcrumbs')">
            <template v-for="(item, index) in breadcrumbs" :key="`${index}-${item.to}`">
              <b v-if="index" aria-hidden="true">/</b>
              <router-link :to="item.to" :aria-current="index === breadcrumbs.length - 1 ? 'page' : undefined">{{ t(item.labelKey) }}</router-link>
            </template>
          </nav>
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
          <NotificationCenter />
          <div ref="profileWrap" class="profile-wrap">
            <button class="profile-menu profile-trigger" type="button" :aria-expanded="profileOpen" :aria-label="t('app.profileAria')" @click="profileOpen = !profileOpen">
              <div class="profile-avatar">{{ initials }}</div>
              <div class="profile-copy"><strong>{{ auth.name || t('app.defaultName') }}</strong><small>{{ auth.role || t('app.defaultRole') }}</small></div>
              <span class="profile-chevron chevron-icon" aria-hidden="true" />
            </button>
            <div v-if="profileOpen" class="profile-dropdown">
              <div class="profile-dropdown-meta"><span class="profile-dropdown-label">{{ t('app.signedInAs') }}</span><strong>{{ auth.name || t('app.defaultName') }}</strong><small>{{ auth.role || t('app.defaultRole') }}</small></div>
              <button class="profile-action" type="button" @click="openChangePassword"><svg class="profile-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="10" width="14" height="10" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></svg> {{ t('app.changePassword') }}</button>
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

    <ChangePasswordModal v-if="changePasswordOpen" @close="changePasswordOpen = false" />
  </div>
</template>
