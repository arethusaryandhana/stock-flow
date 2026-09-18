<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import PaginationControls from '../components/PaginationControls.vue'
import { useI18n } from '../i18n'
import { useDisplayPreferences } from '../preferences'
import { useToastStore } from '../stores/toast'
import { useAuthStore } from '../stores/auth'
import { useUserManagement, type ManagedUser, type ManagedUserRequest } from '../userManagement'

const toast = useToastStore()
const auth = useAuthStore()
const canManage = computed(() => auth.can('action.users.manage'))
const { t } = useI18n()
const { defaultPageSize, formatDate } = useDisplayPreferences()
const {
  users: managedUsers,
  roles: managedRoles,
  loadUsers,
  loadRoles,
  createUser,
  updateUser,
} = useUserManagement()
const usersLoading = ref(false)
const usersSaving = ref(false)
const usersError = ref('')
const usersSearch = ref('')
const usersRoleFilter = ref('all')
const usersStatusFilter = ref('all')
const usersPage = ref(1)
const usersPageSize = ref<number>(defaultPageSize.value)
const userEditorOpen = ref(false)
const editingUserId = ref<string | null>(null)
const userDraft = ref<ManagedUserRequest>({
  fullName: '', email: '', password: '', role: 'Staff', isActive: true,
})
const roleOptions = computed(() => (managedRoles.value.length
  ? managedRoles.value
  : [{ name: 'Admin' }, { name: 'Manager' }, { name: 'Staff' }])
  .filter((role) => auth.isAdmin || role.name !== 'Admin'))

const userEditorTitle = computed(() => editingUserId.value ? t('settings.editUserTitle') : t('settings.addUserTitle'))

async function loadUserConfiguration() {
  usersLoading.value = true
  usersError.value = ''
  try {
    await Promise.all([
      loadRoles(),
      loadUsers({
        page: usersPage.value,
        pageSize: usersPageSize.value,
        search: usersSearch.value.trim() || undefined,
        role: usersRoleFilter.value === 'all' ? undefined : usersRoleFilter.value,
        status: usersStatusFilter.value === 'all' ? undefined : usersStatusFilter.value,
      }),
    ])
    if (!userDraft.value.role && managedRoles.value.length) userDraft.value.role = managedRoles.value[0].name
  } catch (requestError) {
    usersError.value = (requestError as Error).message
  } finally {
    usersLoading.value = false
  }
}

async function refreshUsers() {
  usersLoading.value = true
  usersError.value = ''
  try {
    await loadUsers({
      page: usersPage.value,
      pageSize: usersPageSize.value,
      search: usersSearch.value.trim() || undefined,
      role: usersRoleFilter.value === 'all' ? undefined : usersRoleFilter.value,
      status: usersStatusFilter.value === 'all' ? undefined : usersStatusFilter.value,
    })
  } catch (requestError) {
    usersError.value = (requestError as Error).message
  } finally {
    usersLoading.value = false
  }
}

function openUserEditor(user?: ManagedUser) {
  if (!canManage.value) return
  editingUserId.value = user?.id ?? null
  userDraft.value = {
    fullName: user?.fullName ?? '',
    email: user?.email ?? '',
    password: '',
    role: user?.role ?? 'Staff',
    isActive: user?.isActive ?? true,
  }
  usersError.value = ''
  userEditorOpen.value = true
}

function closeUserEditor() {
  userEditorOpen.value = false
  editingUserId.value = null
}

async function saveUserConfiguration() {
  if (!canManage.value) return
  usersError.value = ''
  const fullName = userDraft.value.fullName.trim().replace(/\s+/g, ' ')
  const email = userDraft.value.email.trim().toLowerCase()
  const password = userDraft.value.password?.trim() || null
  if (!fullName || !email || !userDraft.value.role || (!editingUserId.value && !password)) {
    usersError.value = t('settings.userValidation')
    return
  }
  if (password && password.length < 12) {
    usersError.value = t('settings.userPasswordValidation')
    return
  }
  usersSaving.value = true
  try {
    const wasEditing = Boolean(editingUserId.value)
    const request: ManagedUserRequest = { ...userDraft.value, fullName, email, password }
    if (editingUserId.value) await updateUser(editingUserId.value, request)
    else await createUser(request)
    closeUserEditor()
    await refreshUsers()
    toast.success(t(wasEditing ? 'settings.userUpdatedToast' : 'settings.userCreatedToast'))
  } catch (requestError) {
    usersError.value = (requestError as Error).message
  } finally {
    usersSaving.value = false
  }
}

