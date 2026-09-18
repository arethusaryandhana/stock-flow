<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useDisplayPreferences } from '../preferences'
import { useToastStore } from '../stores/toast'
import { useAuthStore } from '../stores/auth'
import PaginationControls from '../components/PaginationControls.vue'

type ReportJob = {
  id: string
  jobNumber: string
  reportType: string
  format: string
  status: string
  progress: number
  fileSize: number | null
  requestedAt: string
  startedAt: string | null
  completedAt: string | null
  errorMessage: string | null
}
type StatusCounts = { queued: number; processing: number; completed: number; failed: number }
type ReportPage = {
  items: ReportJob[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  statusCounts: StatusCounts
}

const displayPreferences = useDisplayPreferences()
const jobs = ref<ReportJob[]>([])
const page = ref(1)
const pageSize = ref<number>(displayPreferences.defaultPageSize.value)
const totalCount = ref(0)
const totalPages = ref(0)
const statusFilter = ref('all')
const statusCounts = ref<StatusCounts>({ queued: 0, processing: 0, completed: 0, failed: 0 })
const loading = ref(true)
const creating = ref(false)
const downloadingId = ref('')
const error = ref('')
const { t } = useI18n()
const toast = useToastStore()
const auth = useAuthStore()
const canExport = computed(() => auth.can('action.reports.export'))
let pollTimer: number | undefined

const hasActiveJobs = computed(() => statusCounts.value.queued + statusCounts.value.processing > 0)
const activeCount = computed(() => statusCounts.value.queued + statusCounts.value.processing)
const dateTime = (value: string) => displayPreferences.formatDate(value, { includeTime: true })
const statusLabel = (status: string) => t(`reports.${status.toLowerCase()}`)
const statusClass = (status: string) => status === 'Completed' ? 'ok' : status === 'Processing' || status === 'Queued' ? 'warn' : status === 'Failed' ? 'danger' : 'neutral'
const reportTypeLabel = (reportType: string) => reportType === 'product-stock' ? t('reports.productStock') : reportType
const fileSize = (bytes: number | null) => {
  if (bytes === null) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${displayPreferences.formatNumber(bytes / 1024, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} KB`
  return `${displayPreferences.formatNumber(bytes / (1024 * 1024), { minimumFractionDigits: 1, maximumFractionDigits: 1 })} MB`
}

async function load(showLoading = true) {
  if (showLoading) loading.value = true
  error.value = ''
  try {
    const { data } = await api.get<ReportPage>('/report-exports', {
      params: {
        page: page.value,
        pageSize: pageSize.value,
        status: statusFilter.value === 'all' ? undefined : statusFilter.value,
      },
    })
    jobs.value = data.items
    page.value = data.page
    totalCount.value = data.totalCount
    totalPages.value = data.totalPages
    statusCounts.value = data.statusCounts
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    if (showLoading) loading.value = false
  }
}

async function createReport() {
  if (!canExport.value) return
  creating.value = true
  try {
    const { data } = await api.post<ReportJob>('/report-exports', {
      reportType: 'product-stock',
      format: 'csv',
    })
    page.value = 1
    statusFilter.value = 'all'
    await load(false)
    toast.success(t('reports.createdToast', { number: data.jobNumber }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  } finally {
    creating.value = false
  }
}

async function download(job: ReportJob) {
  downloadingId.value = job.id
  try {
    const response = await api.get<Blob>(`/report-exports/${job.id}/download`, {
      responseType: 'blob',
    })
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a')
    link.href = url
    link.download = `stockflow-product-stock-${job.jobNumber}.csv`
    link.click()
    URL.revokeObjectURL(url)
    toast.success(t('reports.downloadedToast', { number: job.jobNumber }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  } finally {
    downloadingId.value = ''
  }
}

function changePageSize(nextPageSize: number) {
  displayPreferences.setDefaultPageSize(nextPageSize)
  pageSize.value = nextPageSize
  page.value = 1
}

watch(statusFilter, () => {
  page.value = 1
  void load()
})
watch(page, (nextPage, previousPage) => {
  if (nextPage !== previousPage) void load()
})
watch(pageSize, () => void load())

onMounted(async () => {
  await load()
  pollTimer = window.setInterval(() => {
    if (hasActiveJobs.value && document.visibilityState === 'visible') void load(false)
  }, 5000)
})

onBeforeUnmount(() => {
  if (pollTimer !== undefined) window.clearInterval(pollTimer)
})
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">{{ t('reports.eyebrow') }}</p><h1>{{ t('reports.title') }}</h1><p class="subtitle">{{ t('reports.subtitle') }}</p></div>
      <div v-if="canExport" class="header-actions"><button class="primary" type="button" :disabled="creating || hasActiveJobs" @click="createReport"><span class="button-plus">+</span> {{ creating ? t('reports.creating') : t('reports.create') }}</button></div>
    </div>
    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <div class="summary-grid"><div class="mini-stat"><span class="mini-stat-icon">▤</span><span><small>{{ t('reports.total') }}</small><strong>{{ loading ? '—' : totalCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon amber">◷</span><span><small>{{ t('reports.active') }}</small><strong>{{ loading ? '—' : activeCount }}</strong></span></div><div class="mini-stat"><span class="mini-stat-icon in">↓</span><span><small>{{ t('reports.ready') }}</small><strong>{{ loading ? '—' : statusCounts.completed }}</strong></span></div></div>

    <section class="surface-card report-explanation"><span class="report-explanation-icon" aria-hidden="true">CSV</span><div><strong>{{ t('reports.contentsTitle') }}</strong><p>{{ t('reports.contentsDescription') }}</p></div></section>

    <section class="surface-card page-panel">
      <div class="tab-row"><button class="tab-button" :class="{ active: statusFilter === 'all' }" type="button" @click="statusFilter = 'all'">{{ t('reports.allStatuses') }}</button><button class="tab-button" :class="{ active: statusFilter === 'Queued' }" type="button" @click="statusFilter = 'Queued'">{{ t('reports.queued') }} <span class="count">{{ statusCounts.queued }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Processing' }" type="button" @click="statusFilter = 'Processing'">{{ t('reports.processing') }} <span class="count">{{ statusCounts.processing }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Completed' }" type="button" @click="statusFilter = 'Completed'">{{ t('reports.completed') }} <span class="count">{{ statusCounts.completed }}</span></button><button class="tab-button" :class="{ active: statusFilter === 'Failed' }" type="button" @click="statusFilter = 'Failed'">{{ t('reports.failed') }} <span class="count">{{ statusCounts.failed }}</span></button></div>
      <div class="toolbar report-toolbar"><select v-model="statusFilter" class="filter-select wide" :aria-label="t('reports.statusFilterAria')"><option value="all">{{ t('reports.allStatuses') }}</option><option value="Queued">{{ t('reports.queued') }}</option><option value="Processing">{{ t('reports.processing') }}</option><option value="Completed">{{ t('reports.completed') }}</option><option value="Failed">{{ t('reports.failed') }}</option></select></div>
      <div v-if="loading" class="empty">{{ t('reports.loading') }}</div>
      <div v-else-if="!jobs.length" class="empty"><strong>{{ t('reports.emptyTitle') }}</strong>{{ t('reports.emptyHint') }}</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>{{ t('reports.requestedAt') }}</th><th>{{ t('reports.number') }}</th><th>{{ t('reports.type') }}</th><th>{{ t('reports.status') }}</th><th>{{ t('reports.progress') }}</th><th>{{ t('reports.fileSize') }}</th><th>{{ t('reports.actions') }}</th></tr></thead>
          <tbody><tr v-for="job in jobs" :key="job.id"><td class="date-cell">{{ dateTime(job.requestedAt) }}<small v-if="job.completedAt">{{ t('reports.completed') }}: {{ dateTime(job.completedAt) }}</small></td><td class="stock-value">{{ job.jobNumber }}</td><td><strong>{{ reportTypeLabel(job.reportType) }}</strong><small>{{ job.format.toUpperCase() }}</small></td><td><span class="badge" :class="statusClass(job.status)">{{ statusLabel(job.status) }}</span><small v-if="job.errorMessage" class="report-error">{{ job.errorMessage }}</small></td><td><div class="report-progress"><span><i :style="{ width: `${job.progress}%` }" /></span><small>{{ job.progress }}%</small></div></td><td>{{ fileSize(job.fileSize) }}</td><td><button v-if="job.status === 'Completed'" class="purchase-order-action primary-action" type="button" :disabled="downloadingId === job.id" @click="download(job)">{{ downloadingId === job.id ? t('reports.downloading') : t('reports.download') }}</button><span v-else>—</span></td></tr></tbody>
        </table>
      </div>
      <PaginationControls v-if="!loading && jobs.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>
  </div>
</template>

<style scoped>
.report-explanation { display: flex; align-items: center; gap: 14px; padding: 18px 20px; margin-bottom: 18px; }
.report-explanation-icon { display: grid; place-items: center; width: 48px; height: 48px; flex: 0 0 auto; border-radius: 14px; background: var(--blue-soft); color: var(--blue); font-size: 12px; font-weight: 800; letter-spacing: .08em; }
.report-explanation p { margin: 4px 0 0; color: var(--muted); line-height: 1.5; }
.report-toolbar { justify-content: flex-end; }
.report-error { max-width: 280px; margin-top: 5px; color: var(--red); white-space: normal; }
.report-progress { display: flex; align-items: center; gap: 8px; min-width: 130px; }
.report-progress > span { width: 88px; height: 7px; overflow: hidden; border-radius: 999px; background: var(--line); }
.report-progress i { display: block; height: 100%; border-radius: inherit; background: var(--blue); transition: width .25s ease; }
.report-progress small { min-width: 32px; }
@media (max-width: 700px) { .report-explanation { align-items: flex-start; } }
</style>
