<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'
import { useDisplayPreferences } from '../preferences'
import PaginationControls from '../components/PaginationControls.vue'
import FormattedNumberInput from '../components/FormattedNumberInput.vue'

type Customer = { id: string; code: string; name: string; isActive: boolean }
type Product = { id: string; sku: string; name: string; unit: string; sellingPrice: number; stockOnHand: number; isActive: boolean }
type SalesOrderItem = { id: string; productId: string; productSku: string; productName: string; unit: string; stockOnHand: number; quantity: number; unitPrice: number }
type SalesOrder = { id: string; number: string; customerId: string; customerCode: string; customerName: string; status: string; orderDate: string; completedAt: string | null; notes: string | null; totalAmount: number; items: SalesOrderItem[] }
type SalesOrderStatusCounts = { draft: number; confirmed: number; processing: number; completed: number; cancelled: number }
type SalesOrderPageResponse = PagedResponse<SalesOrder> & { statusCounts: SalesOrderStatusCounts }
type OrderLine = { productId: string; quantity: string; unitPrice: string }

const displayPreferences = useDisplayPreferences()
const orders = ref<SalesOrder[]>([])
const customers = ref<Customer[]>([])
const products = ref<Product[]>([])
const q = ref('')
const statusFilter = ref('all')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const showForm = ref(false)
const expandedOrderId = ref('')
const newOrder = ref({ customerId: '', notes: '' })
const lines = ref<OrderLine[]>([{ productId: '', quantity: '', unitPrice: '' }])
const page = ref(1)
const pageSize = ref<number>(displayPreferences.defaultPageSize.value)
const totalCount = ref(0)
const totalPages = ref(0)
const statusCounts = ref<SalesOrderStatusCounts>({ draft: 0, confirmed: 0, processing: 0, completed: 0, cancelled: 0 })
const { t } = useI18n()
const auth = useAuthStore()
const toast = useToastStore()

const canManage = computed(() => ['admin', 'manager'].includes(auth.role.trim().toLowerCase()))
const activeCustomers = computed(() => customers.value.filter((customer) => customer.isActive))
const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const totalAmount = computed(() => lines.value.reduce((total, line) => total + parseAmount(line.quantity) * parseAmount(line.unitPrice), 0))
const date = (value: string) => displayPreferences.formatDate(value)
const money = (value: number) => displayPreferences.formatNumber(value, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 })
const formatQuantity = (value: number) => displayPreferences.formatNumber(Number(value.toFixed(2)), { maximumFractionDigits: 2 })
const parseAmount = (value: string | number) => {
  const parsed = Number(String(value ?? '').trim().replace(',', '.'))
  return Number.isFinite(parsed) ? parsed : 0
}
const statusLabel = (status: string) => t(`sales.${status.toLowerCase()}`)
const statusClass = (status: string) => status === 'Completed' ? 'ok' : status === 'Processing' ? 'warn' : status === 'Cancelled' ? 'danger' : 'neutral'

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [ordersResponse, customersResponse, productsResponse] = await Promise.all([
      api.get<SalesOrderPageResponse>('/sales-orders', { params: { page: page.value, pageSize: pageSize.value, search: q.value.trim() || undefined, status: statusFilter.value === 'all' ? undefined : statusFilter.value } }),
      api.get<PagedResponse<Customer>>('/customers', { params: { page: 1, pageSize: 100 } }),
      api.get<PagedResponse<Product>>('/products', { params: { page: 1, pageSize: 100 } }),
    ])
    orders.value = ordersResponse.data.items
    page.value = ordersResponse.data.page
    totalCount.value = ordersResponse.data.totalCount
    totalPages.value = ordersResponse.data.totalPages
    statusCounts.value = ordersResponse.data.statusCounts
    customers.value = customersResponse.data.items
    products.value = productsResponse.data.items
    if (!newOrder.value.customerId && activeCustomers.value[0]) newOrder.value.customerId = activeCustomers.value[0].id
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function openForm() {
  formError.value = ''
  if (!newOrder.value.customerId && activeCustomers.value[0]) newOrder.value.customerId = activeCustomers.value[0].id
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  formError.value = ''
  newOrder.value = { customerId: activeCustomers.value[0]?.id ?? '', notes: '' }
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
  if (product && !line.unitPrice) line.unitPrice = String(product.sellingPrice)
}

