<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import type { PagedResponse } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'
import PaginationControls from '../components/PaginationControls.vue'

type EntityType = 'categories' | 'products' | 'suppliers' | 'customers'
type Category = { id: string; name: string; description?: string | null; isActive: boolean }
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
type Partner = { id: string; code: string; name: string; email?: string | null; phone?: string | null; address?: string | null; isActive: boolean }
type MasterItem = Category | Product | Partner

const props = defineProps<{ entity: EntityType }>()
const auth = useAuthStore()
const toast = useToastStore()
const { locale, t } = useI18n()
const items = ref<MasterItem[]>([])
const categories = ref<Category[]>([])
const query = ref('')
const loading = ref(true)
const error = ref('')
const formError = ref('')
const saving = ref(false)
const showForm = ref(false)
const editingId = ref<string | null>(null)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = ref(0)

const emptyForm = () => ({
  code: '',
  name: '',
  description: '',
  email: '',
  phone: '',
  address: '',
  sku: '',
  categoryId: categories.value[0]?.id ?? '',
  purchasePrice: 0,
  sellingPrice: 0,
  reorderLevel: 0,
  unit: 'pcs',
})
const form = reactive(emptyForm())

const entityKey = computed(() => props.entity === 'categories' ? 'master.categoryEntity' : props.entity === 'products' ? 'master.productEntity' : props.entity === 'suppliers' ? 'master.supplierEntity' : 'master.customerEntity')
const entityLabel = computed(() => t(entityKey.value))
const endpoint = computed(() => `/${props.entity}`)
const canManage = computed(() => auth.isAdmin)
const filtered = computed(() => items.value)
const categoryItems = computed(() => filtered.value as Category[])
const productItems = computed(() => filtered.value as Product[])
const partnerItems = computed(() => filtered.value as Partner[])
const categoryOptions = computed(() => categories.value.filter((category) => category.isActive || category.id === form.categoryId))

const money = (value: number) => new Intl.NumberFormat(locale.value, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }).format(value)
const shortName = (value: string) => value.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase()

