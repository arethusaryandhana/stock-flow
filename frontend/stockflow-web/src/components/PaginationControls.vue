<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from '../i18n'

const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}>()

const emit = defineEmits<{
  pageChange: [page: number]
  pageSizeChange: [pageSize: number]
}>()
const { t } = useI18n()
const pageSizeOptions = [5, 10, 25, 50, 100]

const pageNumbers = computed(() => {
  const totalPages = Math.max(props.totalPages, 1)
  const windowSize = 5
  let start = Math.max(1, props.page - 2)
  const end = Math.min(totalPages, start + windowSize - 1)
  start = Math.max(1, end - windowSize + 1)
  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
})

const firstItem = computed(() => props.totalCount === 0 ? 0 : (props.page - 1) * props.pageSize + 1)
const lastItem = computed(() => Math.min(props.page * props.pageSize, props.totalCount))
const goTo = (page: number) => {
  if (page >= 1 && page <= props.totalPages && page !== props.page) emit('pageChange', page)
}
const changePageSize = (event: Event) => {
  const nextPageSize = Number((event.target as HTMLSelectElement).value)
  if (pageSizeOptions.includes(nextPageSize) && nextPageSize !== props.pageSize) emit('pageSizeChange', nextPageSize)
}
</script>

<template>
  <div v-if="totalCount > 0" class="pagination" :aria-label="t('common.paginationAria')">
    <p>{{ t('common.paginationShowing', { from: firstItem, to: lastItem, total: totalCount }) }}</p>
    <div class="pagination-actions">
      <label class="pagination-size"><span>{{ t('common.paginationPageSize') }}</span><select :value="pageSize" :aria-label="t('common.paginationPageSize')" @change="changePageSize"><option v-for="option in pageSizeOptions" :key="option" :value="option">{{ option }}</option></select></label>
      <div class="pagination-controls">
      <button type="button" :aria-label="t('common.paginationPrevious')" :disabled="page <= 1" @click="goTo(page - 1)">‹</button>
      <button v-for="number in pageNumbers" :key="number" type="button" :class="{ active: number === page }" :aria-current="number === page ? 'page' : undefined" :aria-label="t('common.paginationPage', { page: number })" @click="goTo(number)">{{ number }}</button>
      <button type="button" :aria-label="t('common.paginationNext')" :disabled="page >= totalPages" @click="goTo(page + 1)">›</button>
      </div>
    </div>
  </div>
</template>
