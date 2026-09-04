<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'
import FormattedNumberInput from '../components/FormattedNumberInput.vue'

type Supplier = { id: string; code: string; name: string; isActive: boolean }
type Product = { id: string; sku: string; name: string; unit: string; purchasePrice: number; isActive: boolean }
type PurchaseOrderItem = { id: string; productId: string; productSku: string; productName: string; unit: string; quantity: number; receivedQuantity: number; unitPrice: number }
type PurchaseOrder = { id: string; number: string; supplierId: string; supplierCode: string; supplierName: string; status: string; orderDate: string; expectedDate: string | null; notes: string | null; totalAmount: number; items: PurchaseOrderItem[] }
type OrderLine = { productId: string; quantity: string; unitPrice: string }

const orders = ref<PurchaseOrder[]>([])
const suppliers = ref<Supplier[]>([])
const products = ref<Product[]>([])
const q = ref('')
const statusFilter = ref('all')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const showForm = ref(false)
const openMenu = ref('')
const expandedOrderId = ref('')
const newOrder = ref({ supplierId: '', expectedDate: '', notes: '' })
const lines = ref<OrderLine[]>([{ productId: '', quantity: '', unitPrice: '' }])
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)
const statusCounts = ref({ submitted: 0, approved: 0 })
const { locale, t } = useI18n()
const auth = useAuthStore()
const toast = useToastStore()

const canManage = computed(() => ['admin', 'manager'].includes(auth.role.trim().toLowerCase()))
const activeSuppliers = computed(() => suppliers.value.filter((supplier) => supplier.isActive))
const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const totalAmount = computed(() => lines.value.reduce((total, line) => total + parseAmount(line.quantity) * parseAmount(line.unitPrice), 0))
const date = (value: string) => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value))
const money = (value: number) => new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const formatQuantity = (value: number) => Number(value.toFixed(2)).toString()
const parseAmount = (value: string | number) => {
  const normalized = String(value ?? '').trim().replace(',', '.')
  const parsed = Number(normalized)
  return Number.isFinite(parsed) ? parsed : 0
}
const statusLabel = (status: string) => t(`operations.${status.toLowerCase()}`)
const statusClass = (status: string) => status === 'Approved' || status === 'Received' ? 'ok' : status === 'Submitted' ? 'warn' : status === 'Cancelled' ? 'danger' : 'neutral'
const filtered = computed(() => orders.value)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const orderRequest = api.get<PagedResponse<PurchaseOrder>>('/purchase-orders', { params: { page: page.value, pageSize: pageSize.value, search: q.value.trim() || undefined, status: statusFilter.value === 'all' ? undefined : statusFilter.value } })
    const [ordersResponse, submittedResponse, approvedResponse, suppliersResponse, productsResponse] = await Promise.all([
      orderRequest,
      api.get<PagedResponse<PurchaseOrder>>('/purchase-orders', { params: { page: 1, pageSize: 1, search: q.value.trim() || undefined, status: 'Submitted' } }),
      api.get<PagedResponse<PurchaseOrder>>('/purchase-orders', { params: { page: 1, pageSize: 1, search: q.value.trim() || undefined, status: 'Approved' } }),
      api.get<PagedResponse<Supplier>>('/suppliers', { params: { page: 1, pageSize: 100 } }),
      api.get<PagedResponse<Product>>('/products', { params: { page: 1, pageSize: 100 } }),
    ])
    orders.value = ordersResponse.data.items
    page.value = ordersResponse.data.page
    totalCount.value = ordersResponse.data.totalCount
    totalPages.value = ordersResponse.data.totalPages
    statusCounts.value = { submitted: submittedResponse.data.totalCount, approved: approvedResponse.data.totalCount }
    suppliers.value = suppliersResponse.data.items
    products.value = productsResponse.data.items
    if (!newOrder.value.supplierId && activeSuppliers.value[0]) newOrder.value.supplierId = activeSuppliers.value[0].id
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function openForm() {
  formError.value = ''
  if (!newOrder.value.supplierId && activeSuppliers.value[0]) newOrder.value.supplierId = activeSuppliers.value[0].id
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  formError.value = ''
  newOrder.value = { supplierId: activeSuppliers.value[0]?.id ?? '', expectedDate: '', notes: '' }
  lines.value = [{ productId: '', quantity: '', unitPrice: '' }]
}

