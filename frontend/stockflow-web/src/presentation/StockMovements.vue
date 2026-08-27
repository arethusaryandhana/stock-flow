<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

type Movement = { id: string; productId: string; productSku: string; productName: string; unit: string; type: string; quantity: number; balanceAfter: number; referenceNumber: string; reason: string | null; createdAt: string }
const movements = ref<Movement[]>([])
const q = ref('')
const typeFilter = ref('all')
const period = ref('30')
const loading = ref(true)
const error = ref('')
const labels: Record<string, string> = { GoodsReceipt: 'Barang masuk', Sale: 'Penjualan', AdjustmentIn: 'Penyesuaian masuk', AdjustmentOut: 'Penyesuaian keluar' }
const isInbound = (movement: Movement) => movement.type === 'GoodsReceipt' || movement.type === 'AdjustmentIn'
const label = (movement: Movement) => labels[movement.type] ?? movement.type
const quantity = (movement: Movement) => `${isInbound(movement) ? '+' : '−'}${movement.quantity} ${movement.unit}`
const date = (value: string) => new Intl.DateTimeFormat('id-ID', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
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
  const rows = [['Tanggal', 'Produk', 'SKU', 'Tipe', 'Jumlah', 'Saldo setelah', 'Referensi', 'Keterangan'], ...filtered.value.map((item) => [date(item.createdAt), item.productName, item.productSku, label(item), String(item.quantity), String(item.balanceAfter), item.referenceNumber, item.reason ?? ''])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-pergerakan.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">INVENTORY / AUDIT TRAIL</p><h1>Pergerakan stok</h1><p class="subtitle">Satu jejak kronologis untuk setiap barang masuk, keluar, dan koreksi.</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">↓ Export CSV</button></div>
    </div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">◷</span><span><small>Aktivitas hari ini</small><strong>{{ loading ? '—' : todayCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">↓</span><span><small>Total barang masuk</small><strong>{{ loading ? '—' : received }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">↑</span><span><small>Total barang keluar</small><strong>{{ loading ? '—' : shipped }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="toolbar"><label class="search-input"><span>⌕</span><input v-model="q" aria-label="Cari pergerakan" placeholder="Cari produk, referensi, atau keterangan..."></label><div class="toolbar-actions"><select v-model="typeFilter" class="filter-select wide" aria-label="Filter tipe pergerakan"><option value="all">Semua tipe</option><option value="GoodsReceipt">Barang masuk</option><option value="Sale">Penjualan</option><option value="AdjustmentIn">Penyesuaian masuk</option><option value="AdjustmentOut">Penyesuaian keluar</option></select><select v-model="period" class="filter-select" aria-label="Pilih periode"><option value="7">7 hari terakhir</option><option value="30">30 hari terakhir</option><option value="90">90 hari terakhir</option></select></div></div>
      <div class="section-note" style="padding: 12px 18px 0"><span class="status-dot" /> Menampilkan aktivitas terbaru · {{ filtered.length }} catatan</div>
      <div v-if="loading" class="empty">Memuat riwayat pergerakan…</div>
      <div v-else-if="!filtered.length" class="empty"><strong>Belum ada pergerakan stok</strong>Aktivitas akan tercatat otomatis saat stok berubah.</div>
      <div v-else class="table-wrap"><table><thead><tr><th>Tanggal</th><th>Produk</th><th>Tipe aktivitas</th><th>Perubahan</th><th>Saldo akhir</th><th>Referensi</th><th>Keterangan</th></tr></thead><tbody><tr v-for="movement in filtered" :key="movement.id"><td class="date-cell">{{ date(movement.createdAt) }}</td><td><div class="product-cell"><span class="product-avatar" :class="{ teal: isInbound(movement) }">{{ shortName(movement.productName) }}</span><span><strong>{{ movement.productName }}</strong><small>{{ movement.productSku }}</small></span></div></td><td><span class="badge" :class="isInbound(movement) ? 'ok' : 'danger'">{{ label(movement) }}</span></td><td :class="isInbound(movement) ? 'quantity-in' : 'quantity-out'">{{ quantity(movement) }}</td><td class="stock-value">{{ movement.balanceAfter }} {{ movement.unit }}</td><td class="muted-cell">{{ movement.referenceNumber }}</td><td class="muted-cell">{{ movement.reason || '—' }}</td></tr></tbody></table></div>
    </section>
  </div>
</template>
