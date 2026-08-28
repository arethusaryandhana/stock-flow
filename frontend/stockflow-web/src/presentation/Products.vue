<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'
import FormattedNumberInput from '../components/FormattedNumberInput.vue'

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
const editingProduct = ref<Product | null>(null)
const editReorderLevel = ref('0')
const editSaving = ref(false)
const newProduct = ref({ sku: '', name: '', categoryId: '', purchasePrice: '0', sellingPrice: '0', reorderLevel: '0', unit: 'pcs' })
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const counts = ref({ all: 0, low: 0, out: 0, inactive: 0 })
const { locale, t } = useI18n()
const auth = useAuthStore()
const toast = useToastStore()
const canManage = computed(() => auth.isAdmin)
watch(openMenu, (value) => { if (value && !canManage.value) openMenu.value = '' })

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
const filtered = computed(() => items.value)
const attentionCount = computed(() => counts.value.low + counts.value.out)
const statusQuery = computed(() => statusFilter.value === 'all' ? undefined : statusFilter.value)
const categoryQuery = computed(() => categoryFilter.value === 'all' ? undefined : categoryFilter.value)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const statuses = [undefined, 'low', 'out', 'inactive'] as const
    const productResponse = api.get<PagedResponse<Product>>('/products', { params: { page: page.value, pageSize: pageSize.value, search: q.value.trim() || undefined, status: statusQuery.value, categoryId: categoryQuery.value } })
    const countResponses = statuses.map((statusValue) => statusValue === statusQuery.value
      ? productResponse
      : api.get<PagedResponse<Product>>('/products', { params: { page: 1, pageSize: 1, search: q.value.trim() || undefined, status: statusValue, categoryId: categoryQuery.value } }))
    const [productsResponse, allCountResponse, lowCountResponse, outCountResponse, inactiveCountResponse, categoriesResponse] = await Promise.all([
      productResponse,
      countResponses[0],
      countResponses[1],
      countResponses[2],
      countResponses[3],
      api.get<PagedResponse<Category>>('/categories', { params: { page: 1, pageSize: 100 } }),
    ])
    items.value = productsResponse.data.items
    page.value = productsResponse.data.page
    totalCount.value = productsResponse.data.totalCount
    totalPages.value = productsResponse.data.totalPages
    counts.value = {
      all: allCountResponse.data.totalCount,
      low: lowCountResponse.data.totalCount,
      out: outCountResponse.data.totalCount,
      inactive: inactiveCountResponse.data.totalCount,
    }
    categories.value = categoriesResponse.data.items.filter((category) => category.isActive)
    if (!newProduct.value.categoryId && categories.value[0]) newProduct.value.categoryId = categories.value[0].id
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally { loading.value = false }
}

