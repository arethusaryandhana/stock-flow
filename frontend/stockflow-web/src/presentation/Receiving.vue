<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'
import FormattedNumberInput from '../components/FormattedNumberInput.vue'

type PurchaseOrderItem = { id: string; productId: string; productSku: string; productName: string; unit: string; quantity: number; receivedQuantity: number; unitPrice: number }
type PurchaseOrder = { id: string; number: string; supplierName: string; status: string; orderDate: string; items: PurchaseOrderItem[] }
type Receipt = { id: string; number: string; purchaseOrderId: string; purchaseOrderNumber: string; supplierName: string; receivedAt: string; items: { id: string; productId: string; productSku: string; productName: string; unit: string; quantity: number }[] }

const route = useRoute()
const auth = useAuthStore()
const toast = useToastStore()
const { locale, t } = useI18n()
const orders = ref<PurchaseOrder[]>([])
const receipts = ref<Receipt[]>([])
const selectedOrderId = ref('')
const quantities = ref<Record<string, string>>({})
const search = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

const canManage = computed(() => ['admin', 'manager'].includes(auth.role.trim().toLowerCase()))
const selectedOrder = computed(() => orders.value.find((order) => order.id === selectedOrderId.value))
const receivableOrders = computed(() => orders.value.filter((order) => order.status === 'Approved'))
const receivedUnits = computed(() => receipts.value.reduce((total, receipt) => total + receipt.items.reduce((subtotal, item) => subtotal + item.quantity, 0), 0))
const date = (value: string) => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const formatQuantity = (value: number) => Number(value.toFixed(2)).toString()
const parseQuantity = (value: string | number) => {
  const parsed = Number(String(value ?? '').trim().replace(',', '.'))
  return Number.isFinite(parsed) ? parsed : 0
}
const remaining = (item: PurchaseOrderItem) => Math.max(0, Number((item.quantity - item.receivedQuantity).toFixed(2)))