function changeUsersFilter() {
  usersPage.value = 1
  void refreshUsers()
}

function changeUsersPageSize(nextPageSize: number) {
  usersPageSize.value = nextPageSize
  usersPage.value = 1
  void refreshUsers()
}

function changeUsersPage(nextPage: number) {
  usersPage.value = nextPage
  void refreshUsers()
}

onMounted(() => {
  void loadUserConfiguration()
})
</script>

<template>
  <div class="page users-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('app.administration') }}</p>
        <h1>{{ t('app.users') }}</h1>
        <p class="subtitle">{{ t('settings.usersListDescription') }}</p>
      </div>
    </div>
    <main class="settings-content">
        <section class="surface-card inventory-summary users-summary">
          <div class="inventory-summary-icon users-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3" /><path d="M3.5 20c.6-3.6 2.4-5.5 5.5-5.5s4.9 1.9 5.5 5.5M16 11a3 3 0 1 0 0-6M16 14.5c2.8 0 4.5 1.8 5 5.5" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.adminConfiguration') }}</span>
            <h2>{{ t('settings.usersSummaryTitle') }}</h2>
            <p>{{ t('settings.usersSummaryDescription') }}</p>
          </div>
          <button v-if="canManage" class="primary users-add-button" type="button" @click="openUserEditor()"><span class="button-plus">+</span>{{ t('settings.addUser') }}</button>
        </section>

        <p v-if="usersError" class="alert error-banner" role="alert">{{ usersError }}</p>

        <section v-if="userEditorOpen && canManage" class="surface-card settings-card user-editor-card">
          <div class="settings-card-head"><div><h2>{{ userEditorTitle }}</h2><p>{{ editingUserId ? t('settings.editUserDescription') : t('settings.addUserDescription') }}</p></div><button class="ghost-button" type="button" :disabled="usersSaving" @click="closeUserEditor">{{ t('common.cancel') }}</button></div>
          <form class="user-editor-form" :aria-busy="usersSaving" @submit.prevent="saveUserConfiguration">
            <div class="user-fields">
              <label class="field-label">{{ t('settings.userFullName') }}<input v-model="userDraft.fullName" maxlength="160" autocomplete="name" required :disabled="usersSaving"><small class="field-hint">{{ t('settings.userFullNameHint') }}</small></label>
              <label class="field-label">{{ t('settings.userEmail') }}<input v-model="userDraft.email" type="email" maxlength="254" autocomplete="email" required :disabled="usersSaving"><small class="field-hint">{{ t('settings.userEmailHint') }}</small></label>
              <label class="field-label">{{ t('settings.userRole') }}<select v-model="userDraft.role" :disabled="usersSaving"><option v-for="roleOption in roleOptions" :key="roleOption.name" :value="roleOption.name">{{ roleOption.name }}</option></select><small class="field-hint">{{ t('settings.userRoleHint') }}</small></label>
              <label class="field-label">{{ editingUserId ? t('settings.userNewPassword') : t('settings.userPassword') }}<input v-model="userDraft.password" type="password" minlength="12" autocomplete="new-password" :required="!editingUserId" :disabled="usersSaving"><small class="field-hint">{{ editingUserId ? t('settings.userNewPasswordHint') : t('settings.userPasswordHint') }}</small></label>
            </div>
            <label class="user-active-toggle"><input v-model="userDraft.isActive" type="checkbox" :disabled="usersSaving"><span><strong>{{ t('settings.userActive') }}</strong><small>{{ t('settings.userActiveHint') }}</small></span></label>
            <div class="profile-actions"><button class="secondary" type="button" :disabled="usersSaving" @click="closeUserEditor">{{ t('common.cancel') }}</button><button class="primary" type="submit" :disabled="usersSaving"><span v-if="usersSaving" class="button-spinner" aria-hidden="true" />{{ usersSaving ? t('settings.savingUser') : t('settings.saveUser') }}</button></div>
          </form>
        </section>

        <section class="surface-card settings-card users-list-card">
          <div class="settings-card-head"><div><h2>{{ t('settings.usersListTitle') }}</h2><p>{{ t('settings.usersListDescription') }}</p></div><span class="admin-badge">{{ managedUsers.totalCount }} {{ t('settings.usersCount') }}</span></div>
          <div class="toolbar users-toolbar"><label class="search-input"><span>⌕</span><input v-model="usersSearch" :aria-label="t('settings.userSearchAria')" :placeholder="t('settings.userSearchPlaceholder')" @keyup.enter="changeUsersFilter"></label><div class="toolbar-actions"><select v-model="usersRoleFilter" class="filter-select wide" :aria-label="t('settings.userRoleFilterAria')" @change="changeUsersFilter"><option value="all">{{ t('settings.allRoles') }}</option><option v-for="roleOption in roleOptions" :key="roleOption.name" :value="roleOption.name">{{ roleOption.name }}</option></select><select v-model="usersStatusFilter" class="filter-select wide" :aria-label="t('settings.userStatusFilterAria')" @change="changeUsersFilter"><option value="all">{{ t('settings.allStatuses') }}</option><option value="active">{{ t('settings.active') }}</option><option value="inactive">{{ t('settings.inactive') }}</option></select><button class="secondary" type="button" :disabled="usersLoading" @click="changeUsersFilter">{{ t('settings.applyFilters') }}</button></div></div>
          <div v-if="usersLoading" class="empty">{{ t('settings.loadingUsers') }}</div>
          <div v-else-if="!managedUsers.items.length" class="empty"><strong>{{ t('settings.usersEmptyTitle') }}</strong>{{ t('settings.usersEmptyHint') }}</div>
          <div v-else class="table-wrap users-table-wrap"><table><thead><tr><th>{{ t('settings.userIdentity') }}</th><th>{{ t('settings.userRole') }}</th><th>{{ t('settings.userStatus') }}</th><th>{{ t('settings.userCreated') }}</th><th v-if="canManage">{{ t('settings.userActions') }}</th></tr></thead><tbody><tr v-for="user in managedUsers.items" :key="user.id"><td><strong>{{ user.fullName }}</strong><small>{{ user.email }}</small></td><td><span class="role-pill" :class="`role-${user.role.toLowerCase()}`">{{ user.role }}</span></td><td><span class="badge" :class="user.isActive ? 'ok' : 'neutral'">{{ user.isActive ? t('settings.active') : t('settings.inactive') }}</span></td><td class="date-cell">{{ formatDate(user.createdAt) }}</td><td v-if="canManage"><button v-if="auth.isAdmin || user.role !== 'Admin'" class="secondary compact-action" type="button" @click="openUserEditor(user)">{{ t('settings.editUser') }}</button></td></tr></tbody></table></div>
          <PaginationControls v-if="!usersLoading && managedUsers.items.length" :page="managedUsers.page" :page-size="managedUsers.pageSize" :total-count="managedUsers.totalCount" :total-pages="managedUsers.totalPages" @page-change="changeUsersPage" @page-size-change="changeUsersPageSize" />
        </section>

    </main>
  </div>
