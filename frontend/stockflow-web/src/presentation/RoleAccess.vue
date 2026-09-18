<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useI18n } from '../i18n'

type Role = { id: string; name: string; isSystem: boolean; isActive: boolean; userCount: number; permissions: string[] }
type Permission = { code: string; group: string; name: string; kind: 'menu' | 'action'; requiresMenu: string | null }

const props = defineProps<{ mode: 'roles' | 'access' }>()
const auth = useAuthStore()
const toast = useToastStore()
const { t } = useI18n()
const roles = ref<Role[]>([])
const permissions = ref<Permission[]>([])
const selectedRoleId = ref('')
const draftPermissions = ref<string[]>([])
const name = ref('')
const editingId = ref<string | null>(null)
const busy = ref(false)
const loading = ref(true)
const error = ref('')

const selectedRole = computed(() => roles.value.find((role) => role.id === selectedRoleId.value))
const canManageRoles = computed(() => auth.can('action.roles.manage'))
const canManageAccess = computed(() => auth.can('action.access.manage'))
const canEditSelected = computed(() => canManageAccess.value && selectedRole.value?.isActive && selectedRole.value.name !== 'Admin')
const isDirty = computed(() => {
  const saved = selectedRole.value?.permissions ?? []
  return saved.length !== draftPermissions.value.length || saved.some((code) => !draftPermissions.value.includes(code))
})
const groups = computed(() => {
  const order = ['workspace', 'administration', 'access', 'inventory', 'operations', 'insight']
  return order.map((key) => ({ key, items: permissions.value.filter((item) => item.group === key) }))
    .filter((group) => group.items.length)
})
const menuPreview = computed(() => permissions.value.filter((item) =>
  item.kind === 'menu' && draftPermissions.value.includes(item.code)))
const groupLabels: Record<string, string> = {
  workspace: 'app.workspace', administration: 'app.administration', access: 'app.accessManagement',
  inventory: 'app.inventory', operations: 'app.operations', insight: 'app.insight',
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [roleResponse, permissionResponse] = await Promise.all([
      api.get<Role[]>('/access/roles'),
      props.mode === 'access' ? api.get<Permission[]>('/access/permissions') : Promise.resolve({ data: [] as Permission[] }),
    ])
    roles.value = roleResponse.data
    permissions.value = permissionResponse.data
    if (!roles.value.some((role) => role.id === selectedRoleId.value)) {
      selectedRoleId.value = roles.value.find((role) => role.name !== 'Admin' && role.isActive)?.id ?? roles.value[0]?.id ?? ''
    }
    draftPermissions.value = [...(selectedRole.value?.permissions ?? [])]
  } catch (cause) {
    error.value = (cause as Error).message
  } finally {
    loading.value = false
  }
}

function chooseRole(id: string) {
  selectedRoleId.value = id
  draftPermissions.value = [...(selectedRole.value?.permissions ?? [])]
}

function togglePermission(item: Permission) {
  if (!canEditSelected.value) return
  const next = new Set(draftPermissions.value)
  if (next.has(item.code)) {
    next.delete(item.code)
    if (item.kind === 'menu') {
      permissions.value.filter((permission) => permission.requiresMenu === item.code)
        .forEach((permission) => next.delete(permission.code))
    }
  } else {
    next.add(item.code)
    if (item.requiresMenu) next.add(item.requiresMenu)
  }
  draftPermissions.value = [...next]
}

async function savePermissions() {
  if (!canEditSelected.value || !selectedRole.value || !isDirty.value) return
  busy.value = true
  error.value = ''
  try {
    const { data } = await api.put<Role>(`/access/roles/${selectedRole.value.id}/permissions`, {
      permissions: draftPermissions.value,
    })
    roles.value = roles.value.map((role) => role.id === data.id ? data : role)
    draftPermissions.value = [...data.permissions]
    await auth.refreshAccess()
    toast.success(t('access.saved'))
  } catch (cause) {
    error.value = (cause as Error).message
  } finally {
    busy.value = false
  }
}

