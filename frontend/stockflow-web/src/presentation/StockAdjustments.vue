<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'

type Product = { id: string; sku: string; name: string; stockOnHand: number; unit: string; isActive: boolean }
type Adjustment = { id: string; number: string; productId: string; productSku: string; productName: string; unit: string; quantityDelta: number; reason: string; createdAt: string }
type AdjustmentForm = { productId: string; quantityDelta: string; reason: string }
const products = ref<Product[]>([])
const adjustments = ref<Adjustment[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const form = ref<AdjustmentForm>({ productId: '', quantityDelta: '', reason: '' })
const historyQuery = ref('')
const { locale, t } = useI18n()
const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const selectedProduct = computed(() => products.value.find((product) => product.id === form.value.productId))
const quantityDelta = computed(() => parseQuantity(form.value.quantityDelta))
const quantityDeltaValue = computed(() => quantityDelta.value ?? 0)
const newBalance = computed(() => {
  const stockOnHand = selectedProduct.value?.stockOnHand ?? 0
  return quantityDelta.value === null ? stockOnHand : roundQuantity(stockOnHand + quantityDelta.value)
})
const date = (value: string) => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const roundQuantity = (value: number) => Number(value.toFixed(2))
const formatQuantity = (value: number) => roundQuantity(value).toFixed(2)
function parseQuantity(value: string): number | null {
  const normalized = value.trim()
  if (!/^[+-]?\d+(?:\.\d{1,2})?$/.test(normalized)) return null
  const parsed = Number(normalized)
  return Number.isFinite(parsed) ? parsed : null
}
const delta = (adjustment: Adjustment) => `${adjustment.quantityDelta > 0 ? '+' : '−'}${formatQuantity(Math.abs(adjustment.quantityDelta))} ${adjustment.unit}`
const filteredAdjustments = computed(() => {
  const term = historyQuery.value.toLowerCase().trim()
  return adjustments.value.filter((item) => `${item.number} ${item.productName} ${item.productSku} ${item.reason}`.toLowerCase().includes(term))
})
async function load() {
  loading.value = true; error.value = ''
  try {
    const [productsResponse, adjustmentsResponse] = await Promise.all([api.get<Product[]>('/products'), api.get<Adjustment[]>('/stock-adjustments')])
    products.value = productsResponse.data; adjustments.value = adjustmentsResponse.data
    if (!form.value.productId && activeProducts.value[0]) form.value.productId = activeProducts.value[0].id
  } catch (requestError) { error.value = (requestError as Error).message } finally { loading.value = false }
}
function normalizeQuantity() {
  if (quantityDelta.value !== null) form.value.quantityDelta = formatQuantity(quantityDelta.value)
}
function setDirection(direction: 'in' | 'out') {
  const magnitude = quantityDelta.value === null ? 0 : Math.abs(quantityDelta.value)
  form.value.quantityDelta = `${direction === 'in' ? '' : '-'}${formatQuantity(magnitude)}`
}
async function submit() {
  formError.value = ''
  if (quantityDelta.value === null) { formError.value = t('adjustments.invalidChange'); return }
  if (newBalance.value < 0) { formError.value = t('adjustments.invalidBalance'); return }
  if (!quantityDelta.value) { formError.value = t('adjustments.missingChange'); return }
  saving.value = true
  try {
    const { data } = await api.post<Adjustment>('/stock-adjustments', { productId: form.value.productId, quantityDelta: quantityDelta.value, reason: form.value.reason })
    adjustments.value.unshift(data)
    const product = products.value.find((item) => item.id === data.productId)
    if (product) product.stockOnHand = roundQuantity(product.stockOnHand + data.quantityDelta)
    form.value.quantityDelta = ''; form.value.reason = ''
  } catch (requestError) { formError.value = (requestError as Error).message } finally { saving.value = false }
}
function exportCsv() {
  const rows = [[t('common.date'), t('adjustments.number'), t('adjustments.product'), 'SKU', t('adjustments.change'), t('adjustments.reason')], ...filteredAdjustments.value.map((item) => [date(item.createdAt), item.number, item.productName, item.productSku, delta(item), item.reason])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-penyesuaian.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading"><div><p class="eyebrow">{{ t('adjustments.eyebrow') }}</p><h1>{{ t('adjustments.title') }}</h1><p class="subtitle">{{ t('adjustments.subtitle') }}</p></div><div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button></div></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <section class="surface-card form-panel">
      <div class="surface-card-head"><div><h2>{{ t('adjustments.newTitle') }}</h2><p>{{ t('adjustments.newDescription') }}</p></div><span class="badge neutral">{{ t('adjustments.auditActive') }}</span></div>
      <form class="adjustment-grid" @submit.prevent="submit">
        <label class="field-label">{{ t('adjustments.product') }}<select v-model="form.productId" required :disabled="loading || !activeProducts.length"><option v-for="product in activeProducts" :key="product.id" :value="product.id">{{ product.name }} · {{ product.sku }}</option></select></label>
        <label class="field-label">{{ t('adjustments.stockChange') }}<div class="quantity-control"><button type="button" :class="{ selected: quantityDeltaValue > 0, in: quantityDeltaValue > 0 }" :aria-label="t('adjustments.addStock')" @click="setDirection('in')">+</button><input v-model="form.quantityDelta" type="number" step="0.01" inputmode="decimal" placeholder="0.00" required @blur="normalizeQuantity"><button type="button" :class="{ selected: quantityDeltaValue < 0, out: quantityDeltaValue < 0 }" :aria-label="t('adjustments.removeStock')" @click="setDirection('out')">−</button></div><small class="field-hint">{{ t('adjustments.stockChangeHint') }}</small></label>
        <label class="field-label">{{ t('adjustments.reason') }}<input v-model.trim="form.reason" required maxlength="300" :placeholder="t('adjustments.reasonPlaceholder')"></label>
        <p v-if="selectedProduct" class="adjustment-helper" :class="{ warning: newBalance < 0 }"><span>i</span> {{ t('adjustments.currentBalance') }} <strong>{{ formatQuantity(selectedProduct.stockOnHand) }} {{ selectedProduct.unit }}</strong> {{ t('adjustments.willBecome') }} <strong>{{ formatQuantity(newBalance) }} {{ selectedProduct.unit }}</strong>.</p>
        <p v-if="formError" class="alert" style="grid-column: 1 / -1; margin: 14px 19px 0">{{ formError }}</p>
        <div class="adjustment-actions" style="grid-column: 1 / -1"><button class="secondary" type="button" @click="form.quantityDelta = ''; form.reason = ''">{{ t('adjustments.reset') }}</button><button class="primary" :disabled="saving || loading || !activeProducts.length">{{ saving ? t('common.saving') : t('adjustments.save') }}</button></div>
      </form>
    </section>

    <section class="surface-card page-panel" style="margin-top: 14px">
      <div class="history-head"><div><h2>{{ t('adjustments.historyTitle') }}</h2><p>{{ t('adjustments.historyDescription') }}</p></div><button class="ghost-button" type="button" @click="exportCsv">{{ t('adjustments.exportHistory') }}</button></div>
      <div class="history-filter"><label class="search-input"><span>⌕</span><input v-model="historyQuery" :aria-label="t('adjustments.searchAria')" :placeholder="t('adjustments.searchPlaceholder')"></label></div>
      <div v-if="loading" class="empty">{{ t('adjustments.loading') }}</div>
      <div v-else-if="!filteredAdjustments.length" class="empty"><strong>{{ t('adjustments.emptyTitle') }}</strong>{{ t('adjustments.emptyHint') }}</div>
      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('common.date') }}</th><th>{{ t('adjustments.number') }}</th><th>{{ t('adjustments.product') }}</th><th>{{ t('adjustments.change') }}</th><th>{{ t('adjustments.reason') }}</th></tr></thead><tbody><tr v-for="adjustment in filteredAdjustments" :key="adjustment.id"><td class="date-cell">{{ date(adjustment.createdAt) }}</td><td class="muted-cell">{{ adjustment.number }}</td><td><div class="product-cell"><span class="product-avatar amber">{{ adjustment.productName.slice(0, 2).toUpperCase() }}</span><span><strong>{{ adjustment.productName }}</strong><small>{{ adjustment.productSku }}</small></span></div></td><td :class="adjustment.quantityDelta > 0 ? 'quantity-in' : 'quantity-out'">{{ delta(adjustment) }}</td><td>{{ adjustment.reason }}</td></tr></tbody></table></div>
    </section>
  </div>
</template>
