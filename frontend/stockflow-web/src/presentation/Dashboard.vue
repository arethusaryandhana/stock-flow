<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../infrastructure/api'

type DashboardData = {
  products: number
  lowStock: number
  purchases: number
  salesToday: number
  attention: LowStockProduct[]
}
type LowStockProduct = { id: string; sku: string; name: string; category: string; stockOnHand: number; reorderLevel: number; unit: string }
type Product = { id: string; sku: string; name: string; category: string; stockOnHand: number; reorderLevel: number; unit: string; isActive: boolean }
type Movement = { id: string; productName: string; productSku: string; unit: string; type: string; quantity: number; reason: string | null; createdAt: string }

const router = useRouter()
const data = ref<DashboardData>({ products: 0, lowStock: 0, purchases: 0, salesToday: 0, attention: [] })
const products = ref<Product[]>([])
const movements = ref<Movement[]>([])
const loading = ref(true)
const error = ref('')

const money = (value: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const date = (value: string) => new Intl.DateTimeFormat('id-ID', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()
const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const outOfStock = computed(() => activeProducts.value.filter((product) => product.stockOnHand <= 0).length)
const lowStock = computed(() => activeProducts.value.filter((product) => product.stockOnHand > 0 && product.stockOnHand <= product.reorderLevel).length)
const healthyStock = computed(() => Math.max(activeProducts.value.length - outOfStock.value - lowStock.value, 0))
const totalUnits = computed(() => activeProducts.value.reduce((sum, product) => sum + product.stockOnHand, 0))
const healthPercent = computed(() => activeProducts.value.length ? Math.round((healthyStock.value / activeProducts.value.length) * 100) : 0)
const healthGradient = computed(() => {
  const total = Math.max(activeProducts.value.length, 1)
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

async function load() {
  loading.value = true
  error.value = ''
  const [dashboardResult, productsResult, movementsResult] = await Promise.allSettled([
    api.get<DashboardData>('/dashboard'),
    api.get<Product[]>('/products'),
    api.get<Movement[]>('/stock-movements'),
  ])
  if (dashboardResult.status === 'fulfilled') data.value = dashboardResult.value.data
  else error.value = (dashboardResult.reason as Error)?.message || 'Ringkasan belum dapat dimuat.'
  if (productsResult.status === 'fulfilled') products.value = productsResult.value.data
  if (movementsResult.status === 'fulfilled') movements.value = movementsResult.value.data
  loading.value = false
}

function go(path: string) { router.push(path) }
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading page-heading-dashboard">
      <div>
        <p class="eyebrow">WORKSPACE OVERVIEW</p>
        <h1>Selamat pagi, Admin <span aria-hidden="true">👋</span></h1>
        <p class="subtitle">Pantau kondisi inventori dan ambil tindakan sebelum operasional terhambat.</p>
      </div>
      <div class="header-actions">
        <button class="date-control" type="button"><span>◷</span> 01 – 27 Agu 2026 <span>⌄</span></button>
        <button class="primary" type="button" @click="go('/inventory/adjustments')"><span class="button-plus">+</span> Sesuaikan stok</button>
      </div>
    </div>

    <div class="notice-strip"><span class="status-dot" /><span><strong>Live inventory</strong> · Data terakhir diperbarui beberapa detik lalu dari workspace StockFlow Demo.</span></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <section class="metric-grid" aria-label="Ringkasan metrik">
      <article class="metric-card">
        <div class="metric-top"><span class="metric-label">Total produk aktif</span><span class="metric-icon blue">▦</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.products }}</strong>
        <div class="metric-meta"><span class="metric-trend up">Aktif</span> terdaftar di workspace</div>
      </article>
      <article class="metric-card">
        <div class="metric-top"><span class="metric-label">Stok menipis</span><span class="metric-icon amber">△</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.lowStock }}</strong>
        <div class="metric-meta"><span class="metric-trend warn">Perlu tindakan</span> di bawah minimum</div>
      </article>
      <article class="metric-card">
        <div class="metric-top"><span class="metric-label">Purchase order</span><span class="metric-icon teal">▤</span></div>
        <strong class="metric-value">{{ loading ? '—' : data.purchases }}</strong>
        <div class="metric-meta"><span class="metric-trend up">Aktif</span> menunggu diproses</div>
      </article>
      <article class="metric-card">
        <div class="metric-top"><span class="metric-label">Penjualan hari ini</span><span class="metric-icon red">↗</span></div>
        <strong class="metric-value currency">{{ loading ? '—' : money(data.salesToday) }}</strong>
        <div class="metric-meta"><span class="metric-trend up">Hari ini</span> nilai transaksi tercatat</div>
      </article>
    </section>

    <div class="dashboard-grid">
      <section class="surface-card">
        <div class="surface-card-head">
          <div><h2>Aktivitas inventori</h2><p>Ringkasan barang masuk dan keluar dalam 30 hari terakhir</p></div>
          <button class="ghost-button" type="button" @click="go('/inventory/movements')">Lihat detail ↗</button>
        </div>
        <div class="chart-area">
          <div class="chart-y"><span>100</span><span>75</span><span>50</span><span>25</span><span>0</span></div>
          <div class="chart">
            <div v-for="(bar, index) in chartBars" :key="chartLabels[index]" class="chart-bar"><span :style="{ height: `${bar.outbound}px` }" /><span :style="{ height: `${bar.inbound}px` }" /><label>{{ chartLabels[index] }}</label></div>
            <div v-if="!movements.length && !loading" class="chart-empty">Belum ada transaksi pada periode ini</div>
          </div>
        </div>
        <div class="chart-legend"><span class="legend-item"><i class="legend-dot strong" /> Barang masuk</span><span class="legend-item"><i class="legend-dot" /> Barang keluar</span></div>
      </section>

      <section class="surface-card">
        <div class="surface-card-head"><div><h2>Kesehatan stok</h2><p>Distribusi kondisi produk aktif</p></div><button class="ghost-button" type="button" @click="go('/products')">Semua produk ↗</button></div>
        <div class="health-body">
          <div class="health-ring" :style="{ background: healthGradient }"><div class="health-ring-copy"><strong>{{ healthPercent }}%</strong><span>aman</span></div></div>
          <div class="health-legend">
            <div class="health-row"><span class="health-row-label"><i /> Stok aman</span><strong>{{ healthyStock }}</strong></div>
            <div class="health-row"><span class="health-row-label"><i class="amber" /> Stok menipis</span><strong>{{ lowStock }}</strong></div>
            <div class="health-row"><span class="health-row-label"><i class="red" /> Stok habis</span><strong>{{ outOfStock }}</strong></div>
            <div class="health-note">Total <strong>{{ totalUnits }} unit</strong> tercatat di seluruh produk aktif.</div>
          </div>
        </div>
      </section>
    </div>

    <div class="two-column">
      <section class="surface-card">
        <div class="surface-card-head"><div><h2>Perlu perhatian</h2><p>Produk yang sudah menyentuh batas minimum stok</p></div><button class="ghost-button" type="button" @click="go('/products')">Kelola stok ↗</button></div>
        <div v-if="loading" class="empty">Memuat data produk…</div>
        <div v-else-if="!data.attention.length" class="empty"><strong>Semua stok terlihat aman</strong>Tidak ada produk yang perlu direstock saat ini.</div>
        <div v-else class="table-wrap"><table><thead><tr><th>Produk</th><th>Stok tersisa</th><th>Status</th></tr></thead><tbody><tr v-for="product in data.attention.slice(0, 5)" :key="product.id"><td><div class="product-cell"><span class="product-avatar amber">{{ shortName(product.name) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.sku }} · {{ product.category }}</small></span></div></td><td><span class="stock-value" :class="product.stockOnHand <= 0 ? 'out' : 'low'">{{ product.stockOnHand }} {{ product.unit }}</span><small>Min. {{ product.reorderLevel }}</small></td><td><span class="badge" :class="product.stockOnHand <= 0 ? 'danger' : 'warn'">{{ product.stockOnHand <= 0 ? 'Habis' : 'Menipis' }}</span></td></tr></tbody></table></div>
      </section>

      <section class="surface-card">
        <div class="surface-card-head"><div><h2>Aktivitas terbaru</h2><p>Perubahan stok paling baru di workspace</p></div><button class="ghost-button" type="button" @click="go('/inventory/movements')">Lihat semua ↗</button></div>
        <div v-if="loading" class="empty">Memuat aktivitas…</div>
        <div v-else-if="!latestMovements.length" class="empty"><strong>Belum ada aktivitas</strong>Aktivitas stok akan muncul di sini.</div>
        <div v-else class="activity-list"><div v-for="movement in latestMovements" :key="movement.id" class="activity-item"><span class="activity-icon" :class="{ out: movement.type === 'Sale' || movement.type === 'AdjustmentOut' }">{{ movement.type === 'Sale' || movement.type === 'AdjustmentOut' ? '↑' : '↓' }}</span><div class="activity-copy"><strong>{{ movement.productName }}</strong><p>{{ movement.type === 'Sale' ? 'Penjualan' : movement.type === 'GoodsReceipt' ? 'Barang masuk' : 'Penyesuaian' }} · {{ movement.quantity }} {{ movement.unit }}</p></div><span class="activity-time">{{ date(movement.createdAt) }}</span></div></div>
      </section>
    </div>
  </div>
</template>