async function saveRole() {
  if (!canManageRoles.value || !name.value.trim()) return
  busy.value = true
  error.value = ''
  try {
    if (editingId.value) {
      const role = roles.value.find((item) => item.id === editingId.value)
      if (!role || role.isSystem) return
      await api.put(`/access/roles/${role.id}`, { name: name.value.trim(), isActive: role.isActive })
    } else {
      await api.post('/access/roles', { name: name.value.trim(), isActive: true })
    }
    name.value = ''
    editingId.value = null
    await load()
    toast.success(t('access.saved'))
  } catch (cause) {
    error.value = (cause as Error).message
  } finally {
    busy.value = false
  }
}

function editRole(role: Role) {
  editingId.value = role.id
  name.value = role.name
}

async function toggleRole(role: Role) {
  if (!canManageRoles.value || role.isSystem || (role.isActive && role.userCount > 0)) return
  busy.value = true
  error.value = ''
  try {
    await api.put(`/access/roles/${role.id}`, { name: role.name, isActive: !role.isActive })
    await load()
    toast.success(t('access.saved'))
  } catch (cause) {
    error.value = (cause as Error).message
  } finally {
    busy.value = false
  }
}

watch(() => props.mode, () => { void load() })
onMounted(() => { void load() })
</script>

<template>
  <div class="page access-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('app.accessManagement') }}</p>
        <h1>{{ t(mode === 'roles' ? 'app.roles' : 'app.menuAccess') }}</h1>
        <p class="subtitle">{{ t(mode === 'roles' ? 'access.rolesDescription' : 'access.matrixDescription') }}</p>
      </div>
    </div>
    <div class="access-tabs">
      <router-link v-if="auth.can('menu.roles')" to="/admin/roles" :class="{ active: mode === 'roles' }">{{ t('app.roles') }}</router-link>
      <router-link v-if="auth.can('menu.access')" to="/admin/access" :class="{ active: mode === 'access' }">{{ t('app.menuAccess') }}</router-link>
    </div>
    <p v-if="error" class="alert error-banner" role="alert">{{ error }}</p>
    <p v-if="loading" class="surface-card access-message">{{ t('access.loading') }}</p>

    <template v-else-if="mode === 'roles'">
      <section v-if="canManageRoles" class="surface-card access-card">
        <h2>{{ t(editingId ? 'access.editRole' : 'access.addRole') }}</h2>
        <form class="access-form" @submit.prevent="saveRole">
          <label><span>{{ t('access.roleName') }}</span><input v-model="name" maxlength="64" minlength="2" required></label>
          <button class="primary" type="submit" :disabled="busy">{{ t('access.save') }}</button>
          <button v-if="editingId" class="secondary" type="button" @click="editingId = null; name = ''">{{ t('access.cancel') }}</button>
        </form>
      </section>
      <section class="surface-card access-card">
        <div class="access-section-title"><h2>{{ t('access.roleList') }}</h2><span>{{ roles.length }} role</span></div>
        <div class="access-role-list">
          <article v-for="role in roles" :key="role.id" class="access-role-row">
            <div><strong>{{ role.name }}</strong><small>{{ role.isSystem ? t('access.builtIn') : t('access.custom') }} · {{ role.userCount }} {{ t('access.users') }} · {{ role.permissions.length }} {{ t('access.permissions') }}</small></div>
            <span class="access-status" :class="{ inactive: !role.isActive }">{{ role.isActive ? t('access.active') : t('access.inactive') }}</span>
            <div v-if="canManageRoles && !role.isSystem" class="access-row-actions">
              <button class="secondary" type="button" :disabled="busy" @click="editRole(role)">{{ t('access.edit') }}</button>
              <button class="secondary" type="button" :disabled="busy || (role.isActive && role.userCount > 0)" @click="toggleRole(role)">{{ role.isActive ? t('access.deactivate') : t('access.activate') }}</button>
            </div>
          </article>
        </div>
        <p class="access-note">{{ t('access.roleNote') }}</p>
      </section>
    </template>

    <template v-else>
      <section class="surface-card access-card">
        <div class="access-toolbar">
          <label><span>{{ t('access.selectRole') }}</span>
            <select :value="selectedRoleId" @change="chooseRole(($event.target as HTMLSelectElement).value)">
              <option v-for="role in roles" :key="role.id" :value="role.id">{{ role.name }}{{ role.isActive ? '' : ` (${t('access.inactive')})` }}</option>
            </select>
          </label>
          <div><strong>{{ draftPermissions.filter((code) => code.startsWith('menu.')).length }}</strong> {{ t('access.menus') }} · <strong>{{ draftPermissions.filter((code) => code.startsWith('action.')).length }}</strong> {{ t('access.actions') }}</div>
          <button v-if="canEditSelected" class="primary" type="button" :disabled="busy || !isDirty" @click="savePermissions">{{ t('access.saveAccess') }}</button>
        </div>
        <p class="access-note">{{ selectedRole?.name === 'Admin' ? t('access.adminLocked') : t('access.matrixHint') }}</p>
        <div class="access-preview">
          <h3>{{ t('access.menuPreview') }}</h3>
          <p v-if="!menuPreview.length" class="access-note">{{ t('access.noMenuPreview') }}</p>
          <div v-else class="access-preview-items">
            <span v-for="item in menuPreview" :key="item.code">{{ item.name }}</span>
          </div>
        </div>
      </section>
      <section v-for="group in groups" :key="group.key" class="surface-card access-card">
        <div class="access-section-title"><h2>{{ t(groupLabels[group.key]) }}</h2><span>{{ group.items.length }} {{ t('access.permissions') }}</span></div>
        <div class="access-permission-grid">
          <label v-for="item in group.items" :key="item.code" class="access-permission">
            <input type="checkbox" :checked="draftPermissions.includes(item.code)" :disabled="!canEditSelected" @change="togglePermission(item)">
            <span><strong>{{ item.name }}</strong><small>{{ item.kind === 'menu' ? t('access.menu') : t('access.action') }} · {{ item.code }}</small></span>
          </label>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.access-page { display: grid; gap: 1.25rem; }
