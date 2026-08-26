<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

type Product = {
  id: string
  sku: string
  name: string
  stockOnHand: number
  unit: string
  isActive: boolean
}

type Adjustment = {
  id: string
  number: string
  productId: string
  productSku: string
  productName: string
  unit: string
  quantityDelta: number
  reason: string
  createdAt: string
}

const products = ref<Product[]>([])
const adjustments = ref<Adjustment[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const form = ref({ productId: '', quantityDelta: 0, reason: '' })

const activeProducts = computed(() => products.value.filter((product) => product.isActive))
const selectedProduct = computed(() =>
  products.value.find((product) => product.id === form.value.productId),
)
const date = (value: string) =>
  new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
const delta = (adjustment: Adjustment) =>
  `${adjustment.quantityDelta > 0 ? '+' : '−'}${Math.abs(adjustment.quantityDelta)} ${adjustment.unit}`

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [productsResponse, adjustmentsResponse] = await Promise.all([
      api.get<Product[]>('/products'),
      api.get<Adjustment[]>('/stock-adjustments'),
    ])
    products.value = productsResponse.data
    adjustments.value = adjustmentsResponse.data
    if (!form.value.productId && activeProducts.value[0]) {
      form.value.productId = activeProducts.value[0].id
    }
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

async function submit() {
  formError.value = ''
  saving.value = true
  try {
    const { data } = await api.post<Adjustment>('/stock-adjustments', form.value)
    adjustments.value.unshift(data)
    const product = products.value.find((item) => item.id === data.productId)
    if (product) product.stockOnHand += data.quantityDelta
    form.value.quantityDelta = 0
    form.value.reason = ''
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
    <div class="mb-8">
      <p class="eyebrow">INVENTORY</p>
      <h1>Stock Adjustment</h1>
      <p class="subtitle">Correct stock counts while preserving a clear audit trail.</p>
    </div>

    <p v-if="error" class="alert mb-5">{{ error }}</p>
    <section class="panel mb-8">
      <div class="panel-head"><div><h2>New adjustment</h2><p>Use a positive number to add stock and a negative number to remove it.</p></div></div>
      <form class="adjustment-form" @submit.prevent="submit">
        <label>Product
          <select v-model="form.productId" required :disabled="loading || !activeProducts.length">
            <option v-for="product in activeProducts" :key="product.id" :value="product.id">{{ product.name }} ({{ product.sku }}) — {{ product.stockOnHand }} {{ product.unit }}</option>
          </select>
        </label>
        <label>Quantity change<input v-model.number="form.quantityDelta" type="number" step="0.01" required></label>
        <label class="reason-field">Reason<input v-model.trim="form.reason" required maxlength="300" placeholder="e.g. Cycle count correction"></label>
        <p v-if="selectedProduct" class="helper">Current balance: {{ selectedProduct.stockOnHand }} {{ selectedProduct.unit }}. New balance: {{ selectedProduct.stockOnHand + form.quantityDelta }} {{ selectedProduct.unit }}.</p>
        <p v-if="formError" class="alert adjustment-error">{{ formError }}</p>
        <button class="primary" :disabled="saving || loading || !activeProducts.length">{{ saving ? 'Saving…' : 'Save adjustment' }}</button>
      </form>
    </section>

    <section class="panel">
      <div class="panel-head"><div><h2>Adjustment history</h2><p>All completed manual stock corrections.</p></div><router-link class="secondary" to="/inventory/movements">View movements</router-link></div>
      <div v-if="loading" class="empty">Loading adjustments…</div>
      <div v-else-if="!adjustments.length" class="empty">No adjustments have been recorded yet.</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>Date</th><th>Number</th><th>Product</th><th>Change</th><th>Reason</th></tr></thead>
          <tbody>
            <tr v-for="adjustment in adjustments" :key="adjustment.id">
              <td>{{ date(adjustment.createdAt) }}</td>
              <td>{{ adjustment.number }}</td>
              <td><strong>{{ adjustment.productName }}</strong><small>{{ adjustment.productSku }}</small></td>
              <td :class="adjustment.quantityDelta > 0 ? 'quantity-in' : 'quantity-out'">{{ delta(adjustment) }}</td>
              <td>{{ adjustment.reason }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
