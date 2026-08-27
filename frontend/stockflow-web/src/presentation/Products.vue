<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

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
const newProduct = ref({ sku: '', name: '', categoryId: '', purchasePrice: 0, sellingPrice: 0, reorderLevel: 0, unit: 'pcs' })

const money = (value: number) => new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()
const status = (product: Product) => {
  if (!product.isActive) return 'Tidak aktif'
  if (product.stockOnHand <= 0) return 'Habis'
  if (product.stockOnHand <= product.reorderLevel) return 'Menipis'
  return 'Aman'
}
const statusClass = (product: Product) => status(product) === 'Aman' ? 'ok' : status(product) === 'Menipis' ? 'warn' : status(product) === 'Habis' ? 'danger' : 'neutral'
const activeItems = computed(() => items.value.filter((item) => item.isActive))
const counts = computed(() => ({ all: items.value.length, healthy: activeItems.value.filter((item) => status(item) === 'Aman').length, low: activeItems.value.filter((item) => status(item) === 'Menipis').length, out: activeItems.value.filter((item) => status(item) === 'Habis').length, inactive: items.value.filter((item) => !item.isActive).length }))
const filtered = computed(() => {
  const term = q.value.toLowerCase().trim()
  return items.value.filter((item) => {
    const matchesTerm = `${item.sku} ${item.name} ${item.category}`.toLowerCase().includes(term)
    const matchesCategory = categoryFilter.value === 'all' || item.categoryId === categoryFilter.value
    const matchesStatus = statusFilter.value === 'all' || (statusFilter.value === 'inactive' ? !item.isActive : status(item) === statusFilter.value)
    return matchesTerm && matchesCategory && matchesStatus
  })
})

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [productsResponse, categoriesResponse] = await Promise.all([api.get<Product[]>('/products'), api.get<Category[]>('/categories')])
    items.value = productsResponse.data
    categories.value = categoriesResponse.data.filter((category) => category.isActive)
    if (!newProduct.value.categoryId && categories.value[0]) newProduct.value.categoryId = categories.value[0].id
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally { loading.value = false }
}

