<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'

type DashboardData = {
  products: number
  lowStock: number
  purchases: number
  salesToday: number
  healthyProducts: number
  outOfStockProducts: number
  totalUnits: number
}
type LowStockProduct = { id: string; sku: string; name: string; category: string; stockOnHand: number; reorderLevel: number; unit: string }
type Movement = { id: string; productName: string; productSku: string; unit: string; type: string; quantity: number; reason: string | null; createdAt: string }
type MovementPageResponse = PagedResponse<Movement> & { summary: { todayCount: number; inboundQuantity: number; outboundQuantity: number } }

const data = ref<DashboardData>({ products: 0, lowStock: 0, purchases: 0, salesToday: 0, healthyProducts: 0, outOfStockProducts: 0, totalUnits: 0 })
const movements = ref<Movement[]>([])
const attention = ref<LowStockProduct[]>([])
const loading = ref(true)
const attentionLoading = ref(true)
const error = ref('')
const attentionPage = ref(1)
const attentionPageSize = ref(10)
const attentionTotalCount = ref(0)
const attentionTotalPages = ref(0)
const { locale, t } = useI18n()

const money = (value: number) => new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const date = (value: string) => new Intl.DateTimeFormat(locale.value, { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()
const healthyStock = computed(() => data.value.healthyProducts)
const lowStock = computed(() => data.value.lowStock)
const outOfStock = computed(() => data.value.outOfStockProducts)
const totalUnits = computed(() => data.value.totalUnits)
const healthPercent = computed(() => data.value.products ? Math.round((healthyStock.value / data.value.products) * 100) : 0)
const healthGradient = computed(() => {
  const total = Math.max(data.value.products, 1)
  const healthy = healthyStock.value / total * 100
  const low = lowStock.value / total * 100
  return `conic-gradient(#129a88 0 ${healthy}%, #f2b258 ${healthy}% ${healthy + low}%, #dd6c76 ${healthy + low}% 100%)`
})
const chartBars = computed(() => {
  const buckets = Array.from({ length: 12 }, () => ({ inbound: 0, outbound: 0 }))
  const now = Date.now()
  movements.value.forEach((movement) => {
    const age = Math.max(0, now - new Date(movement.createdAt).getTime())
    const bucket = Math.min(11, Math.floor(age / (30 * 24 * 60 * 60 * 1000) * 12))
    if (isInbound(movement)) buckets[11 - bucket].inbound += movement.quantity
    else buckets[11 - bucket].outbound += movement.quantity
  })
  const max = Math.max(...buckets.flatMap((bucket) => [bucket.inbound, bucket.outbound]), 1)
  return buckets.map((bucket) => ({ inbound: Math.round(bucket.inbound / max * 72), outbound: Math.round(bucket.outbound / max * 50) }))
})
const chartLabels = ['01', '03', '05', '07', '09', '11', '13', '15', '17', '19', '21', '23']
const latestMovements = computed(() => movements.value.slice(0, 4))
const isInbound = (movement: Movement) => movement.type === 'GoodsReceipt' || movement.type === 'AdjustmentIn'

async function loadAttention() {
  attentionLoading.value = true
  try {
    const response = await api.get<PagedResponse<LowStockProduct>>('/products', { params: { page: attentionPage.value, pageSize: attentionPageSize.value, status: 'attention' } })
    attention.value = response.data.items
    attentionPage.value = response.data.page
    attentionTotalCount.value = response.data.totalCount
    attentionTotalPages.value = response.data.totalPages
  } catch (requestError) {
    error.value = (requestError as Error)?.message || t('dashboard.loadError')
  } finally {
    attentionLoading.value = false
  }
}
function changeAttentionPageSize(nextPageSize: number) {
  attentionPageSize.value = nextPageSize
  attentionPage.value = 1
}

async function load() {
  loading.value = true
  error.value = ''
  const [dashboardResult, movementsResult, attentionResult] = await Promise.allSettled([
    api.get<DashboardData>('/dashboard'),
    api.get<MovementPageResponse>('/stock-movements', { params: { page: 1, pageSize: 100, periodDays: 30 } }),
    api.get<PagedResponse<LowStockProduct>>('/products', { params: { page: 1, pageSize: attentionPageSize.value, status: 'attention' } }),
  ])
  if (dashboardResult.status === 'fulfilled') data.value = dashboardResult.value.data
  else error.value = (dashboardResult.reason as Error)?.message || t('dashboard.loadError')
  if (movementsResult.status === 'fulfilled') movements.value = movementsResult.value.data.items
  if (attentionResult.status === 'fulfilled') {
    attention.value = attentionResult.value.data.items
    attentionPage.value = attentionResult.value.data.page
    attentionTotalCount.value = attentionResult.value.data.totalCount
    attentionTotalPages.value = attentionResult.value.data.totalPages
    attentionLoading.value = false
  } else {
    attentionLoading.value = false
    if (!error.value) error.value = (attentionResult.reason as Error)?.message || t('dashboard.loadError')
  }
  loading.value = false
}

watch(attentionPage, (nextPage, previousPage) => {
  if (nextPage !== previousPage) void loadAttention()
})
watch(attentionPageSize, () => void loadAttention())
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading page-heading-dashboard">
      <div>
        <p class="eyebrow">{{ t('dashboard.eyebrow') }}</p>
        <h1>{{ t('dashboard.greeting') }} <span aria-hidden="true">👋</span></h1>
        <p class="subtitle">{{ t('dashboard.subtitle') }}</p>
      </div>
      <div class="header-actions">
        <div class="date-control" :aria-label="t('dashboard.dateAria')"><span class="date-icon" aria-hidden="true">◷</span> {{ t('dashboard.date') }} <span class="chevron-icon" aria-hidden="true" /></div>
      </div>
    </div>

    <div class="notice-strip"><span class="status-dot" /><span><strong>{{ t('dashboard.liveInventory') }}</strong> · {{ t('dashboard.updated') }}</span></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <section class="metric-grid" :aria-label="t('dashboard.metricsAria')">
      <router-link class="metric-card dashboard-link" to="/products" :aria-label="t('dashboard.openProducts')">
        <div class="metric-top"><span class="metric-label">{{ t('dashboard.activeProducts') }}</span><span class="metric-icon blue">▦</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.products }}</strong>
        <div class="metric-meta"><span class="metric-trend up">{{ t('dashboard.active') }}</span> {{ t('dashboard.registeredInWorkspace') }}</div>
      </router-link>
      <router-link class="metric-card dashboard-link" to="/products" :aria-label="t('dashboard.openLowStock')">
        <div class="metric-top"><span class="metric-label">{{ t('dashboard.lowStock') }}</span><span class="metric-icon amber">△</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.lowStock }}</strong>
        <div class="metric-meta"><span class="metric-trend warn">{{ t('dashboard.needsAction') }}</span> {{ t('dashboard.belowMinimum') }}</div>
      </router-link>
      <article class="metric-card">
        <div class="metric-top"><span class="metric-label">{{ t('dashboard.purchaseOrder') }}</span><span class="metric-icon teal">▤</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.purchases }}</strong>
        <div class="metric-meta"><span class="metric-trend up">{{ t('dashboard.active') }}</span> {{ t('dashboard.waitingToProcess') }}</div>
      </article>
      <router-link class="metric-card dashboard-link" to="/inventory/movements" :aria-label="t('dashboard.openSales')">
        <div class="metric-top"><span class="metric-label">{{ t('dashboard.salesToday') }}</span><span class="metric-icon red" aria-hidden="true"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M6.5 3.75h11v16.5l-2.75-1.75L12 20.25l-2.75-1.75L6.5 20.25V3.75Z" /><path d="M9.5 8h5M9.5 11.5h5M9.5 15h2.5" /></svg></span></div>
        <strong class="metric-value currency">{{ loading ? '—' : money(data.salesToday) }}</strong>
        <div class="metric-meta"><span class="metric-trend up">{{ t('dashboard.today') }}</span> {{ t('dashboard.recordedTransactions') }}</div>
      </router-link>
    </section>

    <div class="dashboard-grid">
      <router-link class="surface-card dashboard-link" to="/inventory/movements" :aria-label="t('dashboard.openMovements')">
        <div class="surface-card-head">
          <div><h2>{{ t('dashboard.inventoryActivity') }}</h2><p>{{ t('dashboard.inventoryActivitySubtitle') }}</p></div>
          <span class="dashboard-link-arrow" aria-hidden="true">↗</span>
        </div>
        <div class="chart-area">
          <div class="chart-y"><span>100</span><span>75</span><span>50</span><span>25</span><span>0</span></div>
          <div class="chart">
            <div v-for="(bar, index) in chartBars" :key="chartLabels[index]" class="chart-bar"><span :style="{ height: `${bar.outbound}px` }" /><span :style="{ height: `${bar.inbound}px` }" /><label>{{ chartLabels[index] }}</label></div>
            <div v-if="!movements.length && !loading" class="chart-empty">{{ t('dashboard.noTransactions') }}</div>
          </div>
        </div>
        <div class="chart-legend"><span class="legend-item"><i class="legend-dot strong" /> {{ t('dashboard.inbound') }}</span><span class="legend-item"><i class="legend-dot" /> {{ t('dashboard.outbound') }}</span></div>
      </router-link>

      <router-link class="surface-card dashboard-link" to="/products" :aria-label="t('dashboard.openProducts')">
        <div class="surface-card-head"><div><h2>{{ t('dashboard.stockHealth') }}</h2><p>{{ t('dashboard.stockHealthSubtitle') }}</p></div><span class="dashboard-link-arrow" aria-hidden="true">↗</span></div>
        <div class="health-body">
          <div class="health-ring" :style="{ background: healthGradient }"><div class="health-ring-copy"><strong>{{ healthPercent }}%</strong><span>{{ t('dashboard.safe') }}</span></div></div>
          <div class="health-legend">
            <div class="health-row"><span class="health-row-label"><i /> {{ t('dashboard.safeStock') }}</span><strong>{{ healthyStock }}</strong></div>
            <div class="health-row"><span class="health-row-label"><i class="amber" /> {{ t('dashboard.lowStock') }}</span><strong>{{ lowStock }}</strong></div>
            <div class="health-row"><span class="health-row-label"><i class="red" /> {{ t('dashboard.depletedStock') }}</span><strong>{{ outOfStock }}</strong></div>
            <div class="health-note">{{ t('dashboard.totalUnits', { count: totalUnits }) }}</div>
          </div>
        </div>
      </router-link>
    </div>

    <div class="two-column">
      <router-link class="surface-card dashboard-link" to="/products" :aria-label="t('dashboard.openAttention')">
        <div class="surface-card-head"><div><h2>{{ t('dashboard.attention') }}</h2><p>{{ t('dashboard.attentionSubtitle') }}</p></div><span class="dashboard-link-arrow" aria-hidden="true">↗</span></div>
        <div v-if="loading || attentionLoading" class="empty">{{ t('dashboard.loadingProducts') }}</div>
        <div v-else-if="!attention.length" class="empty"><strong>{{ t('dashboard.allSafe') }}</strong>{{ t('dashboard.noRestock') }}</div>
        <div v-else class="table-wrap"><table><thead><tr><th>{{ t('products.product') }}</th><th>{{ t('dashboard.remainingStock') }}</th><th>{{ t('dashboard.status') }}</th></tr></thead><tbody><tr v-for="product in attention" :key="product.id"><td><div class="product-cell"><span class="product-avatar amber">{{ shortName(product.name) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.sku }} · {{ product.category }}</small></span></div></td><td><span class="stock-value" :class="product.stockOnHand <= 0 ? 'out' : 'low'">{{ product.stockOnHand }} {{ product.unit }}</span><small>{{ t('products.min') }} {{ product.reorderLevel }}</small></td><td><span class="badge" :class="product.stockOnHand <= 0 ? 'danger' : 'warn'">{{ product.stockOnHand <= 0 ? t('products.out') : t('products.low') }}</span></td></tr></tbody></table></div>
        <PaginationControls v-if="!loading && !attentionLoading && attention.length" @click.stop :page="attentionPage" :page-size="attentionPageSize" :total-count="attentionTotalCount" :total-pages="attentionTotalPages" @page-change="attentionPage = $event" @page-size-change="changeAttentionPageSize" />
      </router-link>

      <router-link class="surface-card dashboard-link" to="/inventory/movements" :aria-label="t('dashboard.openLatest')">
        <div class="surface-card-head"><div><h2>{{ t('dashboard.latestActivity') }}</h2><p>{{ t('dashboard.latestActivitySubtitle') }}</p></div><span class="dashboard-link-arrow" aria-hidden="true">↗</span></div>
        <div v-if="loading" class="empty">{{ t('dashboard.loadingActivity') }}</div>
        <div v-else-if="!latestMovements.length" class="empty"><strong>{{ t('dashboard.noActivity') }}</strong>{{ t('dashboard.noActivityHint') }}</div>
        <div v-else class="activity-list"><div v-for="movement in latestMovements" :key="movement.id" class="activity-item"><span class="activity-icon" :class="{ out: movement.type === 'Sale' || movement.type === 'AdjustmentOut' }">{{ movement.type === 'Sale' || movement.type === 'AdjustmentOut' ? '↑' : '↓' }}</span><div class="activity-copy"><strong>{{ movement.productName }}</strong><p>{{ movement.type === 'Sale' ? t('dashboard.sale') : movement.type === 'GoodsReceipt' ? t('dashboard.inbound') : t('dashboard.adjustment') }} · {{ movement.quantity }} {{ movement.unit }}</p></div><span class="activity-time">{{ date(movement.createdAt) }}</span></div></div>
      </router-link>
    </div>
  </div>
</template>
