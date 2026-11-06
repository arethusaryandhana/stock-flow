<script setup lang="ts">
import { ref, watch } from 'vue'
import { api, type PagedResponse } from '../infrastructure/api'
import { useDisplayPreferences } from '../preferences'
import { useI18n } from '../i18n'
import PaginationControls from './PaginationControls.vue'

export type Candidate = {
  id: string
  name: string
  sku: string
  unit: string
  purchasePrice: number
  stockOnHand: number
  threshold: number
  incomingQuantity: number
}

const props = defineProps<{ productId?: string }>()
const emit = defineEmits<{ draft: [items: Candidate[]] }>()
const { t } = useI18n()
const { formatNumber } = useDisplayPreferences()
const page = ref(1)
const pageSize = ref(10)
const result = ref<PagedResponse<Candidate>>()
const selected = ref(new Map<string, Candidate>())
const loading = ref(false)
const error = ref('')

watch([page, pageSize, () => props.productId], async (_, __, onCleanup) => {
  let current = true
  onCleanup(() => { current = false })
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.get<PagedResponse<Candidate>>('/purchase-orders/reorder-candidates', {
      params: { page: page.value, pageSize: pageSize.value, productId: props.productId },
    })
    if (current) result.value = data
  } catch (cause) {
    if (current) error.value = (cause as Error).message
  } finally {
    if (current) loading.value = false
  }
}, { immediate: true })

function toggle(item: Candidate) {
  if (selected.value.has(item.id)) selected.value.delete(item.id)
  else selected.value.set(item.id, item)
}

function resize(size: number) {
  page.value = 1
  pageSize.value = size
}
</script>

<template>
  <section class="surface-card page-panel">
    <div class="toolbar">
      <h2>{{ t('reorder.title') }}</h2>
      <button type="button" class="primary" :disabled="!selected.size || loading || !!error"
        @click="emit('draft', [...selected.values()])">{{ t('reorder.draft', { count: selected.size }) }}</button>
    </div>
    <p v-if="loading" class="empty" role="status">{{ t('reorder.loading') }}</p>
    <p v-else-if="error" class="alert" role="alert">{{ error }}</p>
    <p v-else-if="!result?.items.length" class="empty">{{ t('reorder.empty') }}</p>
    <template v-else>
      <div class="table-wrap"><table>
        <thead><tr>
          <th>{{ t('operations.product') }}</th><th>{{ t('reorder.stock') }}</th>
          <th>{{ t('products.reorderLevel') }}</th><th>{{ t('reorder.incoming') }}</th>
        </tr></thead>
        <tbody><tr v-for="item in result.items" :key="item.id">
          <td><label><input type="checkbox" :checked="selected.has(item.id)" @change="toggle(item)">
            {{ item.name }} · {{ item.sku }}</label></td>
          <td>{{ formatNumber(item.stockOnHand) }} {{ item.unit }}</td>
          <td>{{ formatNumber(item.threshold) }} {{ item.unit }}</td>
          <td>{{ formatNumber(item.incomingQuantity) }} {{ item.unit }}</td>
        </tr></tbody>
      </table></div>
      <PaginationControls :page="page" :page-size="pageSize" :total-count="result.totalCount"
        :total-pages="result.totalPages" @page-change="page = $event" @page-size-change="resize" />
    </template>
  </section>
</template>