function resetQuantities(order = selectedOrder.value) {
  quantities.value = {}
  order?.items.forEach((item) => { quantities.value[item.productId] = '' })
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [ordersResponse, receiptsResponse] = await Promise.all([
      api.get<PagedResponse<PurchaseOrder>>('/purchase-orders', { params: { page: 1, pageSize: 100, status: 'Approved' } }),
      api.get<PagedResponse<Receipt>>('/goods-receipts', { params: { page: page.value, pageSize: pageSize.value, search: search.value.trim() || undefined } }),
    ])
    orders.value = ordersResponse.data.items
    receipts.value = receiptsResponse.data.items
    page.value = receiptsResponse.data.page
    totalCount.value = receiptsResponse.data.totalCount
    totalPages.value = receiptsResponse.data.totalPages
    const requestedOrderId = String(route.query.purchaseOrderId ?? '')
    if (requestedOrderId && receivableOrders.value.some((order) => order.id === requestedOrderId)) selectedOrderId.value = requestedOrderId
    else if (!selectedOrderId.value || !receivableOrders.value.some((order) => order.id === selectedOrderId.value)) selectedOrderId.value = receivableOrders.value[0]?.id ?? ''
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function resetForm() {
  formError.value = ''
  selectedOrderId.value = ''
  quantities.value = {}
}

async function submit() {
  formError.value = ''
  const order = selectedOrder.value
  if (!order) {
    formError.value = t('receiving.required')
    return
  }

  const items = order.items
    .map((item) => ({ productId: item.productId, quantity: parseQuantity(quantities.value[item.productId] ?? '') }))
    .filter((item) => item.quantity > 0)
  if (!items.length) {
    formError.value = t('receiving.required')
    return
  }
  if (items.some((item) => {
    const orderItem = order.items.find((candidate) => candidate.productId === item.productId)
    return !orderItem || item.quantity > remaining(orderItem) || Number(item.quantity.toFixed(2)) !== item.quantity
  })) {
    formError.value = t('receiving.invalidQuantity')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<Receipt>('/goods-receipts', { purchaseOrderId: order.id, items })
    await load()
    resetForm()
    toast.success(t('receiving.createdToast', { number: data.number }))
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally {
    saving.value = false
  }
}

function exportCsv() {
  const rows = [[t('receiving.date'), t('receiving.number'), t('receiving.orderNumber'), t('receiving.supplier'), t('receiving.items')], ...receipts.value.map((receipt) => [date(receipt.receivedAt), receipt.number, receipt.purchaseOrderNumber, receipt.supplierName, String(receipt.items.length)])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a')
  link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  link.download = 'stockflow-goods-receipts.csv'
  link.click()
  URL.revokeObjectURL(link.href)
}

function changePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}

watch(selectedOrderId, () => resetQuantities())
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
    <div class="page-heading"><div><p class="eyebrow">{{ t('receiving.eyebrow') }}</p><h1>{{ t('receiving.title') }}</h1><p class="subtitle">{{ t('receiving.subtitle') }}</p></div><div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('receiving.export') }}</button></div></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">↓</span><span><small>{{ t('receiving.totalReceipts') }}</small><strong>{{ loading ? '—' : totalCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon amber">◷</span><span><small>{{ t('receiving.readyOrders') }}</small><strong>{{ loading ? '—' : receivableOrders.length }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">✓</span><span><small>{{ t('receiving.totalUnits') }}</small><strong>{{ loading ? '—' : formatQuantity(receivedUnits) }}</strong></span></div></div>

    <section v-if="canManage" class="surface-card form-panel receiving-panel">
      <div class="surface-card-head"><div><h2>{{ t('receiving.newTitle') }}</h2><p>{{ t('receiving.newDescription') }}</p></div><span class="badge neutral">{{ t('receiving.receivingActive') }}</span></div>
      <form @submit.prevent="submit"><div class="receiving-grid"><label class="field-label">{{ t('receiving.purchaseOrder') }}<select v-model="selectedOrderId" :disabled="loading || !receivableOrders.length"><option disabled value="">{{ t('receiving.selectOrder') }}</option><option v-for="order in receivableOrders" :key="order.id" :value="order.id">{{ order.number }} · {{ order.supplierName }}</option></select></label></div><div v-if="selectedOrder" class="receiving-lines"><div class="receiving-line receiving-line-head"><span>{{ t('receiving.product') }}</span><span>{{ t('receiving.ordered') }}</span><span>{{ t('receiving.remaining') }}</span><span>{{ t('receiving.receiveQuantity') }}</span></div><div v-for="item in selectedOrder.items" :key="item.id" class="receiving-line"><div><strong>{{ item.productName }}</strong><small>{{ item.productSku }}</small></div><span>{{ formatQuantity(item.quantity) }} {{ item.unit }}</span><span class="stock-value low">{{ formatQuantity(remaining(item)) }} {{ item.unit }}</span><FormattedNumberInput v-model="quantities[item.productId]" :decimal-scale="2" :placeholder="remaining(item).toString()" :aria-label="`${t('receiving.receiveQuantity')} ${item.productName}`"></FormattedNumberInput></div><small class="field-hint">{{ t('receiving.receiveHint') }}</small></div><p v-else class="empty compact-empty">{{ t('receiving.noOrders') }}</p><p v-if="formError" class="alert adjustment-form-alert">{{ formError }}</p><div class="adjustment-actions receiving-actions"><button class="secondary" type="button" @click="resetForm">{{ t('receiving.reset') }}</button><button class="primary" type="submit" :disabled="saving || loading || !receivableOrders.length">{{ saving ? t('operations.saving') : t('receiving.receive') }}</button></div></form>
    </section>

    <section class="surface-card page-panel spaced-page-panel"><div class="history-head"><div><h2>{{ t('receiving.historyTitle') }}</h2><p>{{ t('receiving.historyDescription') }}</p></div><button class="ghost-button" type="button" @click="exportCsv">{{ t('receiving.export') }}</button></div><div class="history-filter"><label class="search-input"><span>⌕</span><input v-model="search" :aria-label="t('receiving.searchAria')" :placeholder="t('receiving.searchPlaceholder')"></label></div><div v-if="loading" class="empty">{{ t('receiving.loading') }}</div><div v-else-if="!receipts.length" class="empty"><strong>{{ t('receiving.emptyTitle') }}</strong>{{ t('receiving.emptyHint') }}</div><div v-else class="table-wrap"><table><thead><tr><th>{{ t('receiving.date') }}</th><th>{{ t('receiving.number') }}</th><th>{{ t('receiving.orderNumber') }}</th><th>{{ t('receiving.supplier') }}</th><th>{{ t('receiving.items') }}</th></tr></thead><tbody><tr v-for="receipt in receipts" :key="receipt.id"><td class="date-cell">{{ date(receipt.receivedAt) }}</td><td class="stock-value">{{ receipt.number }}</td><td class="muted-cell">{{ receipt.purchaseOrderNumber }}</td><td><strong>{{ receipt.supplierName }}</strong></td><td>{{ receipt.items.length }}</td></tr></tbody></table></div><PaginationControls v-if="!loading && receipts.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" /></section>
  </div>
</template>