.access-tabs { display: flex; gap: .5rem; border-bottom: 1px solid var(--line); }
.access-tabs a { padding: .75rem 1rem; color: var(--muted); text-decoration: none; border-bottom: 2px solid transparent; }
.access-tabs a.active { color: var(--ink); border-color: var(--teal); font-weight: 700; }
.access-card { padding: 1.5rem; }
.access-card h2 { margin: 0; font-size: 1.1rem; }
.access-form, .access-toolbar { display: flex; align-items: end; gap: .75rem; flex-wrap: wrap; }
.access-form { margin-top: 1rem; }
.access-form label, .access-toolbar label { display: grid; gap: .4rem; min-width: min(100%, 18rem); }
.access-form input, .access-toolbar select { min-height: 2.6rem; width: 100%; border: 1px solid var(--line); border-radius: .6rem; padding: .4rem .7rem; background: var(--surface); color: inherit; }
.access-section-title { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
.access-section-title span, .access-note, .access-role-row small, .access-permission small { color: var(--muted); }
.access-role-row { display: flex; align-items: center; gap: 1rem; justify-content: space-between; padding: 1rem 0; border-top: 1px solid var(--line); flex-wrap: wrap; }
.access-role-row > div:first-child, .access-permission span { display: grid; gap: .2rem; }
.access-row-actions { display: flex; gap: .5rem; }
.access-status { font-size: .8rem; color: #07815f; }
.access-status.inactive { color: var(--muted); }
.access-toolbar { justify-content: space-between; }
.access-permission-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr)); gap: .6rem; }
.access-permission { display: flex; gap: .65rem; align-items: start; padding: .8rem; border: 1px solid var(--line); border-radius: .7rem; cursor: pointer; }
.access-permission input { margin-top: .2rem; }
.access-permission small { overflow-wrap: anywhere; }
.access-message { padding: 1.5rem; }
.access-preview { margin-top: 1.25rem; padding-top: 1rem; border-top: 1px solid var(--line); }
.access-preview h3 { margin: 0 0 .7rem; font-size: .9rem; }
.access-preview-items { display: flex; flex-wrap: wrap; gap: .4rem; }
.access-preview-items span { padding: .35rem .65rem; border-radius: 999px; background: var(--teal-soft); color: var(--teal); font-size: .78rem; }
</style>