</template>

<style scoped>
.users-page { max-width: 1260px; }
.settings-content { display: grid; gap: 14px; }
.account-kicker { color: var(--muted-2); font-size: .55rem; font-weight: 800; letter-spacing: .1em; text-transform: uppercase; }
.inventory-summary { display: flex; min-height: 106px; align-items: center; gap: 14px; padding: 20px; background: linear-gradient(125deg, color-mix(in srgb, var(--blue) 7%, var(--surface)) 0%, var(--surface) 48%, color-mix(in srgb, var(--amber) 7%, var(--surface)) 100%); }
.inventory-summary-icon { display: grid; width: 58px; height: 58px; flex: 0 0 58px; place-items: center; border: 1px solid color-mix(in srgb, var(--amber) 18%, var(--line)); border-radius: 16px; color: var(--amber); background: var(--amber-soft); }
.inventory-summary-icon svg { width: 28px; height: 28px; }
.inventory-summary > div:nth-child(2) { min-width: 0; flex: 1; }
.inventory-summary h2 { margin-top: 5px; font-size: 1.04rem; }
.inventory-summary p { max-width: 620px; margin-top: 5px; color: var(--muted); font-size: .64rem; line-height: 1.5; }
.admin-badge { display: inline-flex; flex: 0 0 auto; align-items: center; padding: 7px 10px; border: 1px solid color-mix(in srgb, var(--amber) 20%, var(--line)); border-radius: 999px; color: var(--amber); background: var(--amber-soft); font-size: .57rem; font-weight: 800; }
.users-summary-icon { color: var(--teal); background: var(--teal-soft); }
.users-add-button { flex: 0 0 auto; white-space: nowrap; }
.user-editor-card, .users-list-card, .permissions-card { overflow: hidden; }
.user-editor-form { padding: 20px; }
.user-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.user-fields input, .user-fields select { width: 100%; }
.user-active-toggle { display: flex; align-items: flex-start; gap: 10px; margin-top: 18px; color: var(--control-text); }
.user-active-toggle input { width: 17px; height: 17px; margin-top: 1px; accent-color: var(--blue); }
.user-active-toggle strong, .user-active-toggle small { display: block; }
.user-active-toggle strong { color: var(--ink); font-size: .67rem; }
.user-active-toggle small { margin-top: 3px; color: var(--muted); font-size: .6rem; }
.users-toolbar { align-items: center; padding: 14px 20px; border-bottom: 1px solid var(--line); }
.users-toolbar .search-input { max-width: 330px; }
.users-table-wrap { padding: 0 20px; }
.users-table-wrap table { min-width: 640px; }
.users-table-wrap td small { display: block; margin-top: 4px; }
.role-pill { display: inline-flex; align-items: center; min-height: 24px; padding: 4px 8px; border-radius: 999px; font-size: .58rem; font-weight: 800; }
.role-pill.role-admin { color: var(--blue); background: var(--blue-soft); }
.role-pill.role-manager { color: var(--teal); background: var(--teal-soft); }
.role-pill.role-staff { color: var(--amber); background: var(--amber-soft); }
.compact-action { min-height: 32px; padding: 0 10px; font-size: .6rem; }
.permissions-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; padding: 20px; }
.permissions-grid article { display: grid; align-content: start; gap: 8px; min-height: 132px; padding: 14px; border: 1px solid var(--line); border-radius: 11px; background: var(--surface-raised); }
.permissions-grid article .role-pill { width: max-content; }
.permissions-grid article strong { color: var(--ink); font-size: .67rem; }
.permissions-grid article p { color: var(--muted); font-size: .6rem; line-height: 1.5; }
.settings-card { overflow: hidden; }
.settings-card-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; padding: 20px 20px 17px; border-bottom: 1px solid var(--line); }
.settings-card-head p { max-width: 600px; margin-top: 5px; color: var(--muted); font-size: .66rem; line-height: 1.5; }
.settings-card-icon { display: grid; width: 34px; height: 34px; flex: 0 0 34px; place-items: center; border-radius: 9px; }
.settings-card-icon.teal { color: var(--teal); background: var(--teal-soft); }
.profile-actions { display: flex; justify-content: flex-end; gap: 9px; margin-top: 19px; padding-top: 17px; border-top: 1px solid var(--line); }
@media (max-width: 600px) {
  .inventory-summary { align-items: flex-start; flex-wrap: wrap; }
  .inventory-summary > div:nth-child(2) { flex: 0 0 calc(100% - 72px); }
  .users-summary .users-add-button { width: calc(100% - 72px); margin-left: 72px; }
  .users-toolbar { align-items: stretch; flex-direction: column; }
  .users-toolbar .search-input { max-width: none; }
  .users-toolbar .toolbar-actions { flex-wrap: wrap; }
  .users-toolbar .toolbar-actions > * { flex: 1; }
  .user-fields { grid-template-columns: 1fr; }
  .permissions-grid { grid-template-columns: 1fr; }
  .profile-actions button { flex: 1; }
}
</style>
