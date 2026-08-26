<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

type Product = {
  id: string
  sku: string
  name: string
  categoryId: string
  category: string
  purchasePrice: number
  sellingPrice: number
  stockOnHand: number
  reorderLevel: number
  unit: string
  isActive: boolean
}

type Category = { id: string; name: string; isActive: boolean }

const items = ref<Product[]>([])
const categories = ref<Category[]>([])
const q = ref('')
const loading = ref(true)
const error = ref('')
const formError = ref('')
const saving = ref(false)
const showForm = ref(false)

const blankProduct = () => ({
  sku: '',
  name: '',
  categoryId: '',
  purchasePrice: 0,
  sellingPrice: 0,
  reorderLevel: 0,
  unit: 'pcs',
})
const newProduct = ref(blankProduct())

const filtered = computed(() => {
  const term = q.value.toLowerCase()
  return items.value.filter((item) =>
    `${item.sku} ${item.name} ${item.category}`.toLowerCase().includes(term),
  )
})

const money = (value: number) =>
  new Intl.NumberFormat('id-ID', {
    style: 'currency', currency: 'IDR', maximumFractionDigits: 0,
  }).format(value)

const status = (product: Product) => {
  if (!product.isActive) return 'Inactive'
  if (product.stockOnHand <= 0) return 'Out of Stock'
  if (product.stockOnHand <= product.reorderLevel) return 'Low Stock'
  return 'In Stock'
}

const statusClass = (product: Product) => status(product) === 'In Stock' ? 'ok' : 'danger'

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [productsResponse, categoriesResponse] = await Promise.all([
      api.get<Product[]>('/products'),
      api.get<Category[]>('/categories'),
    ])
    items.value = productsResponse.data
    categories.value = categoriesResponse.data.filter((category) => category.isActive)
    if (!newProduct.value.categoryId && categories.value[0]) {
      newProduct.value.categoryId = categories.value[0].id
    }
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function openForm() {
  formError.value = ''
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  formError.value = ''
  newProduct.value = { ...blankProduct(), categoryId: categories.value[0]?.id ?? '' }
}

async function createProduct() {
  formError.value = ''
  saving.value = true
  try {
    const { data } = await api.post<Product>('/products', newProduct.value)
    items.value = [...items.value, data].sort((left, right) => left.name.localeCompare(right.name))
    closeForm()
  } catch (requestError) {
    formError.value = (requestError as Error).message
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page">
    <div class="panel-head mb-8">
      <div>
        <p class="eyebrow">INVENTORY</p>
        <h1>Products</h1>
        <p class="subtitle">Your complete product catalog and stock position.</p>
      </div>
      <button class="primary" :disabled="!categories.length" @click="openForm">Add product</button>
    </div>

    <p v-if="error" class="alert mb-5">{{ error }}</p>
    <section class="panel">
      <div class="toolbar">
        <label class="sr-only" for="search">Search products</label>
        <input id="search" v-model="q" class="search" placeholder="Search by name, SKU, or category">
      </div>
      <div v-if="loading" class="empty">Loading products…</div>
      <div v-else-if="!filtered.length" class="empty">No products match your search.</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>Product</th><th>Category</th><th>Stock</th><th>Selling price</th><th>Status</th></tr></thead>
          <tbody>
            <tr v-for="product in filtered" :key="product.id">
              <td><strong>{{ product.name }}</strong><small>{{ product.sku }}</small></td>
              <td>{{ product.category }}</td>
              <td>{{ product.stockOnHand }} {{ product.unit }}</td>
              <td>{{ money(product.sellingPrice) }}</td>
              <td><span class="badge" :class="statusClass(product)">{{ status(product) }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <Teleport to="body">
      <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
        <form class="modal" @submit.prevent="createProduct">
          <div class="modal-head">
            <div><p class="eyebrow">INVENTORY</p><h2>Add product</h2></div>
            <button type="button" class="icon-button" aria-label="Close" @click="closeForm">×</button>
          </div>
          <div class="form-grid">
            <label>SKU<input v-model.trim="newProduct.sku" required maxlength="80" placeholder="e.g. SKU-003"></label>
            <label>Product name<input v-model.trim="newProduct.name" required maxlength="160"></label>
            <label>Category<select v-model="newProduct.categoryId" required><option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option></select></label>
            <label>Unit<input v-model.trim="newProduct.unit" required maxlength="24" placeholder="pcs"></label>
            <label>Purchase price<input v-model.number="newProduct.purchasePrice" type="number" min="0" step="1" required></label>
            <label>Selling price<input v-model.number="newProduct.sellingPrice" type="number" min="0" step="1" required></label>
            <label>Reorder level<input v-model.number="newProduct.reorderLevel" type="number" min="0" step="0.01" required></label>
          </div>
          <p v-if="formError" class="alert mt-4">{{ formError }}</p>
          <div class="modal-actions"><button type="button" class="secondary" @click="closeForm">Cancel</button><button class="primary" :disabled="saving">{{ saving ? 'Saving…' : 'Save product' }}</button></div>
        </form>
      </div>
    </Teleport>
  </div>
</template>