async function load() {
  if (!canManage.value) {
    loading.value = false
    return
  }

  loading.value = true
  error.value = ''
  try {
    const response = await api.get<PagedResponse<MasterItem>>(endpoint.value, { params: { page: page.value, pageSize: pageSize.value, search: query.value.trim() || undefined } })
    items.value = response.data.items
    page.value = response.data.page
    totalCount.value = response.data.totalCount
    totalPages.value = response.data.totalPages
    if (props.entity === 'products') {
      const categoryResponse = await api.get<PagedResponse<Category>>('/categories', { params: { page: 1, pageSize: 100 } })
      categories.value = categoryResponse.data.items
      if (!form.categoryId && categories.value[0]) form.categoryId = categories.value[0].id
    }
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function resetForm() {
  Object.assign(form, emptyForm())
  formError.value = ''
}

function changePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize
  page.value = 1
}

function openCreate() {
  if (!canManage.value) return
  editingId.value = null
  resetForm()
  showForm.value = true
}

function openEdit(item: MasterItem) {
  if (!canManage.value) return
  editingId.value = item.id
  formError.value = ''
  if (props.entity === 'categories') {
    const category = item as Category
    Object.assign(form, { ...emptyForm(), name: category.name, description: category.description ?? '' })
  } else if (props.entity === 'products') {
    const product = item as Product
    Object.assign(form, { ...emptyForm(), sku: product.sku, name: product.name, categoryId: product.categoryId, purchasePrice: product.purchasePrice, sellingPrice: product.sellingPrice, reorderLevel: product.reorderLevel, unit: product.unit })
  } else {
    const partner = item as Partner
    Object.assign(form, { ...emptyForm(), code: partner.code, name: partner.name, email: partner.email ?? '', phone: partner.phone ?? '', address: partner.address ?? '' })
  }
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingId.value = null
  resetForm()
}

function payload() {
  if (props.entity === 'categories') return { name: form.name, description: form.description || null }
  if (props.entity === 'products') return { sku: form.sku, name: form.name, categoryId: form.categoryId, purchasePrice: Number(form.purchasePrice), sellingPrice: Number(form.sellingPrice), reorderLevel: Number(form.reorderLevel), unit: form.unit }
  return { code: form.code, name: form.name, email: form.email || null, phone: form.phone || null, address: form.address || null }
}

async function save() {
  if (!canManage.value) return
  formError.value = ''
  saving.value = true
  const isEditing = Boolean(editingId.value)
  const itemName = form.name
  try {
    if (editingId.value) await api.put<MasterItem>(`${endpoint.value}/${editingId.value}`, payload())
    else await api.post<MasterItem>(endpoint.value, payload())
    closeForm()
    await load()
    toast.success(t(isEditing ? 'master.updatedToast' : 'master.createdToast', { entity: entityLabel.value, name: itemName }))
  } catch (requestError) {
    const message = (requestError as Error).message
    formError.value = message
    toast.error(message)
  } finally {
    saving.value = false
  }
}

async function toggleActive(item: MasterItem) {
  if (!canManage.value) return
  const willActivate = !item.isActive
  try {
    await api.patch(`${endpoint.value}/${item.id}/active`, willActivate)
    item.isActive = willActivate
    toast.success(t(willActivate ? 'master.activatedToast' : 'master.deactivatedToast', { entity: entityLabel.value, name: item.name }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  }
}

async function remove(item: MasterItem) {
  if (!canManage.value || !window.confirm(`${t('master.delete')} ${item.name}?`)) return
  try {
    await api.delete(`${endpoint.value}/${item.id}`)
    item.isActive = false
    toast.success(t('master.deletedToast', { entity: entityLabel.value, name: item.name }))
  } catch (requestError) {
    const message = (requestError as Error).message
    error.value = message
    toast.error(message)
  }
}

onMounted(load)
watch(query, () => {
  page.value = 1
  void load()
})
watch(page, (nextPage, previousPage) => {
  if (nextPage !== previousPage) void load()
})
watch(pageSize, () => void load())
watch(() => props.entity, () => {
  query.value = ''
  page.value = 1
  closeForm()
  void load()
})
</script>

<template>
  <div v-if="!canManage" class="page">
    <section class="surface-card empty access-denied"><strong>{{ t('master.accessDenied') }}</strong></section>
  </div>

  <div v-else class="page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('master.eyebrow') }}</p>
        <h1>{{ entityLabel }}</h1>
        <p class="subtitle">{{ t('master.subtitle') }}</p>
      </div>
      <button class="primary" type="button" @click="openCreate"><span class="button-plus">+</span> {{ t('master.add') }} {{ entityLabel }}</button>
    </div>

    <p v-if="error" class="alert error-banner">{{ error }}</p>
    <section class="surface-card page-panel master-panel">
      <div class="toolbar">
        <label class="search-input"><span>⌕</span><input v-model="query" :aria-label="t('master.search')" :placeholder="t('master.search')"></label>
      </div>
      <div v-if="loading" class="empty">{{ t('master.loading') }}</div>
      <div v-else-if="!filtered.length" class="empty">{{ t('master.empty') }}</div>

      <div v-else-if="props.entity === 'categories'" class="table-wrap"><table><thead><tr><th>{{ t('master.name') }}</th><th>{{ t('master.description') }}</th><th>{{ t('master.status') }}</th><th><span class="sr-only">{{ t('master.edit') }}</span></th></tr></thead><tbody><tr v-for="item in categoryItems" :key="item.id"><td><div class="product-cell"><span class="product-avatar">{{ shortName(item.name) }}</span><strong>{{ item.name }}</strong></div></td><td>{{ item.description || '—' }}</td><td><span class="badge" :class="item.isActive ? 'ok' : 'neutral'">{{ item.isActive ? t('master.active') : t('master.inactive') }}</span></td><td><div class="master-actions"><button type="button" @click="openEdit(item)">{{ t('master.edit') }}</button><button type="button" @click="toggleActive(item)">{{ item.isActive ? t('master.deactivate') : t('master.activate') }}</button><button type="button" @click="remove(item)">{{ t('master.delete') }}</button></div></td></tr></tbody></table></div>

      <div v-else-if="props.entity === 'products'" class="table-wrap"><table><thead><tr><th>{{ t('master.sku') }} / {{ t('master.name') }}</th><th>{{ t('master.category') }}</th><th>{{ t('master.sellingPrice') }}</th><th>{{ t('master.status') }}</th><th><span class="sr-only">{{ t('master.edit') }}</span></th></tr></thead><tbody><tr v-for="item in productItems" :key="item.id"><td><div class="product-cell"><span class="product-avatar">{{ shortName(item.name) }}</span><span><strong>{{ item.name }}</strong><small>{{ item.sku }}</small></span></div></td><td>{{ item.category }}</td><td class="stock-value">{{ money(item.sellingPrice) }}</td><td><span class="badge" :class="item.isActive ? 'ok' : 'neutral'">{{ item.isActive ? t('master.active') : t('master.inactive') }}</span></td><td><div class="master-actions"><button type="button" @click="openEdit(item)">{{ t('master.edit') }}</button><button type="button" @click="toggleActive(item)">{{ item.isActive ? t('master.deactivate') : t('master.activate') }}</button><button type="button" @click="remove(item)">{{ t('master.delete') }}</button></div></td></tr></tbody></table></div>

      <div v-else class="table-wrap"><table><thead><tr><th>{{ t('master.code') }}</th><th>{{ t('master.name') }}</th><th>{{ t('master.email') }}</th><th>{{ t('master.phone') }}</th><th>{{ t('master.status') }}</th><th><span class="sr-only">{{ t('master.edit') }}</span></th></tr></thead><tbody><tr v-for="item in partnerItems" :key="item.id"><td class="stock-value">{{ item.code }}</td><td><strong>{{ item.name }}</strong><small>{{ item.address || '—' }}</small></td><td>{{ item.email || '—' }}</td><td>{{ item.phone || '—' }}</td><td><span class="badge" :class="item.isActive ? 'ok' : 'neutral'">{{ item.isActive ? t('master.active') : t('master.inactive') }}</span></td><td><div class="master-actions"><button type="button" @click="openEdit(item)">{{ t('master.edit') }}</button><button type="button" @click="toggleActive(item)">{{ item.isActive ? t('master.deactivate') : t('master.activate') }}</button><button type="button" @click="remove(item)">{{ t('master.delete') }}</button></div></td></tr></tbody></table></div>

      <PaginationControls v-if="!loading && filtered.length" :page="page" :page-size="pageSize" :total-count="totalCount" :total-pages="totalPages" @page-change="page = $event" @page-size-change="changePageSize" />
    </section>

    <Teleport to="body"><div v-if="showForm" class="modal-backdrop" @click.self="closeForm"><form class="modal" @submit.prevent="save"><div class="modal-head"><div><p class="eyebrow">{{ t('master.eyebrow') }}</p><h2>{{ editingId ? t('master.editTitle', { entity: entityLabel }) : t('master.createTitle', { entity: entityLabel }) }}</h2></div><button class="close-button" type="button" :aria-label="t('common.close')" @click="closeForm">×</button></div><div class="modal-body">
      <div v-if="props.entity === 'categories'" class="form-grid"><label class="field-label">{{ t('master.name') }}<input v-model.trim="form.name" required maxlength="160"></label><label class="field-label full">{{ t('master.description') }}<textarea v-model.trim="form.description" rows="3" maxlength="500" /></label></div>
      <div v-else-if="props.entity === 'products'" class="form-grid"><label class="field-label">{{ t('master.sku') }}<input v-model.trim="form.sku" required maxlength="80"></label><label class="field-label">{{ t('master.name') }}<input v-model.trim="form.name" required maxlength="160"></label><label class="field-label full">{{ t('master.category') }}<select v-model="form.categoryId" required><option disabled value="">{{ t('master.noCategories') }}</option><option v-for="category in categoryOptions" :key="category.id" :value="category.id">{{ category.name }}{{ !category.isActive ? ` (${t('master.inactive')})` : '' }}</option></select></label><label class="field-label">{{ t('master.purchasePrice') }}<input v-model.number="form.purchasePrice" type="number" min="0" step="1" required></label><label class="field-label">{{ t('master.sellingPrice') }}<input v-model.number="form.sellingPrice" type="number" min="0" step="1" required></label><label class="field-label">{{ t('master.reorderLevel') }}<input v-model.number="form.reorderLevel" type="number" min="0" step="0.01" required></label><label class="field-label">{{ t('master.unit') }}<input v-model.trim="form.unit" required maxlength="24"></label></div>
      <div v-else class="form-grid"><label class="field-label">{{ t('master.code') }}<input v-model.trim="form.code" required maxlength="80"></label><label class="field-label">{{ t('master.name') }}<input v-model.trim="form.name" required maxlength="160"></label><label class="field-label">{{ t('master.email') }}<input v-model.trim="form.email" type="email" maxlength="160"></label><label class="field-label">{{ t('master.phone') }}<input v-model.trim="form.phone" maxlength="40"></label><label class="field-label full">{{ t('master.address') }}<textarea v-model.trim="form.address" rows="3" maxlength="300" /></label></div>
      <p v-if="formError" class="alert" style="margin-top: 14px">{{ formError }}</p><div class="modal-actions"><button class="secondary" type="button" @click="closeForm">{{ t('master.cancel') }}</button><button class="primary" :disabled="saving">{{ saving ? t('master.saving') : t('master.save') }}</button></div>
    </div></form></div></Teleport>
  </div>
</template>
