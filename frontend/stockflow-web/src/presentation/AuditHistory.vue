<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import PaginationControls from '../components/PaginationControls.vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useDisplayPreferences } from '../preferences'

type AuditLog = {
  id: string
  actorId: string | null
  actorName: string
  actorEmail: string | null
  action: string
  entityType: string
  entityId: string
  summary: string
  changes: string
  createdAt: string
}

type AuditValue = { Before?: unknown; After?: unknown; before?: unknown; after?: unknown }
type ParsedChange = { property: string; before: unknown; after: unknown }

const displayPreferences = useDisplayPreferences()
const logs = ref<AuditLog[]>([])
const search = ref('')
const entityType = ref('all')
const action = ref('all')
const page = ref(1)
const pageSize = ref<number>(displayPreferences.defaultPageSize.value)
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(true)
const error = ref('')
const expandedIds = ref(new Set<string>())
const { t } = useI18n()

const entityTypes = ['Product', 'Category', 'Supplier', 'Customer', 'PurchaseOrder', 'GoodsReceipt', 'SalesOrder', 'StockAdjustment']

function dateTime(value: string) {
  return displayPreferences.formatDate(value, { includeTime: true, includeSeconds: true })
}

const actionLabel = (value: string) => t(`audit.action${value}`)
const entityLabel = (value: string) => t(`audit.entity${value}`)
const propertyLabel = (value: string) => value.replace(/([a-z0-9])([A-Z])/g, '$1 $2')

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === '') return t('audit.noValue')
  if (typeof value === 'boolean') return value ? t('audit.yes') : t('audit.no')
  return String(value)
}

function changesFor(log: AuditLog): ParsedChange[] {
  try {
    const parsed = JSON.parse(log.changes) as Record<string, AuditValue>
    return Object.entries(parsed).map(([property, value]) => ({
      property,
      before: value.Before ?? value.before,
      after: value.After ?? value.after,
    }))
  } catch {
    return []
  }
}

