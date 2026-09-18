<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import PaginationControls from '../components/PaginationControls.vue'
import { api, type PagedResponse } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useDisplayPreferences } from '../preferences'

type AccessLog = {
  id: string
  actorName: string
  actorEmail: string | null
  action: 'Created' | 'Updated' | 'Deleted' | 'Granted' | 'Revoked'
  entityType: 'User' | 'Role' | 'RolePermission'
  summary: string
  changes: string
  createdAt: string
}
type AuditValue = { Before?: unknown; After?: unknown; before?: unknown; after?: unknown }

const { t } = useI18n()
const display = useDisplayPreferences()
const logs = ref<AccessLog[]>([])
const search = ref('')
const entityType = ref('all')
const action = ref('all')
const page = ref(1)
const pageSize = ref<number>(display.defaultPageSize.value)
const totalCount = ref(0)
const totalPages = ref(0)
const loading = ref(true)
const error = ref('')
const expandedId = ref('')

const entityLabels: Record<string, string> = {
  User: 'accessHistory.user', Role: 'accessHistory.role', RolePermission: 'accessHistory.permission',
}
const actionLabels: Record<string, string> = {
  Created: 'accessHistory.created', Updated: 'accessHistory.updated', Deleted: 'accessHistory.deleted',
  Granted: 'accessHistory.granted', Revoked: 'accessHistory.revoked',
}
const propertyLabels: Record<string, string> = {
  FullName: 'accessHistory.fullName', Email: 'Email', RoleId: 'accessHistory.roleId',
  IsActive: 'accessHistory.active', Name: 'accessHistory.roleName',
  PermissionCode: 'accessHistory.permissionCode', PasswordChanged: 'accessHistory.passwordChanged',
}

function details(log: AccessLog) {
  try {
    const changes = JSON.parse(log.changes) as Record<string, AuditValue>
    return Object.entries(changes).map(([name, value]) => ({
      name,
      before: value.Before ?? value.before,
      after: value.After ?? value.after,
    }))
  } catch {
    return []
  }
}

function valueLabel(value: unknown) {
  if (value === null || value === undefined || value === '') return t('accessHistory.noValue')
  if (value === true) return t('accessHistory.yes')
  if (value === false) return t('accessHistory.no')
  return String(value)
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get<PagedResponse<AccessLog>>('/access/history', {
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
    if (!logs.value.some((log) => log.id === expandedId.value)) expandedId.value = ''
  } catch (cause) {
    error.value = (cause as Error).message
  } finally {
    loading.value = false
  }
}

function changePageSize(next: number) {
  display.setDefaultPageSize(next)
  pageSize.value = next
  page.value = 1
}

watch([search, entityType, action], () => { page.value = 1; void load() })
watch(page, (next, previous) => { if (next !== previous) void load() })
watch(pageSize, () => void load())
onMounted(load)
</script>

<template>
  <div class="page access-history-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('app.accessManagement') }}</p>
        <h1>{{ t('app.accessHistory') }}</h1>
        <p class="subtitle">{{ t('accessHistory.description') }}</p>
      </div>
    </div>
    <p v-if="error" class="alert error-banner" role="alert">{{ error }}</p>
    <section class="surface-card access-history-note">{{ t('accessHistory.note') }}</section>
    <section class="surface-card page-panel access-history-panel">
      <div class="toolbar access-history-toolbar">
        <label class="search-input"><span>⌕</span><input v-model="search" :aria-label="t('accessHistory.search')" :placeholder="t('accessHistory.search')"></label>
        <div class="toolbar-actions">
          <select v-model="entityType" class="filter-select wide" :aria-label="t('accessHistory.allTypes')">
            <option value="all">{{ t('accessHistory.allTypes') }}</option>
            <option v-for="item in ['User', 'Role', 'RolePermission']" :key="item" :value="item">{{ t(entityLabels[item]) }}</option>
          </select>
          <select v-model="action" class="filter-select wide" :aria-label="t('accessHistory.allActions')">
            <option value="all">{{ t('accessHistory.allActions') }}</option>
            <option v-for="item in ['Created', 'Updated', 'Deleted', 'Granted', 'Revoked']" :key="item" :value="item">{{ t(actionLabels[item]) }}</option>
          </select>
        </div>
      </div>
      <div v-if="loading" class="empty">{{ t('accessHistory.loading') }}</div>
      <div v-else-if="!logs.length" class="empty">{{ t('accessHistory.empty') }}</div>
      <div v-else class="table-wrap">
        <table>
          <thead><tr><th>{{ t('accessHistory.time') }}</th><th>{{ t('accessHistory.actor') }}</th><th>{{ t('accessHistory.event') }}</th><th>{{ t('accessHistory.target') }}</th><th>{{ t('accessHistory.details') }}</th></tr></thead>
          <tbody>
            <template v-for="log in logs" :key="log.id">
              <tr>
                <td class="date-cell">{{ display.formatDate(log.createdAt, { includeTime: true, includeSeconds: true }) }}</td>
                <td><strong>{{ log.actorName }}</strong><small v-if="log.actorEmail">{{ log.actorEmail }}</small></td>
                <td><span class="badge neutral">{{ t(actionLabels[log.action]) }}</span> {{ t(entityLabels[log.entityType]) }}</td>
                <td>{{ log.summary }}</td>
                <td><button class="secondary compact-action" type="button" :aria-expanded="expandedId === log.id" @click="expandedId = expandedId === log.id ? '' : log.id">{{ t('accessHistory.details') }}</button></td>
              </tr>
              <tr v-if="expandedId === log.id" class="access-history-detail-row"><td colspan="5">
                <div v-for="item in details(log)" :key="item.name" class="access-history-change">
                  <strong>{{ propertyLabels[item.name]?.startsWith('accessHistory.') ? t(propertyLabels[item.name]) : propertyLabels[item.name] ?? item.name }}</strong>
                  <span>{{ t('accessHistory.before') }}: {{ valueLabel(item.before) }}</span>
                  <span>{{ t('accessHistory.after') }}: {{ valueLabel(item.after) }}</span>
                </div>
              </td></tr>
            </template>
          </tbody>
        </table>
      </div>
      <PaginationControls v-if="!loading && logs.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>
  </div>
</template>

<style scoped>
.access-history-page { display: grid; gap: 1.2rem; }
.access-history-note { padding: 1rem 1.4rem; color: var(--muted); }
.access-history-panel { padding: 1.25rem; }
.access-history-toolbar { margin-bottom: 1rem; }
.access-history-change { display: grid; grid-template-columns: minmax(8rem, 1fr) minmax(10rem, 2fr) minmax(10rem, 2fr); gap: .75rem; padding: .65rem; border-bottom: 1px solid var(--line); overflow-wrap: anywhere; }
.access-history-detail-row td { background: var(--surface-raised); }
@media (max-width: 700px) { .access-history-change { grid-template-columns: 1fr; gap: .2rem; } }
</style>
