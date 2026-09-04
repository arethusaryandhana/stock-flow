<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'

type Supplier = { id: string; code: string; name: string; email: string | null; phone: string | null; address: string | null; isActive: boolean; createdAt: string; updatedAt: string | null }

const suppliers = ref<Supplier[]>([])
const search = ref('')
const loading = ref(true)
const error = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const auth = useAuthStore()
const { t } = useI18n()

const activeCount = computed(() => suppliers.value.filter((supplier) => supplier.isActive).length)
const filtered = computed(() => suppliers.value)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const response = await api.get<PagedResponse<Supplier>>('/suppliers', { params: { page: page.value, pageSize: pageSize.value, search: search.value.trim() || undefined } })
    suppliers.value = response.data.items
    page.value = response.data.page
    totalCount.value = response.data.totalCount
    totalPages.value = response.data.totalPages
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function exportCsv() {
  const rows = [[t('suppliersView.code'), t('suppliersView.name'), t('suppliersView.contact'), t('suppliersView.address'), t('suppliersView.status')], ...filtered.value.map((supplier) => [supplier.code, supplier.name, [supplier.email, supplier.phone].filter(Boolean).join(' · '), supplier.address ?? '', supplier.isActive ? t('suppliersView.activeLabel') : t('suppliersView.inactiveLabel')])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a')
  link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  link.download = 'stockflow-operational-suppliers.csv'
  link.click()
  URL.revokeObjectURL(link.href)
}

function changePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}

watch(search, () => {
  page.value = 1
  void load()
})
watch(page, (nextPage, previousPage) => {
  if (nextPage !== previousPage) void load()
})
watch(pageSize, () => void load())
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading"><div><p class="eyebrow">{{ t('suppliersView.eyebrow') }}</p><h1>{{ t('suppliersView.title') }}</h1><p class="subtitle">{{ t('suppliersView.subtitle') }}</p></div><div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button><router-link v-if="auth.isAdmin" class="primary button-link" to="/master-data/suppliers"><span class="button-plus">+</span> {{ t('suppliersView.add') }}</router-link></div></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid supplier-summary-grid"><div class="mini-stat"><span class="mini-stat-icon">◎</span><span><small>{{ t('suppliersView.total') }}</small><strong>{{ loading ? '—' : totalCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">✓</span><span><small>{{ t('suppliersView.active') }}</small><strong>{{ loading ? '—' : activeCount }}</strong></span></div></div>
    <section class="surface-card page-panel"><div class="toolbar"><label class="search-input"><span>⌕</span><input v-model="search" :aria-label="t('suppliersView.searchAria')" :placeholder="t('suppliersView.searchPlaceholder')"></label><div class="toolbar-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button></div></div><div v-if="loading" class="empty">{{ t('suppliersView.loading') }}</div><div v-else-if="!filtered.length" class="empty"><strong>{{ t('suppliersView.emptyTitle') }}</strong>{{ t('suppliersView.emptyHint') }}</div><div v-else class="table-wrap"><table><thead><tr><th>{{ t('suppliersView.code') }}</th><th>{{ t('suppliersView.name') }}</th><th>{{ t('suppliersView.contact') }}</th><th>{{ t('suppliersView.address') }}</th><th>{{ t('suppliersView.status') }}</th></tr></thead><tbody><tr v-for="supplier in filtered" :key="supplier.id"><td class="stock-value">{{ supplier.code }}</td><td><div class="product-cell"><span class="product-avatar teal">{{ supplier.name.slice(0, 2).toUpperCase() }}</span><span><strong>{{ supplier.name }}</strong><small>{{ supplier.email || '—' }}</small></span></div></td><td><span class="supplier-contact">{{ supplier.phone || '—' }}</span></td><td class="muted-cell">{{ supplier.address || '—' }}</td><td><span class="badge" :class="supplier.isActive ? 'ok' : 'neutral'">{{ supplier.isActive ? t('suppliersView.activeLabel') : t('suppliersView.inactiveLabel') }}</span></td></tr></tbody></table></div><PaginationControls v-if="!loading && filtered.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" /></section>
  </div>
</template>