function toggleDetails(id: string) {
  const next = new Set(expandedIds.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  expandedIds.value = next
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get<PagedResponse<AuditLog>>('/audit-logs', {
      params: {
        page: page.value,
        pageSize: pageSize.value,
        search: search.value.trim() || undefined,
        entityType: entityType.value === 'all' ? undefined : entityType.value,
        action: action.value === 'all' ? undefined : action.value,
      },
    })
    logs.value = data.items
    page.value = data.page
    totalCount.value = data.totalCount
    totalPages.value = data.totalPages
    expandedIds.value = new Set()
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function changePageSize(nextPageSize: number) {
  displayPreferences.setDefaultPageSize(nextPageSize)
  pageSize.value = nextPageSize
  page.value = 1
}

watch([search, entityType, action], () => {
  page.value = 1
  void load()
})
watch(page, (nextPage, previousPage) => {
  if (nextPage !== previousPage) void load()
})
watch(pageSize, () => void load())
onMounted(load)
</script>

<template>
  <div class="page">
    <div class="page-heading">
      <div><p class="eyebrow">{{ t('audit.eyebrow') }}</p><h1>{{ t('audit.title') }}</h1><p class="subtitle">{{ t('audit.subtitle') }}</p></div>
    </div>

    <p v-if="error" class="alert error-banner">{{ error }}</p>

    <section class="surface-card audit-note">
      <span class="audit-note-icon" aria-hidden="true">✓</span>
      <div><strong>{{ t('audit.immutableTitle') }}</strong><p>{{ t('audit.immutableDescription') }}</p></div>
    </section>

    <section class="surface-card page-panel">
      <div class="toolbar audit-toolbar">
        <label class="search-input"><span>⌕</span><input v-model="search" :aria-label="t('audit.searchAria')" :placeholder="t('audit.searchPlaceholder')"></label>
        <div class="toolbar-actions">
          <select v-model="entityType" class="filter-select wide" :aria-label="t('audit.entityFilterAria')"><option value="all">{{ t('audit.allEntities') }}</option><option v-for="item in entityTypes" :key="item" :value="item">{{ entityLabel(item) }}</option></select>
          <select v-model="action" class="filter-select wide" :aria-label="t('audit.actionFilterAria')"><option value="all">{{ t('audit.allActions') }}</option><option value="Created">{{ t('audit.actionCreated') }}</option><option value="Updated">{{ t('audit.actionUpdated') }}</option><option value="Deleted">{{ t('audit.actionDeleted') }}</option></select>
        </div>
      </div>

      <div class="section-note"><span class="status-dot" /> {{ t('audit.showing', { count: totalCount }) }}</div>
      <div v-if="loading" class="empty">{{ t('audit.loading') }}</div>
      <div v-else-if="!logs.length" class="empty"><strong>{{ t('audit.emptyTitle') }}</strong>{{ t('audit.emptyHint') }}</div>
      <div v-else class="table-wrap">
        <table class="audit-table">
          <thead><tr><th>{{ t('audit.time') }}</th><th>{{ t('audit.actor') }}</th><th>{{ t('audit.action') }}</th><th>{{ t('audit.entity') }}</th><th>{{ t('audit.record') }}</th><th>{{ t('audit.details') }}</th></tr></thead>
          <tbody>
            <template v-for="log in logs" :key="log.id">
              <tr>
                <td class="date-cell">{{ dateTime(log.createdAt) }}</td>
                <td><strong>{{ log.actorName }}</strong><small>{{ log.actorEmail || t('audit.systemActor') }}</small></td>
                <td><span class="badge" :class="log.action === 'Created' ? 'ok' : log.action === 'Deleted' ? 'danger' : 'warn'">{{ actionLabel(log.action) }}</span></td>
                <td><strong>{{ entityLabel(log.entityType) }}</strong><small class="audit-id">{{ log.entityId }}</small></td>
                <td class="audit-summary">{{ log.summary || '—' }}</td>
                <td><button class="audit-details-button" type="button" :aria-expanded="expandedIds.has(log.id)" @click="toggleDetails(log.id)">{{ expandedIds.has(log.id) ? t('audit.hideChanges') : t('audit.viewChanges') }}</button></td>
              </tr>
              <tr v-if="expandedIds.has(log.id)" class="audit-change-row">
                <td colspan="6">
                  <div v-if="changesFor(log).length" class="audit-changes">
                    <div class="audit-change-head"><span>{{ t('audit.field') }}</span><span>{{ t('audit.before') }}</span><span>{{ t('audit.after') }}</span></div>
                    <div v-for="change in changesFor(log)" :key="change.property" class="audit-change-item"><strong>{{ propertyLabel(change.property) }}</strong><code>{{ formatValue(change.before) }}</code><code>{{ formatValue(change.after) }}</code></div>
                  </div>
                  <p v-else class="audit-no-changes">{{ t('audit.noChanges') }}</p>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
      <PaginationControls v-if="!loading && logs.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>
  </div>
</template>

<style scoped>
.audit-note { display: flex; align-items: center; gap: 13px; padding: 16px 18px; margin-bottom: 18px; }
.audit-note-icon { display: grid; width: 38px; height: 38px; flex: 0 0 auto; place-items: center; border-radius: 11px; color: #13816e; background: var(--teal-soft); font-weight: 900; }
.audit-note strong { color: var(--text); font-size: .73rem; }
.audit-note p { margin: 3px 0 0; color: var(--muted); font-size: .65rem; line-height: 1.45; }
.audit-toolbar .search-input { max-width: 420px; }
.audit-table td { vertical-align: middle; }
.audit-table td small { display: block; margin-top: 4px; }
.audit-id { max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.audit-summary { min-width: 150px; max-width: 260px; }
.audit-details-button { padding: 7px 9px; border: 1px solid var(--line); border-radius: 8px; color: var(--blue); background: var(--surface-raised); font-size: .61rem; font-weight: 800; white-space: nowrap; }
.audit-details-button:hover { border-color: #c9d7f5; background: var(--blue-soft); }
.audit-change-row td { padding: 0 15px 15px; background: color-mix(in srgb, var(--surface) 86%, var(--line)); }
.audit-changes { overflow: hidden; border: 1px solid var(--line); border-radius: 10px; background: var(--surface-raised); }
.audit-change-head, .audit-change-item { display: grid; grid-template-columns: minmax(130px, .8fr) minmax(180px, 1fr) minmax(180px, 1fr); gap: 12px; padding: 9px 12px; }
.audit-change-head { color: var(--muted); background: color-mix(in srgb, var(--surface) 88%, var(--line)); font-size: .58rem; font-weight: 800; text-transform: uppercase; }
.audit-change-item { border-top: 1px solid var(--line); align-items: start; font-size: .63rem; }
.audit-change-item:first-of-type { border-top: 0; }
.audit-change-item strong { color: var(--text); }
.audit-change-item code { overflow-wrap: anywhere; color: var(--muted); font-family: inherit; white-space: normal; }
.audit-no-changes { margin: 0; padding: 13px; border-radius: 9px; color: var(--muted); background: var(--surface-raised); font-size: .65rem; }
@media (max-width: 700px) { .audit-change-head, .audit-change-item { grid-template-columns: 1fr; gap: 5px; } .audit-change-head { display: none; } }
</style>