function openForm() { formError.value = ''; showForm.value = true }
function closeForm() { showForm.value = false; formError.value = ''; newProduct.value = { sku: '', name: '', categoryId: categories.value[0]?.id ?? '', purchasePrice: 0, sellingPrice: 0, reorderLevel: 0, unit: 'pcs' } }
async function createProduct() {
  formError.value = ''; saving.value = true
  try {
    const { data } = await api.post<Product>('/products', newProduct.value)
    items.value = [...items.value, data].sort((left, right) => left.name.localeCompare(right.name))
    closeForm()
  } catch (requestError) { formError.value = (requestError as Error).message } finally { saving.value = false }
}
async function toggleActive(product: Product) {
  openMenu.value = ''
  try { await api.patch(`/products/${product.id}/active`, !product.isActive); product.isActive = !product.isActive } catch (requestError) { error.value = (requestError as Error).message }
}
function exportCsv() {
  const rows = [['SKU', 'Produk', 'Kategori', 'Stok', 'Satuan', 'Harga jual', 'Status'], ...filtered.value.map((item) => [item.sku, item.name, item.category, String(item.stockOnHand), item.unit, String(item.sellingPrice), status(item)])]
  const csv = rows.map((row) => row.map((value) => `"${value.replaceAll('"', '""')}"`).join(',')).join('\n')
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' })); link.download = 'stockflow-produk.csv'; link.click(); URL.revokeObjectURL(link.href)
}
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">INVENTORY / CATALOG</p><h1>Produk & stok</h1><p class="subtitle">Kelola katalog, harga jual, dan ketersediaan barang dari satu tempat.</p></div>
      <div class="header-actions"><button class="secondary" type="button" @click="exportCsv">↓ Export CSV</button><button class="primary" type="button" @click="openForm"><span class="button-plus">+</span> Tambah produk</button></div>
    </div>

    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▦</span><span><small>Total produk</small><strong>{{ loading ? '—' : counts.all }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">✓</span><span><small>Stok aman</small><strong>{{ loading ? '—' : counts.healthy }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon out">△</span><span><small>Perlu perhatian</small><strong>{{ loading ? '—' : counts.low + counts.out }}</strong></span></div></div>

    <section class="surface-card page-panel">
      <div class="tab-row">
        <button class="tab-button" :class="{ active: statusFilter === 'all' }" type="button" @click="statusFilter = 'all'">Semua produk <span class="count">{{ counts.all }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'Menipis' }" type="button" @click="statusFilter = 'Menipis'">Menipis <span class="count">{{ counts.low }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'Habis' }" type="button" @click="statusFilter = 'Habis'">Habis <span class="count">{{ counts.out }}</span></button>
        <button class="tab-button" :class="{ active: statusFilter === 'inactive' }" type="button" @click="statusFilter = 'inactive'">Tidak aktif <span class="count">{{ counts.inactive }}</span></button>
      </div>
      <div class="toolbar">
        <label class="search-input"><span>⌕</span><input v-model="q" aria-label="Cari produk" placeholder="Cari nama produk atau SKU..."></label>
        <div class="toolbar-actions"><select v-model="categoryFilter" class="filter-select wide" aria-label="Filter kategori"><option value="all">Semua kategori</option><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select><button class="secondary" type="button" @click="exportCsv">Filter & export</button></div>
      </div>
      <div v-if="loading" class="empty">Memuat katalog produk…</div>
      <div v-else-if="!filtered.length" class="empty"><strong>Tidak ada produk yang cocok</strong>Coba ubah kata kunci atau filter yang dipilih.</div>
      <div v-else class="table-wrap"><table><thead><tr><th>Produk</th><th>Kategori</th><th>Stok tersedia</th><th>Harga jual</th><th>Status</th><th><span class="sr-only">Aksi</span></th></tr></thead><tbody><tr v-for="product in filtered" :key="product.id"><td><div class="product-cell"><span class="product-avatar">{{ shortName(product.name) }}</span><span><strong>{{ product.name }}</strong><small>{{ product.sku }}</small></span></div></td><td>{{ product.category }}</td><td><span class="stock-value" :class="{ low: status(product) === 'Menipis', out: status(product) === 'Habis' }">{{ product.stockOnHand }} {{ product.unit }}</span><small>Min. {{ product.reorderLevel }} {{ product.unit }}</small></td><td class="stock-value">{{ money(product.sellingPrice) }}</td><td><span class="badge" :class="statusClass(product)">{{ status(product) }}</span></td><td><div class="action-menu-wrap"><button class="action-button" type="button" aria-label="Buka menu produk" @click="openMenu = openMenu === product.id ? '' : product.id">•••</button><div v-if="openMenu === product.id" class="action-menu"><button type="button" @click="toggleActive(product)">{{ product.isActive ? 'Nonaktifkan' : 'Aktifkan' }}</button></div></div></td></tr></tbody></table></div>
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal" @submit.prevent="createProduct"><div class="modal-head"><div><p class="eyebrow">INVENTORY / CATALOG</p><h2>Tambah produk</h2><p>Masukkan detail inti produk untuk mulai melacak stok.</p></div><button class="close-button" type="button" aria-label="Tutup" @click="closeForm">×</button></div><div class="modal-body"><div class="form-grid"><label class="field-label">SKU<input v-model.trim="newProduct.sku" required maxlength="80" placeholder="Contoh: SKU-003"></label><label class="field-label">Nama produk<input v-model.trim="newProduct.name" required maxlength="160" placeholder="Nama yang mudah dicari"></label><label class="field-label">Kategori<select v-model="newProduct.categoryId" required><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select></label><label class="field-label">Satuan<input v-model.trim="newProduct.unit" required maxlength="24" placeholder="pcs"></label><label class="field-label">Harga beli<input v-model.number="newProduct.purchasePrice" type="number" min="0" step="1" required></label><label class="field-label">Harga jual<input v-model.number="newProduct.sellingPrice" type="number" min="0" step="1" required></label><label class="field-label full">Batas minimum stok<input v-model.number="newProduct.reorderLevel" type="number" min="0" step="0.01" required></label></div><p v-if="formError" class="alert" style="margin-top: 14px">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">Batal</button><button class="primary" :disabled="saving">{{ saving ? 'Menyimpan…' : 'Simpan produk' }}</button></div></div></form></div></Teleport>
  </div>
</template>