function openForm() { formError.value = ''; showForm.value = true }
function closeForm() { showForm.value = false; formError.value = ''; newProduct.value = { sku: '', name: '', categoryId: categories.value[0]?.id ?? '', purchasePrice: '0', sellingPrice: '0', reorderLevel: '0', unit: 'pcs' } }
function openEditForm(product: Product) {
  if (!canManage.value) return
  openMenu.value = ''
  formError.value = ''
  editingProduct.value = product
  editReorderLevel.value = String(product.reorderLevel)
}
function closeEditForm() {
  editingProduct.value = null
  editReorderLevel.value = '0'
  formError.value = ''
}
async function createProduct() {
  formError.value = ''; saving.value = true
  try {
    const productName = newProduct.value.name
    await api.post<Product>('/products', {
      ...newProduct.value,
      purchasePrice: Number(newProduct.value.purchasePrice),
      sellingPrice: Number(newProduct.value.sellingPrice),
      reorderLevel: Number(newProduct.value.reorderLevel),
    })
    closeForm()
    await load()
    toast.success(t('products.createdToast', { name: productName }))
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally { saving.value = false }
}
async function updateReorderLevel() {
  formError.value = ''
  const rawReorderLevel = String(editReorderLevel.value).trim()
  const reorderLevel = Number(rawReorderLevel)
  const roundedReorderLevel = Number(reorderLevel.toFixed(2))
  if (!rawReorderLevel || !Number.isFinite(reorderLevel) || reorderLevel < 0 || roundedReorderLevel !== reorderLevel) {
    formError.value = t('products.invalidReorderLevel')
    return
  }

  if (!editingProduct.value) return

  editSaving.value = true
  try {
    const { data } = await api.patch<Product>(`/products/${editingProduct.value.id}/reorder-level`, { reorderLevel })
    items.value = items.value.map((item) => item.id === data.id ? data : item)
    await load()
    toast.success(t('products.reorderUpdatedToast', { name: editingProduct.value.name }))
    closeEditForm()
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally { editSaving.value = false }
}
async function toggleActive(product: Product) {
  if (!canManage.value) return
  openMenu.value = ''
  const willActivate = !product.isActive
  try {
    await api.patch(`/products/${product.id}/active`, willActivate)
    await load()
    toast.success(t(willActivate ? 'products.activatedToast' : 'products.deactivatedToast', { name: product.name }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  }
}
function exportCsv() {
  const rows = [[t('products.sku'), t('products.product'), t('products.category'), t('products.availableStock'), t('products.unit'), t('products.sellingPrice'), t('products.status')], ...filtered.value.map((item) => [item.sku, item.name, item.category, String(item.stockOnHand), item.unit, String(item.sellingPrice), statusLabel(item)])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-produk.csv'; link.click(); URL.revokeObjectURL(link.href)
}
function changePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}
watch([q, statusFilter, categoryFilter], () => {
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
    <div class="page-heading">
      <div><p class="eyebrow">{{ t('products.eyebrow') }}</p><h1>{{ t('products.title') }}</h1><p class="subtitle">{{ t('products.subtitle') }}</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button><button v-if="canManage" class="primary" type="button" @click="openForm"><span class="button-plus">+</span> {{ t('products.add') }}</button></div>
    </div>

    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▦</span><span><small>{{ t('products.total') }}</small><strong>{{ loading ? '—' : counts.all }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">✓</span><span><small>{{ t('products.safe') }}</small><strong>{{ loading ? '—' : Math.max(counts.all - attentionCount - counts.inactive, 0) }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">△</span><span><small>{{ t('products.attention') }}</small><strong>{{ loading ? '—' : attentionCount }}</strong></span></div></div>

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
      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('products.product') }}</th><th>{{ t('products.category') }}</th><th>{{ t('products.availableStock') }}</th><th>{{ t('products.sellingPrice') }}</th><th>{{ t('products.status') }}</th><th><span class="sr-only">{{ t('products.actionAria') }}</span></th></tr></thead><tbody><tr v-for="product in filtered" :key="product.id"><td><div class="product-cell"><span class="product-avatar">{{ shortName(product.name) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.sku }}</small></span></div></td><td>{{ product.category }}</td><td><span class="stock-value" :class="{ low: status(product) === 'low', out: status(product) === 'out' }">{{ product.stockOnHand }} {{ product.unit }}</span><small>{{ t('products.min') }} {{ product.reorderLevel }} {{ product.unit }}</small></td><td class="stock-value">{{ money(product.sellingPrice) }}</td><td><span class="badge" :class="statusClass(product)">{{ statusLabel(product) }}</span></td><td><div class="action-menu-wrap"><button class="action-button" type="button" :aria-label="t('products.menuAria')" @click="openMenu = openMenu === product.id ? '' : product.id">•••</button><div v-if="openMenu === product.id" class="action-menu"><button type="button" @click="openEditForm(product)">{{ t('products.editReorderLevel') }}</button><button type="button" @click="toggleActive(product)">{{ product.isActive ? t('products.deactivate') : t('products.activate') }}</button></div></div></td></tr></tbody></table></div>
      <PaginationControls v-if="!loading && filtered.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal" @submit.prevent="createProduct"><div class="modal-head"><div><p class="eyebrow">{{ t('products.eyebrow') }}</p><h2>{{ t('products.modalTitle') }}</h2><p>{{ t('products.modalDescription') }}</p></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeForm">×</button></div><div class="modal-body"><div class="form-grid"><label class="field-label">{{ t('products.sku') }}<input v-model.trim="newProduct.sku" required maxlength="80" :placeholder="t('products.skuPlaceholder')"></label><label class="field-label">{{ t('products.name') }}<input v-model.trim="newProduct.name" required maxlength="160" :placeholder="t('products.namePlaceholder')"></label><label class="field-label">{{ t('products.category') }}<select v-model="newProduct.categoryId" required><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select></label><label class="field-label">{{ t('products.unit') }}<input v-model.trim="newProduct.unit" required maxlength="24" :placeholder="t('products.unitPlaceholder')"></label><label class="field-label">{{ t('products.purchasePrice') }}<FormattedNumberInput v-model="newProduct.purchasePrice" required></FormattedNumberInput></label><label class="field-label">{{ t('products.sellingPrice') }}<FormattedNumberInput v-model="newProduct.sellingPrice" required></FormattedNumberInput></label><label class="field-label full">{{ t('products.reorderLevel') }}<FormattedNumberInput v-model="newProduct.reorderLevel" :decimal-scale="2" required></FormattedNumberInput><small class="field-hint">{{ t('products.reorderLevelHint') }}</small></label></div><p v-if="formError" class="alert" style="margin-top: 14px">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">{{ t('common.cancel') }}</button><button class="primary" :disabled="saving">{{ saving ? t('common.saving') : t('common.save') }}</button></div></div></form></div><div v-if="editingProduct" class="modal-backdrop" @click.self="closeEditForm"><form class="modal edit-modal" @submit.prevent="updateReorderLevel"><div class="modal-head"><div><p class="eyebrow">{{ t('products.eyebrow') }}</p><h2>{{ t('products.editReorderTitle') }}</h2><p>{{ editingProduct.name }} · {{ editingProduct.sku }}</p></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeEditForm">×</button></div><div class="modal-body"><label class="field-label">{{ t('products.reorderLevel') }}<FormattedNumberInput v-model="editReorderLevel" :decimal-scale="2" required autofocus></FormattedNumberInput></label><p class="field-hint">{{ t('products.reorderLevelHint') }}</p><p v-if="formError" class="alert" style="margin-top: 14px">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeEditForm">{{ t('common.cancel') }}</button><button class="primary" :disabled="editSaving">{{ editSaving ? t('common.saving') : t('products.saveReorderLevel') }}</button></div></div></form></div></Teleport>
  </div>
</template>