function addLine() {
  lines.value.push({ productId: '', quantity: '', unitPrice: '' })
}

function removeLine(index: number) {
  if (lines.value.length === 1) {
    lines.value[0] = { productId: '', quantity: '', unitPrice: '' }
    return
  }
  lines.value.splice(index, 1)
}

function setProductPrice(line: OrderLine) {
  const product = activeProducts.value.find((item) => item.id === line.productId)
  if (product && !line.unitPrice) line.unitPrice = String(product.purchasePrice)
}

async function createOrder() {
  formError.value = ''
  if (!newOrder.value.supplierId || !lines.value.length || lines.value.some((line) => !line.productId)) {
    formError.value = t('operations.formRequired')
    return
  }
  if (lines.value.some((line) => {
    const quantity = parseAmount(line.quantity)
    return quantity <= 0 || Number(quantity.toFixed(2)) !== quantity
  })) {
    formError.value = t('operations.invalidQuantity')
    return
  }
  if (lines.value.some((line) => parseAmount(line.unitPrice) < 0)) {
    formError.value = t('operations.invalidPrice')
    return
  }
  if (new Set(lines.value.map((line) => line.productId)).size !== lines.value.length) {
    formError.value = t('operations.duplicateProduct')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<PurchaseOrder>('/purchase-orders', {
      supplierId: newOrder.value.supplierId,
      expectedDate: newOrder.value.expectedDate || null,
      notes: newOrder.value.notes.trim() || null,
      items: lines.value.map((line) => ({ productId: line.productId, quantity: parseAmount(line.quantity), unitPrice: parseAmount(line.unitPrice) })),
    })
    closeForm()
    await load()
    toast.success(t('operations.createdToast', { number: data.number }))
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally {
    saving.value = false
  }
}

async function updateStatus(order: PurchaseOrder, nextStatus: string) {
  openMenu.value = ''
  if (nextStatus === 'Cancelled' && !window.confirm(t('operations.cancelConfirm', { number: order.number }))) return
  try {
    await api.patch(`/purchase-orders/${order.id}/status`, { status: nextStatus })
    await load()
    toast.success(t('operations.statusUpdatedToast', { number: order.number }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  }
}

function exportCsv() {
  const rows = [[t('operations.date'), t('operations.number'), t('operations.supplier'), t('operations.items'), t('operations.totalAmount'), t('operations.status')], ...filtered.value.map((order) => [date(order.orderDate), order.number, order.supplierName, String(order.items.length), money(order.totalAmount), statusLabel(order.status)])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a')
  link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  link.download = 'stockflow-purchase-orders.csv'
  link.click()
  URL.revokeObjectURL(link.href)
}

function changePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}

watch([q, statusFilter], () => {
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
      <div><p class="eyebrow">{{ t('operations.eyebrow') }}</p><h1>{{ t('operations.title') }}</h1><p class="subtitle">{{ t('operations.subtitle') }}</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('operations.export') }}</button><button v-if="canManage" class="primary" type="button" @click="openForm"><span class="button-plus">+</span> {{ t('operations.add') }}</button></div>
    </div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▤</span><span><small>{{ t('operations.total') }}</small><strong>{{ loading ? '—' : totalCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon amber">◷</span><span><small>{{ t('operations.awaitingApproval') }}</small><strong>{{ loading ? '—' : statusCounts.submitted }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">↓</span><span><small>{{ t('operations.readyToReceive') }}</small><strong>{{ loading ? '—' : statusCounts.approved }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="tab-row"><button class="tab-button" :class="{ active: statusFilter === 'all' }" type="button" @click="statusFilter = 'all'">{{ t('operations.allStatuses') }}</button><button class="tab-button" :class="{ active: statusFilter === 'Draft' }" type="button" @click="statusFilter = 'Draft'">{{ t('operations.draft') }}</button><button class="tab-button" :class="{ active: statusFilter === 'Submitted' }" type="button" @click="statusFilter = 'Submitted'">{{ t('operations.submitted') }} <span class="count">{{ statusCounts.submitted }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Approved' }" type="button" @click="statusFilter = 'Approved'">{{ t('operations.approved') }} <span class="count">{{ statusCounts.approved }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Received' }" type="button" @click="statusFilter = 'Received'">{{ t('operations.received') }}</button></div>
      <div class="toolbar"><label class="search-input"><span>⌕</span><input v-model="q" :aria-label="t('operations.searchAria')" :placeholder="t('operations.searchPlaceholder')"></label><div class="toolbar-actions"><select v-model="statusFilter" class="filter-select wide" :aria-label="t('operations.statusFilterAria')"><option value="all">{{ t('operations.allStatuses') }}</option><option value="Draft">{{ t('operations.draft') }}</option><option value="Submitted">{{ t('operations.submitted') }}</option><option value="Approved">{{ t('operations.approved') }}</option><option value="Received">{{ t('operations.received') }}</option><option value="Cancelled">{{ t('operations.cancelled') }}</option></select></div></div>
      <div v-if="loading" class="empty">{{ t('operations.loading') }}</div>
      <div v-else-if="!filtered.length" class="empty"><strong>{{ t('operations.emptyTitle') }}</strong>{{ t('operations.emptyHint') }}</div>
      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('operations.date') }}</th><th>{{ t('operations.number') }}</th><th>{{ t('operations.supplier') }}</th><th>{{ t('operations.items') }}</th><th>{{ t('operations.totalAmount') }}</th><th>{{ t('operations.status') }}</th><th><span class="sr-only">{{ t('operations.actions') }}</span></th></tr></thead><tbody><template v-for="order in filtered" :key="order.id"><tr><td class="date-cell">{{ date(order.orderDate) }}<small v-if="order.expectedDate">{{ t('operations.expectedDate') }}: {{ date(order.expectedDate) }}</small></td><td class="stock-value">{{ order.number }}</td><td><strong>{{ order.supplierName }}</strong><small>{{ order.supplierCode }}</small></td><td>{{ order.items.length }}</td><td class="stock-value">{{ money(order.totalAmount) }}</td><td><span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span></td><td><div class="action-menu-wrap"><button class="action-button" type="button" :aria-label="t('operations.openMenu')" @click="openMenu = openMenu === order.id ? '' : order.id">•••</button><div v-if="openMenu === order.id" class="action-menu"><button type="button" @click="expandedOrderId = expandedOrderId === order.id ? '' : order.id; openMenu = ''">{{ t('operations.viewDetails') }}</button><button v-if="canManage && order.status === 'Draft'" type="button" @click="updateStatus(order, 'Submitted')">{{ t('operations.submit') }}</button><button v-if="canManage && order.status === 'Submitted'" type="button" @click="updateStatus(order, 'Approved')">{{ t('operations.approve') }}</button><router-link v-if="order.status === 'Approved'" class="action-menu-link" :to="{ path: '/operations/receiving', query: { purchaseOrderId: order.id } }" @click="openMenu = ''">{{ t('operations.receive') }}</router-link><button v-if="canManage && ['Draft', 'Submitted', 'Approved'].includes(order.status)" type="button" @click="updateStatus(order, 'Cancelled')">{{ t('operations.cancel') }}</button></div></div></td></tr><tr v-if="expandedOrderId === order.id" class="detail-row"><td colspan="7"><div class="order-detail"><div class="detail-head"><div><strong>{{ t('operations.detailTitle') }}</strong><p>{{ t('operations.detailDescription') }}</p></div><span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span></div><div class="table-wrap"><table><thead><tr><th>{{ t('operations.product') }}</th><th>{{ t('operations.quantity') }}</th><th>{{ t('operations.receivedQuantity') }}</th><th>{{ t('operations.unitPrice') }}</th><th>{{ t('operations.subtotal') }}</th></tr></thead><tbody><tr v-for="item in order.items" :key="item.id"><td><div class="product-cell"><span class="product-avatar">{{ item.productName.slice(0, 2).toUpperCase() }}</span><span><strong>{{ item.productName }}</strong><small>{{ item.productSku }}</small></span></div></td><td>{{ formatQuantity(item.quantity) }} {{ item.unit }}</td><td>{{ formatQuantity(item.receivedQuantity) }} {{ item.unit }}</td><td class="stock-value">{{ money(item.unitPrice) }}</td><td class="stock-value">{{ money(item.quantity * item.unitPrice) }}</td></tr></tbody></table></div><p v-if="order.notes" class="detail-note"><strong>{{ t('operations.notes') }}:</strong> {{ order.notes }}</p></div></td></tr></template></tbody></table></div>
      <PaginationControls v-if="!loading && filtered.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal order-modal" @submit.prevent="createOrder"><div class="modal-head"><div><p class="eyebrow">{{ t('operations.eyebrow') }}</p><h2>{{ t('operations.newTitle') }}</h2><p>{{ t('operations.newDescription') }}</p></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeForm">×</button></div><div class="modal-body"><div class="form-grid"><label class="field-label">{{ t('operations.supplier') }}<select v-model="newOrder.supplierId" required><option disabled value="">{{ t('operations.selectSupplier') }}</option><option v-for="supplier in activeSuppliers" :key="supplier.id" :value="supplier.id">{{ supplier.name }} · {{ supplier.code }}</option></select></label><label class="field-label">{{ t('operations.expectedDate') }}<input v-model="newOrder.expectedDate" type="date"></label><label class="field-label full">{{ t('operations.notes') }}<textarea v-model="newOrder.notes" rows="2" maxlength="500" :placeholder="t('operations.notesPlaceholder')" /></label></div><p v-if="!activeSuppliers.length || !activeProducts.length" class="alert modal-form-alert">{{ t('operations.masterDataHint') }}</p><div class="order-lines-head"><strong>{{ t('operations.items') }}</strong><button class="ghost-button" type="button" @click="addLine">+ {{ t('operations.addItem') }}</button></div><div class="order-lines"><div v-for="(line, index) in lines" :key="index" class="order-line"><label class="field-label">{{ t('operations.product') }}<select v-model="line.productId" required @change="setProductPrice(line)"><option disabled value="">{{ t('operations.selectProduct') }}</option><option v-for="product in activeProducts" :key="product.id" :value="product.id">{{ product.name }} · {{ product.sku }}</option></select></label><label class="field-label">{{ t('operations.quantity') }}<FormattedNumberInput v-model="line.quantity" :decimal-scale="2" required :placeholder="t('operations.quantityPlaceholder')"></FormattedNumberInput></label><label class="field-label">{{ t('operations.unitPrice') }}<FormattedNumberInput v-model="line.unitPrice" required :placeholder="t('operations.pricePlaceholder')"></FormattedNumberInput></label><button v-if="lines.length > 1" class="remove-line" type="button" :aria-label="t('operations.removeItem')" @click="removeLine(index)">×</button></div></div><p class="order-total"><span>{{ t('operations.totalAmount') }}</span><strong>{{ money(totalAmount) }}</strong></p><p v-if="formError" class="alert modal-form-alert">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">{{ t('operations.createCancel') }}</button><button class="primary" :disabled="saving || !activeSuppliers.length || !activeProducts.length">{{ saving ? t('operations.saving') : t('operations.createSave') }}</button></div></div></form></div></Teleport>
  </div>
</template>
