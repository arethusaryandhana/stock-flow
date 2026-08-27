<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

type Product = { id: string; sku: string; name: string; stockOnHand: number; unit: string; isActive: boolean }
type Adjustment = { id: string; number: string; productId: string; productSku: string; productName: string; unit: string; quantityDelta: number; reason: string; createdAt: string }
const products = ref<Product[]>([])
const adjustments = ref<Adjustment[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const form = ref({ productId: '', quantityDelta: 0, reason: '' })
const historyQuery = ref('')
const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const selectedProduct = computed(() => products.value.find((product) => product.id === form.value.productId))
const newBalance = computed(() => (selectedProduct.value?.stockOnHand ?? 0) + form.value.quantityDelta)
const date = (value: string) => new Intl.DateTimeFormat('id-ID', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
const delta = (adjustment: Adjustment) => `${adjustment.quantityDelta > 0 ? '+' : '−'}${Math.abs(adjustment.quantityDelta)} ${adjustment.unit}`
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
function setDirection(direction: 'in' | 'out') { form.value.quantityDelta = Math.abs(form.value.quantityDelta) * (direction === 'in' ? 1 : -1) }
async function submit() {
  formError.value = ''
  if (newBalance.value < 0) { formError.value = 'Saldo akhir tidak boleh kurang dari 0.'; return }
  if (!form.value.quantityDelta) { formError.value = 'Masukkan jumlah perubahan stok terlebih dahulu.'; return }
  saving.value = true
  try {
    const { data } = await api.post<Adjustment>('/stock-adjustments', form.value)
    adjustments.value.unshift(data)
    const product = products.value.find((item) => item.id === data.productId)
    if (product) product.stockOnHand += data.quantityDelta
    form.value.quantityDelta = 0; form.value.reason = ''
  } catch (requestError) { formError.value = (requestError as Error).message } finally { saving.value = false }
}
function exportCsv() {
  const rows = [['Tanggal', 'Nomor', 'Produk', 'SKU', 'Perubahan', 'Alasan'], ...filteredAdjustments.value.map((item) => [date(item.createdAt), item.number, item.productName, item.productSku, delta(item), item.reason])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-penyesuaian.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading"><div><p class="eyebrow">INVENTORY / CONTROL</p><h1>Penyesuaian stok</h1><p class="subtitle">Koreksi jumlah aktual dengan alasan yang jelas dan jejak audit yang rapi.</p></div><div class="header-actions"><button class="secondary" type="button" @click="exportCsv">↓ Export CSV</button></div></div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <section class="surface-card form-panel">
      <div class="surface-card-head"><div><h2>Buat penyesuaian baru</h2><p>Gunakan saat hasil stock opname berbeda dengan saldo di sistem.</p></div><span class="badge neutral">Audit trail aktif</span></div>
      <form class="adjustment-grid" @submit.prevent="submit">
        <label class="field-label">Produk<select v-model="form.productId" required :disabled="loading || !activeProducts.length"><option v-for="product in activeProducts" :key="product.id" :value="product.id">{{ product.name }} · {{ product.sku }}</option></select></label>
        <label class="field-label">Perubahan stok<div class="quantity-control"><button type="button" :class="{ selected: form.quantityDelta > 0, in: form.quantityDelta > 0 }" aria-label="Tambah stok" @click="setDirection('in')">+</button><input v-model.number="form.quantityDelta" type="number" step="0.01" required><button type="button" :class="{ selected: form.quantityDelta < 0, out: form.quantityDelta < 0 }" aria-label="Kurangi stok" @click="setDirection('out')">−</button></div></label>
        <label class="field-label">Alasan penyesuaian<input v-model.trim="form.reason" required maxlength="300" placeholder="Contoh: Hasil stock opname"></label>
        <p v-if="selectedProduct" class="adjustment-helper" :class="{ warning: newBalance < 0 }"><span>i</span> Saldo saat ini <strong>{{ selectedProduct.stockOnHand }} {{ selectedProduct.unit }}</strong> akan menjadi <strong>{{ newBalance }} {{ selectedProduct.unit }}</strong>.</p>
        <p v-if="formError" class="alert" style="grid-column: 1 / -1; margin: 14px 19px 0">{{ formError }}</p>
        <div class="adjustment-actions" style="grid-column: 1 / -1"><button class="secondary" type="button" @click="form.quantityDelta = 0; form.reason = ''">Reset</button><button class="primary" :disabled="saving || loading || !activeProducts.length">{{ saving ? 'Menyimpan…' : 'Simpan penyesuaian' }}</button></div>
      </form>
    </section>

    <section class="surface-card page-panel" style="margin-top: 14px">
      <div class="history-head"><div><h2>Riwayat penyesuaian</h2><p>Semua koreksi stok manual yang sudah tercatat.</p></div><button class="ghost-button" type="button" @click="exportCsv">Export riwayat ↗</button></div>
      <div class="history-filter"><label class="search-input"><span>⌕</span><input v-model="historyQuery" aria-label="Cari riwayat penyesuaian" placeholder="Cari nomor, produk, atau alasan..."></label></div>
      <div v-if="loading" class="empty">Memuat riwayat penyesuaian…</div>
      <div v-else-if="!filteredAdjustments.length" class="empty"><strong>Belum ada penyesuaian</strong>Koreksi yang disimpan akan muncul di sini.</div>
      <div v-else class="table-wrap"><table><thead><tr><th>Tanggal</th><th>Nomor</th><th>Produk</th><th>Perubahan</th><th>Alasan</th></tr></thead><tbody><tr v-for="adjustment in filteredAdjustments" :key="adjustment.id"><td class="date-cell">{{ date(adjustment.createdAt) }}</td><td class="muted-cell">{{ adjustment.number }}</td><td><div class="product-cell"><span class="product-avatar amber">{{ adjustment.productName.slice(0, 2).toUpperCase() }}</span><span><strong>{{ adjustment.productName }}</strong><small>{{ adjustment.productSku }}</small></span></div></td><td :class="adjustment.quantityDelta > 0 ? 'quantity-in' : 'quantity-out'">{{ delta(adjustment) }}</td><td>{{ adjustment.reason }}</td></tr></tbody></table></div>
    </section>
  </div>
</template>
