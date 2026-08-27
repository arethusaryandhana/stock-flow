<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'

type Product = { id: string; sku: string; name: string; categoryId: string; category: string; purchasePrice: number; sellingPrice: number; stockOnHand: number; reorderLevel: number; unit: string; isActive: boolean }
type Category = { id: string; name: string; isActive: boolean }

const items = ref<Product[]>([])
const categories = ref<Category[]>([])
const q = ref('')
const statusFilter = ref('all')
const categoryFilter = ref('all')
const loading = ref(true)
const error = ref('')
const formError = ref('')
const saving = ref(false)
const showForm = ref(false)
const openMenu = ref('')
const newProduct = ref({ sku: '', name: '', categoryId: '', purchasePrice: 0, sellingPrice: 0, reorderLevel: 0, unit: 'pcs' })
const { locale, t } = useI18n()

const money = (value: number) => new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()
const status = (product: Product) => {
  if (!product.isActive) return 'inactive'
  if (product.stockOnHand <= 0) return 'out'
  if (product.stockOnHand <= product.reorderLevel) return 'low'
  return 'ok'
}
const statusLabel = (product: Product) => status(product) === 'ok' ? t('products.safe') : status(product) === 'low' ? t('products.low') : status(product) === 'out' ? t('products.out') : t('products.inactive')
const statusClass = (product: Product) => status(product) === 'ok' ? 'ok' : status(product) === 'low' ? 'warn' : status(product) === 'out' ? 'danger' : 'neutral'
const activeItems = computed(() => items.value.filter((item) => item.isActive))
const counts = computed(() => ({ all: items.value.length, healthy: activeItems.value.filter((item) => status(item) === 'ok').length, low: activeItems.value.filter((item) => status(item) === 'low').length, out: activeItems.value.filter((item) => status(item) === 'out').length, inactive: items.value.filter((item) => !item.isActive).length }))
const filtered = computed(() => {
  const term = q.value.toLowerCase().trim()
  return items.value.filter((item) => {
    const matchesTerm = `${item.sku} ${item.name} ${item.category}`.toLowerCase().includes(term)
    const matchesCategory = categoryFilter.value === 'all' || item.categoryId === categoryFilter.value
    const matchesStatus = statusFilter.value === 'all' || (statusFilter.value === 'inactive' ? !item.isActive : status(item) === statusFilter.value)
    return matchesTerm && matchesCategory && matchesStatus
  })
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [productsResponse, categoriesResponse] = await Promise.all([api.get<Product[]>('/products'), api.get<Category[]>('/categories')])
    items.value = productsResponse.data
    categories.value = categoriesResponse.data.filter((category) => category.isActive)
    if (!newProduct.value.categoryId && categories.value[0]) newProduct.value.categoryId = categories.value[0].id
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally { loading.value = false }
}

function openForm() { formError.value = ''; showForm.value = true }
function closeForm() { showForm.value = false; formError.value = ''; newProduct.value = { sku: '', name: '', categoryId: categories.value[0]?.id ?? '', purchasePrice: 0, sellingPrice: 0, reorderLevel: 0, unit: 'pcs' } }
async function createProduct() {
  formError.value = ''; saving.value = true
  try {
    const { data } = await api.post<Product>('/products', newProduct.value)
    items.value = [...items.value, data].sort((left, right) => left.name.localeCompare(right.name))
    closeForm()
  } catch (requestError) { formError.value = (requestError as Error).message } finally { saving.value = false }
}
async function toggleActive(product: Product) {
  openMenu.value = ''
  try { await api.patch(`/products/${product.id}/active`, !product.isActive); product.isActive = !product.isActive } catch (requestError) { error.value = (requestError as Error).message }
}
function exportCsv() {
  const rows = [[t('products.sku'), t('products.product'), t('products.category'), t('products.availableStock'), t('products.unit'), t('products.sellingPrice'), t('products.status')], ...filtered.value.map((item) => [item.sku, item.name, item.category, String(item.stockOnHand), item.unit, String(item.sellingPrice), statusLabel(item)])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-produk.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">{{ t('products.eyebrow') }}</p><h1>{{ t('products.title') }}</h1><p class="subtitle">{{ t('products.subtitle') }}</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button><button class="primary" type="button" @click="openForm"><span class="button-plus">+</span> {{ t('products.add') }}</button></div>
    </div>

    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▦</span><span><small>{{ t('products.total') }}</small><strong>{{ loading ? '—' : counts.all }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">✓</span><span><small>{{ t('products.safe') }}</small><strong>{{ loading ? '—' : counts.healthy }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">△</span><span><small>{{ t('products.attention') }}</small><strong>{{ loading ? '—' : counts.low + counts.out }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="tab-row">
        <button class="tab-button" :class="{ active: statusFilter === 'all' }" type="button" @click="statusFilter = 'all'">{{ t('products.all') }} <span class="count">{{ counts.all }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'low' }" type="button" @click="statusFilter = 'low'">{{ t('products.low') }} <span class="count">{{ counts.low }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'out' }" type="button" @click="statusFilter = 'out'">{{ t('products.out') }} <span class="count">{{ counts.out }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'inactive' }" type="button" @click="statusFilter = 'inactive'">{{ t('products.inactive') }} <span class="count">{{ counts.inactive }}</span></button>
      </div>
      <div class="toolbar">
        <label class="search-input"><span>⌕</span><input v-model="q" :aria-label="t('products.searchAria')" :placeholder="t('products.searchPlaceholder')"></label>
        <div class="toolbar-actions"><select v-model="categoryFilter" class="filter-select wide" :aria-label="t('products.categoryFilterAria')"><option value="all">{{ t('products.allCategories') }}</option><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select><button class="secondary" type="button" @click="exportCsv">{{ t('products.filterExport') }}</button></div>
      </div>
      <div v-if="loading" class="empty">{{ t('products.loading') }}</div>
      <div v-else-if="!filtered.length" class="empty"><strong>{{ t('products.noMatchTitle') }}</strong>{{ t('products.noMatchHint') }}</div>
      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('products.product') }}</th><th>{{ t('products.category') }}</th><th>{{ t('products.availableStock') }}</th><th>{{ t('products.sellingPrice') }}</th><th>{{ t('products.status') }}</th><th><span class="sr-only">{{ t('products.actionAria') }}</span></th></tr></thead><tbody><tr v-for="product in filtered" :key="product.id"><td><div class="product-cell"><span class="product-avatar">{{ shortName(product.name) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.sku }}</small></span></div></td><td>{{ product.category }}</td><td><span class="stock-value" :class="{ low: status(product) === 'low', out: status(product) === 'out' }">{{ product.stockOnHand }} {{ product.unit }}</span><small>{{ t('products.min') }} {{ product.reorderLevel }} {{ product.unit }}</small></td><td class="stock-value">{{ money(product.sellingPrice) }}</td><td><span class="badge" :class="statusClass(product)">{{ statusLabel(product) }}</span></td><td><div class="action-menu-wrap"><button class="action-button" type="button" :aria-label="t('products.menuAria')" @click="openMenu = openMenu === product.id ? '' : product.id">•••</button><div v-if="openMenu === product.id" class="action-menu"><button type="button" @click="toggleActive(product)">{{ product.isActive ? t('products.deactivate') : t('products.activate') }}</button></div></div></td></tr></tbody></table></div>
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal" @submit.prevent="createProduct"><div class="modal-head"><div><p class="eyebrow">{{ t('products.eyebrow') }}</p><h2>{{ t('products.modalTitle') }}</h2><p>{{ t('products.modalDescription') }}</p></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeForm">×</button></div><div class="modal-body"><div class="form-grid"><label class="field-label">{{ t('products.sku') }}<input v-model.trim="newProduct.sku" required maxlength="80" :placeholder="t('products.skuPlaceholder')"></label><label class="field-label">{{ t('products.name') }}<input v-model.trim="newProduct.name" required maxlength="160" :placeholder="t('products.namePlaceholder')"></label><label class="field-label">{{ t('products.category') }}<select v-model="newProduct.categoryId" required><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select></label><label class="field-label">{{ t('products.unit') }}<input v-model.trim="newProduct.unit" required maxlength="24" :placeholder="t('products.unitPlaceholder')"></label><label class="field-label">{{ t('products.purchasePrice') }}<input v-model.number="newProduct.purchasePrice" type="number" min="0" step="1" required></label><label class="field-label">{{ t('products.sellingPrice') }}<input v-model.number="newProduct.sellingPrice" type="number" min="0" step="1" required></label><label class="field-label full">{{ t('products.reorderLevel') }}<input v-model.number="newProduct.reorderLevel" type="number" min="0" step="0.01" required></label></div><p v-if="formError" class="alert" style="margin-top: 14px">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">{{ t('common.cancel') }}</button><button class="primary" :disabled="saving">{{ saving ? t('common.saving') : t('common.save') }}</button></div></div></form></div></Teleport>
  </div>
</template>
