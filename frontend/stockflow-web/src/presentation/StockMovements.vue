<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'

type Movement = {
  id: string
  productId: string
  productSku: string
  productName: string
  unit: string
  type: string
  quantity: number
  balanceAfter: number
  referenceNumber: string
  reason: string | null
  createdAt: string
}

const movements = ref<Movement[]>([])
const q = ref('')
const loading = ref(true)
const error = ref('')

const labels: Record<string, string> = {
  GoodsReceipt: 'Goods receipt',
  Sale: 'Sale',
  AdjustmentIn: 'Adjustment in',
  AdjustmentOut: 'Adjustment out',
}

const filtered = computed(() => {
  const term = q.value.toLowerCase()
  return movements.value.filter((movement) =>
    `${movement.productSku} ${movement.productName} ${movement.referenceNumber} ${movement.reason ?? ''}`
      .toLowerCase()
      .includes(term),
  )
})

const isInbound = (movement: Movement) =>
  movement.type === 'GoodsReceipt' || movement.type === 'AdjustmentIn'

const quantity = (movement: Movement) =>
  `${isInbound(movement) ? '+' : '−'}${movement.quantity} ${movement.unit}`
const label = (movement: Movement) => labels[movement.type] ?? movement.type
const date = (value: string) =>
  new Intl.DateTimeFormat('id-ID', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

async function load() {
  loading.value = true
  error.value = ''
  try {
    movements.value = (await api.get<Movement[]>('/stock-movements')).data
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page">
    <div class="mb-8">
      <p class="eyebrow">INVENTORY</p>
      <h1>Stock Movement</h1>
      <p class="subtitle">A chronological ledger of every inventory change.</p>
    </div>

    <p v-if="error" class="alert mb-5">{{ error }}</p>
    <section class="panel">
      <div class="toolbar">
        <label class="sr-only" for="search-movements">Search movements</label>
        <input id="search-movements" v-model="q" class="search" placeholder="Search product, reference, or reason">
      </div>
      <div v-if="loading" class="empty">Loading stock movements…</div>
      <div v-else-if="!filtered.length" class="empty">No stock movements recorded yet.</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>Date</th><th>Product</th><th>Type</th><th>Quantity</th><th>Balance after</th><th>Reference</th><th>Reason</th></tr></thead>
          <tbody>
            <tr v-for="movement in filtered" :key="movement.id">
              <td>{{ date(movement.createdAt) }}</td>
              <td><strong>{{ movement.productName }}</strong><small>{{ movement.productSku }}</small></td>
              <td><span class="badge" :class="isInbound(movement) ? 'ok' : 'danger'">{{ label(movement) }}</span></td>
              <td :class="isInbound(movement) ? 'quantity-in' : 'quantity-out'">{{ quantity(movement) }}</td>
              <td>{{ movement.balanceAfter }} {{ movement.unit }}</td>
              <td>{{ movement.referenceNumber }}</td>
              <td>{{ movement.reason || '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
