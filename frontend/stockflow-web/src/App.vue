<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { useI18n } from './i18n'

const route = useRoute()
const auth = useAuthStore()
const { language, t, toggleLanguage } = useI18n()
const mobileOpen = ref(false)
const sidebarCollapsed = ref(localStorage.getItem('stockflow_sidebar_collapsed') === 'true')
const search = ref('')
const toast = ref('')
const profileOpen = ref(false)
const profileWrap = ref<HTMLElement | null>(null)

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

const initials = computed(() =>
  (auth.name || t('app.defaultName'))
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase(),
)

function notify(message: string) {
  toast.value = message
  window.setTimeout(() => {
    toast.value = ''
  }, 2600)
}

function closeMobileNav() {
  mobileOpen.value = false
}

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem('stockflow_sidebar_collapsed', String(sidebarCollapsed.value))
}

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

onMounted(() => {
  document.addEventListener('click', closeProfileOnOutsideClick)
  document.addEventListener('keydown', closeProfileOnEscape)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', closeProfileOnOutsideClick)
  document.removeEventListener('keydown', closeProfileOnEscape)
})
</script>

<template>
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
          <section v-if="!group.adminOnly || auth.isAdmin" class="nav-group">
          <p class="nav-group-label">{{ t(group.labelKey) }}</p>
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

    <Transition name="toast">
      <div v-if="toast" class="toast-message"><span>✓</span>{{ toast }}</div>
    </Transition>
  </div>
</template>
