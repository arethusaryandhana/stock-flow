<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'

const route = useRoute()
const auth = useAuthStore()
const mobileOpen = ref(false)
const search = ref('')
const toast = ref('')
const profileOpen = ref(false)
const profileWrap = ref<HTMLElement | null>(null)

const groups = [
  {
    label: 'Workspace',
    items: [{ label: 'Ringkasan', path: '/', icon: '⌂', badge: '' }],
  },
  {
    label: 'Inventory',
    items: [
      { label: 'Produk & stok', path: '/products', icon: '▦', badge: '' },
      { label: 'Pergerakan stok', path: '/inventory/movements', icon: '↕', badge: '' },
      { label: 'Penyesuaian', path: '/inventory/adjustments', icon: '△', badge: '' },
    ],
  },
  {
    label: 'Operasional',
    items: [
      { label: 'Purchase order', path: null, icon: '▤', badge: 'Soon' },
      { label: 'Penerimaan barang', path: null, icon: '↓', badge: 'Soon' },
      { label: 'Supplier', path: null, icon: '◎', badge: 'Soon' },
    ],
  },
  {
    label: 'Insight',
    items: [
      { label: 'Laporan', path: null, icon: '◷', badge: 'Soon' },
      { label: 'Pengaturan', path: null, icon: '⚙', badge: 'Soon' },
    ],
  },
]

const initials = computed(() =>
  (auth.name || 'Demo Administrator')
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

function handleSearch() {
  if (search.value.trim()) notify(`Pencarian untuk “${search.value.trim()}” akan tersedia di rilis berikutnya.`)
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
    <aside class="sidebar" :class="{ 'sidebar-open': mobileOpen }">
      <router-link class="brand-lockup" to="/" aria-label="Buka Ringkasan StockFlow" @click="closeMobileNav">
        <img class="brand-logo" src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true">
        <div>
          <strong>StockFlow</strong>
          <span>Inventory OS</span>
        </div>
      </router-link>

      <button class="workspace-switcher" type="button" @click="notify('Workspace switcher siap digunakan saat multi-cabang diaktifkan.')">
        <span class="workspace-avatar"><img src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true"></span>
        <span class="workspace-copy"><small>WORKSPACE</small><strong>StockFlow Demo</strong></span>
        <span class="workspace-chevron">⌄</span>
      </button>

      <nav class="sidebar-nav" aria-label="Navigasi utama">
        <section v-for="group in groups" :key="group.label" class="nav-group">
          <p class="nav-group-label">{{ group.label }}</p>
          <template v-for="item in group.items" :key="item.label">
            <router-link
              v-if="item.path"
              :to="item.path"
              class="nav-link"
              :class="{ active: route.path === item.path }"
              @click="closeMobileNav"
            >
              <span class="nav-icon">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
            </router-link>
            <button v-else class="nav-link nav-placeholder" type="button" @click="notify(`${item.label} akan hadir di rilis berikutnya.`)">
              <span class="nav-icon">{{ item.icon }}</span>
              <span>{{ item.label }}</span>
              <em>{{ item.badge }}</em>
            </button>
          </template>
        </section>
      </nav>

      <div class="sidebar-bottom">
        <div class="sync-pill"><span class="status-dot" /> Data tersinkronisasi</div>
        <button class="sidebar-help" type="button" @click="notify('Tim support akan segera tersedia di workspace ini.')">
          <span class="help-icon">?</span>
          <span><strong>Butuh bantuan?</strong><small>Pelajari StockFlow</small></span>
          <span class="arrow">↗</span>
        </button>
      </div>
    </aside>

    <div class="app-main">
      <header class="topbar">
        <div class="topbar-left">
          <button class="mobile-menu" type="button" aria-label="Buka navigasi" @click="mobileOpen = true">☰</button>
          <router-link class="mobile-brand" to="/" aria-label="Buka Ringkasan StockFlow">
            <img src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true">
            <strong>StockFlow</strong>
          </router-link>
          <div class="breadcrumbs"><span>StockFlow Demo</span><b>/</b><strong>{{ route.path === '/' ? 'Ringkasan' : 'Inventory' }}</strong></div>
        </div>
        <div class="topbar-actions">
          <form class="global-search" @submit.prevent="handleSearch">
            <span class="search-glyph">⌕</span>
            <input v-model="search" aria-label="Cari di StockFlow" placeholder="Cari produk, SKU, atau aktivitas...">
            <kbd>⌘ K</kbd>
          </form>
          <button class="topbar-icon" type="button" aria-label="Bantuan" @click="notify('Pusat bantuan akan segera tersedia.')">?</button>
          <button class="topbar-icon notification-button" type="button" aria-label="Notifikasi" @click="notify('Tidak ada notifikasi baru.')"><span />◔</button>
          <div ref="profileWrap" class="profile-wrap">
            <button class="profile-menu profile-trigger" type="button" :aria-expanded="profileOpen" aria-label="Buka menu profil" @click="profileOpen = !profileOpen">
              <div class="profile-avatar">{{ initials }}</div>
              <div class="profile-copy"><strong>{{ auth.name || 'Demo Administrator' }}</strong><small>{{ auth.role || 'Administrator' }}</small></div>
              <span class="profile-chevron">⌄</span>
            </button>
            <div v-if="profileOpen" class="profile-dropdown">
              <div class="profile-dropdown-meta"><span class="profile-dropdown-label">SIGNED IN AS</span><strong>{{ auth.name || 'Demo Administrator' }}</strong><small>{{ auth.role || 'Administrator' }}</small></div>
              <button class="profile-logout" type="button" @click="logout"><span>↪</span> Logout</button>
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