async function createOrder() {
  formError.value = ''
  if (!newOrder.value.customerId || !lines.value.length || lines.value.some((line) => !line.productId)) {
    formError.value = t('sales.formRequired')
    return
  }
  if (lines.value.some((line) => {
    const quantity = parseAmount(line.quantity)
    return quantity <= 0 || Number(quantity.toFixed(2)) !== quantity
  })) {
    formError.value = t('sales.invalidQuantity')
    return
  }
  if (lines.value.some((line) => parseAmount(line.unitPrice) < 0)) {
    formError.value = t('sales.invalidPrice')
    return
  }
  if (new Set(lines.value.map((line) => line.productId)).size !== lines.value.length) {
    formError.value = t('sales.duplicateProduct')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<SalesOrder>('/sales-orders', {
      customerId: newOrder.value.customerId,
      notes: newOrder.value.notes.trim() || null,
      items: lines.value.map((line) => ({ productId: line.productId, quantity: parseAmount(line.quantity), unitPrice: parseAmount(line.unitPrice) })),
    })
    closeForm()
    await load()
    toast.success(t('sales.createdToast', { number: data.number }))
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally {
    saving.value = false
  }
}

async function updateStatus(order: SalesOrder, nextStatus: string) {
  if (nextStatus === 'Cancelled' && !window.confirm(t('sales.cancelConfirm', { number: order.number }))) return
  if (nextStatus === 'Completed' && !window.confirm(t('sales.completeConfirm', { number: order.number }))) return

  try {
    await api.patch(`/sales-orders/${order.id}/status`, { status: nextStatus })
    await load()
    toast.success(t('sales.statusUpdatedToast', { number: order.number }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  }
}

function exportCsv() {
  const rows = [
    [t('sales.date'), t('sales.number'), t('sales.customer'), t('sales.items'), t('sales.totalAmount'), t('sales.status')],
    ...orders.value.map((order) => [date(order.orderDate), order.number, order.customerName, String(order.items.length), money(order.totalAmount), statusLabel(order.status)]),
  ]
  const csv = rows.map((row) => row.map((value) => `"${String(value).replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a')
  link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  link.download = 'stockflow-sales-orders.csv'
  link.click()
  URL.revokeObjectURL(link.href)
}

function changePageSize(nextPageSize: number) {
  displayPreferences.setDefaultPageSize(nextPageSize)
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
      <div><p class="eyebrow">{{ t('sales.eyebrow') }}</p><h1>{{ t('sales.title') }}</h1><p class="subtitle">{{ t('sales.subtitle') }}</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('sales.export') }}</button><button v-if="canManage" class="primary" type="button" @click="openForm"><span class="button-plus">+</span> {{ t('sales.add') }}</button></div>
    </div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▤</span><span><small>{{ t('sales.total') }}</small><strong>{{ loading ? '—' : totalCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon amber">✓</span><span><small>{{ t('sales.awaitingProcess') }}</small><strong>{{ loading ? '—' : statusCounts.confirmed }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">↑</span><span><small>{{ t('sales.inProcess') }}</small><strong>{{ loading ? '—' : statusCounts.processing }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="tab-row"><button class="tab-button" :class="{ active: statusFilter === 'all' }" type="button" @click="statusFilter = 'all'">{{ t('sales.allStatuses') }}</button><button class="tab-button" :class="{ active: statusFilter === 'Draft' }" type="button" @click="statusFilter = 'Draft'">{{ t('sales.draft') }} <span class="count">{{ statusCounts.draft }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Confirmed' }" type="button" @click="statusFilter = 'Confirmed'">{{ t('sales.confirmed') }} <span class="count">{{ statusCounts.confirmed }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Processing' }" type="button" @click="statusFilter = 'Processing'">{{ t('sales.processing') }} <span class="count">{{ statusCounts.processing }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Completed' }" type="button" @click="statusFilter = 'Completed'">{{ t('sales.completed') }} <span class="count">{{ statusCounts.completed }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Cancelled' }" type="button" @click="statusFilter = 'Cancelled'">{{ t('sales.cancelled') }} <span class="count">{{ statusCounts.cancelled }}</span></button></div>
      <div class="toolbar"><label class="search-input"><span>⌕</span><input v-model="q" :aria-label="t('sales.searchAria')" :placeholder="t('sales.searchPlaceholder')"></label><div class="toolbar-actions"><select v-model="statusFilter" class="filter-select wide" :aria-label="t('sales.statusFilterAria')"><option value="all">{{ t('sales.allStatuses') }}</option><option value="Draft">{{ t('sales.draft') }}</option><option value="Confirmed">{{ t('sales.confirmed') }}</option><option value="Processing">{{ t('sales.processing') }}</option><option value="Completed">{{ t('sales.completed') }}</option><option value="Cancelled">{{ t('sales.cancelled') }}</option></select></div></div>
      <div v-if="loading" class="empty">{{ t('sales.loading') }}</div>
      <div v-else-if="!orders.length" class="empty"><strong>{{ t('sales.emptyTitle') }}</strong>{{ t('sales.emptyHint') }}</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th class="purchase-order-expand-cell"><span class="sr-only">{{ t('sales.viewDetails') }}</span></th><th>{{ t('sales.date') }}</th><th>{{ t('sales.number') }}</th><th>{{ t('sales.customer') }}</th><th>{{ t('sales.items') }}</th><th>{{ t('sales.totalAmount') }}</th><th>{{ t('sales.status') }}</th><th class="purchase-order-actions-head">{{ t('sales.actions') }}</th></tr></thead>
          <tbody>
            <template v-for="order in orders" :key="order.id">
              <tr>
                <td class="purchase-order-expand-cell"><button class="purchase-order-expand-button" :class="{ expanded: expandedOrderId === order.id }" type="button" :aria-label="t('sales.viewDetails')" :aria-expanded="expandedOrderId === order.id" :aria-controls="`sales-order-details-${order.id}`" @click="expandedOrderId = expandedOrderId === order.id ? '' : order.id"><span aria-hidden="true">›</span></button></td>
                <td class="date-cell">{{ date(order.orderDate) }}<small v-if="order.completedAt">{{ t('sales.completedAt') }}: {{ date(order.completedAt) }}</small></td>
                <td class="stock-value purchase-order-number">{{ order.number }}</td>
                <td><strong>{{ order.customerName }}</strong><small>{{ order.customerCode }}</small></td>
                <td>{{ order.items.length }}</td>
                <td class="stock-value">{{ money(order.totalAmount) }}</td>
                <td><span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span></td>
                <td class="purchase-order-actions-cell"><div class="purchase-order-actions"><button v-if="canManage && order.status === 'Draft'" class="purchase-order-action primary-action" type="button" @click="updateStatus(order, 'Confirmed')">{{ t('sales.confirm') }}</button><button v-if="canManage && order.status === 'Confirmed'" class="purchase-order-action primary-action" type="button" @click="updateStatus(order, 'Processing')">{{ t('sales.process') }}</button><button v-if="canManage && order.status === 'Processing'" class="purchase-order-action primary-action" type="button" @click="updateStatus(order, 'Completed')">{{ t('sales.complete') }}</button><button v-if="canManage && ['Draft', 'Confirmed', 'Processing'].includes(order.status)" class="purchase-order-action cancel-action" type="button" @click="updateStatus(order, 'Cancelled')">{{ t('sales.cancel') }}</button><span v-if="!canManage || ['Completed', 'Cancelled'].includes(order.status)" class="purchase-order-no-action" aria-hidden="true">—</span></div></td>
              </tr>
              <tr v-if="expandedOrderId === order.id" :id="`sales-order-details-${order.id}`" class="detail-row"><td colspan="8"><div class="order-detail"><div class="detail-head"><div><strong>{{ t('sales.detailTitle') }}</strong><p>{{ t('sales.detailDescription') }}</p></div><span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span></div><div class="table-wrap"><table><thead><tr><th>{{ t('sales.product') }}</th><th>{{ t('sales.quantity') }}</th><th>{{ t('sales.availableStock') }}</th><th>{{ t('sales.unitPrice') }}</th><th>{{ t('sales.subtotal') }}</th></tr></thead><tbody><tr v-for="item in order.items" :key="item.id"><td><div class="product-cell"><span class="product-avatar">{{ item.productName.slice(0, 2).toUpperCase() }}</span><span><strong>{{ item.productName }}</strong><small>{{ item.productSku }}</small></span></div></td><td>{{ formatQuantity(item.quantity) }} {{ item.unit }}</td><td>{{ formatQuantity(item.stockOnHand) }} {{ item.unit }}</td><td class="stock-value">{{ money(item.unitPrice) }}</td><td class="stock-value">{{ money(item.quantity * item.unitPrice) }}</td></tr></tbody></table></div><p v-if="order.notes" class="detail-note"><strong>{{ t('sales.notes') }}:</strong> {{ order.notes }}</p></div></td></tr>
            </template>
          </tbody>
        </table>
      </div>
      <PaginationControls v-if="!loading && orders.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal order-modal" @submit.prevent="createOrder"><div class="modal-head"><div><p class="eyebrow">{{ t('sales.eyebrow') }}</p><h2>{{ t('sales.newTitle') }}</h2><p>{{ t('sales.newDescription') }}</p></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeForm">×</button></div><div class="modal-body"><div class="form-grid"><label class="field-label full">{{ t('sales.customer') }}<select v-model="newOrder.customerId" required><option disabled value="">{{ t('sales.selectCustomer') }}</option><option v-for="customer in activeCustomers" :key="customer.id" :value="customer.id">{{ customer.name }} · {{ customer.code }}</option></select></label><label class="field-label full">{{ t('sales.notes') }}<textarea v-model="newOrder.notes" rows="2" maxlength="500" :placeholder="t('sales.notesPlaceholder')" /></label></div><p v-if="!activeCustomers.length || !activeProducts.length" class="alert modal-form-alert">{{ t('sales.masterDataHint') }}</p><div class="order-lines-head"><strong>{{ t('sales.items') }}</strong><button class="ghost-button" type="button" @click="addLine">+ {{ t('sales.addItem') }}</button></div><div class="order-lines"><div v-for="(line, index) in lines" :key="index" class="order-line"><label class="field-label">{{ t('sales.product') }}<select v-model="line.productId" required @change="setProductPrice(line)"><option disabled value="">{{ t('sales.selectProduct') }}</option><option v-for="product in activeProducts" :key="product.id" :value="product.id">{{ product.name }} · {{ product.sku }} · {{ formatQuantity(product.stockOnHand) }} {{ product.unit }}</option></select></label><label class="field-label">{{ t('sales.quantity') }}<FormattedNumberInput v-model="line.quantity" :decimal-scale="2" required :placeholder="t('sales.quantityPlaceholder')"></FormattedNumberInput></label><label class="field-label">{{ t('sales.unitPrice') }}<FormattedNumberInput v-model="line.unitPrice" required :placeholder="t('sales.pricePlaceholder')"></FormattedNumberInput></label><button v-if="lines.length > 1" class="remove-line" type="button" :aria-label="t('sales.removeItem')" @click="removeLine(index)">×</button></div></div><p class="order-total"><span>{{ t('sales.totalAmount') }}</span><strong>{{ money(totalAmount) }}</strong></p><p v-if="formError" class="alert modal-form-alert">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">{{ t('sales.createCancel') }}</button><button class="primary" :disabled="saving || !activeCustomers.length || !activeProducts.length">{{ saving ? t('sales.saving') : t('sales.createSave') }}</button></div></div></form></div></Teleport>
  </div>
</template>
