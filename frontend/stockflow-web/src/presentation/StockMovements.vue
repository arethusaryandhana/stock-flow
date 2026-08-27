<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'

type Movement = { id: string; productId: string; productSku: string; productName: string; unit: string; type: string; quantity: number; balanceAfter: number; referenceNumber: string; reason: string | null; createdAt: string }
const movements = ref<Movement[]>([])
const q = ref('')
const typeFilter = ref('all')
const period = ref('30')
const loading = ref(true)
const error = ref('')
const { locale, t } = useI18n()
const isInbound = (movement: Movement) => movement.type === 'GoodsReceipt' || movement.type === 'AdjustmentIn'
const label = (movement: Movement) => movement.type === 'GoodsReceipt' ? t('movements.receipt') : movement.type === 'Sale' ? t('movements.sale') : movement.type === 'AdjustmentIn' ? t('movements.adjustmentIn') : movement.type === 'AdjustmentOut' ? t('movements.adjustmentOut') : movement.type
const quantity = (movement: Movement) => `${isInbound(movement) ? '+' : '−'}${movement.quantity} ${movement.unit}`
const date = (value: string) => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()
const filtered = computed(() => {
  const term = q.value.toLowerCase().trim()
  return movements.value.filter((movement) => `${movement.productSku} ${movement.productName} ${movement.referenceNumber} ${movement.reason ?? ''}`.toLowerCase().includes(term) && (typeFilter.value === 'all' || movement.type === typeFilter.value))
})
const received = computed(() => movements.value.filter(isInbound).reduce((sum, movement) => sum + movement.quantity, 0))
const shipped = computed(() => movements.value.filter((movement) => !isInbound(movement)).reduce((sum, movement) => sum + movement.quantity, 0))
const todayCount = computed(() => movements.value.filter((movement) => new Date(movement.createdAt).toDateString() === new Date().toDateString()).length)
async function load() {
  loading.value = true; error.value = ''
  try { movements.value = (await api.get<Movement[]>('/stock-movements')).data } catch (requestError) { error.value = (requestError as Error).message } finally { loading.value = false }
}
function exportCsv() {
  const rows = [[t('common.date'), t('products.product'), 'SKU', t('movements.activityType'), t('movements.quantity'), t('movements.balance'), t('movements.reference'), t('movements.note')], ...filtered.value.map((item) => [date(item.createdAt), item.productName, item.productSku, label(item), String(item.quantity), String(item.balanceAfter), item.referenceNumber, item.reason ?? ''])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-pergerakan.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">{{ t('movements.eyebrow') }}</p><h1>{{ t('movements.title') }}</h1><p class="subtitle">{{ t('movements.subtitle') }}</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">{{ t('common.exportCsv') }}</button></div>
    </div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">◷</span><span><small>{{ t('movements.todayActivity') }}</small><strong>{{ loading ? '—' : todayCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">↓</span><span><small>{{ t('movements.totalInbound') }}</small><strong>{{ loading ? '—' : received }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">↑</span><span><small>{{ t('movements.totalOutbound') }}</small><strong>{{ loading ? '—' : shipped }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="toolbar"><label class="search-input"><span>⌕</span><input v-model="q" :aria-label="t('movements.searchAria')" :placeholder="t('movements.searchPlaceholder')"></label><div class="toolbar-actions"><select v-model="typeFilter" class="filter-select wide" :aria-label="t('movements.typeFilterAria')"><option value="all">{{ t('movements.allTypes') }}</option><option value="GoodsReceipt">{{ t('movements.receipt') }}</option><option value="Sale">{{ t('movements.sale') }}</option><option value="AdjustmentIn">{{ t('movements.adjustmentIn') }}</option><option value="AdjustmentOut">{{ t('movements.adjustmentOut') }}</option></select><select v-model="period" class="filter-select" :aria-label="t('movements.periodAria')"><option value="7">{{ t('movements.last7') }}</option><option value="30">{{ t('movements.last30') }}</option><option value="90">{{ t('movements.last90') }}</option></select></div></div>
      <div class="section-note" style="padding: 12px 18px 0"><span class="status-dot" /> {{ t('movements.showing', { count: filtered.length }) }}</div>
      <div v-if="loading" class="empty">{{ t('movements.loading') }}</div>
      <div v-else-if="!filtered.length" class="empty"><strong>{{ t('movements.emptyTitle') }}</strong>{{ t('movements.emptyHint') }}</div>
      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('common.date') }}</th><th>{{ t('products.product') }}</th><th>{{ t('movements.activityType') }}</th><th>{{ t('movements.change') }}</th><th>{{ t('movements.balance') }}</th><th>{{ t('movements.reference') }}</th><th>{{ t('movements.note') }}</th></tr></thead><tbody><tr v-for="movement in filtered" :key="movement.id"><td class="date-cell">{{ date(movement.createdAt) }}</td><td><div class="product-cell"><span class="product-avatar" :class="{ teal: isInbound(movement) }">{{ shortName(movement.productName) }}</span><span><strong>{{ movement.productName }}</strong><small>{{ movement.productSku }}</small></span></div></td><td><span class="badge" :class="isInbound(movement) ? 'ok' : 'danger'">{{ label(movement) }}</span></td><td :class="isInbound(movement) ? 'quantity-in' : 'quantity-out'">{{ quantity(movement) }}</td><td class="stock-value">{{ movement.balanceAfter }} {{ movement.unit }}</td><td class="muted-cell">{{ movement.referenceNumber }}</td><td class="muted-cell">{{ movement.reason || '—' }}</td></tr></tbody></table></div>
    </section>
  </div>
</template>
