<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ChangePasswordModal from '../components/ChangePasswordModal.vue'
import FormattedNumberInput from '../components/FormattedNumberInput.vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'
import type { Language } from '../i18n'
import {
  useNotificationPreferences,
  type NotificationPollingInterval,
  type NotificationPreferences,
} from '../notificationPreferences'
import {
  useDisplayPreferences,
  type DateFormatPreference,
  type NumberFormatPreference,
  type TimeZonePreference,
} from '../preferences'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useInventorySettings, type InventorySettings } from '../inventorySettings'
import { useCompanyProfile, type CompanyProfile } from '../companyProfile'
import { useUserManagement, type ManagedUser, type ManagedUserRequest } from '../userManagement'
import { useTheme, type ThemePreference } from '../theme'

type SessionProfile = { fullName: string; email: string; role: string }
type SettingsSection = 'account' | 'display' | 'notifications' | 'inventory' | 'company' | 'users'
type NotificationToggleKey = Exclude<keyof NotificationPreferences, 'pollingIntervalSeconds'>

const auth = useAuthStore()
const toast = useToastStore()
const { language, setLanguage, t } = useI18n()
const { preference: themePreference, resolvedTheme, setTheme } = useTheme()
const {
  preferences: notificationPreferences,
  loadPreferences: loadNotificationPreferences,
  savePreferences: saveNotificationPreferences,
} = useNotificationPreferences()
const {
  settings: inventorySettings,
  loadSettings: loadInventorySettings,
  saveSettings: saveInventorySettings,
} = useInventorySettings()
const {
  profile: companyProfile,
  loadProfile: loadCompanyProfile,
  saveProfile: saveCompanyProfile,
} = useCompanyProfile()
const {
  users: managedUsers,
  roles: managedRoles,
  loadUsers,
  loadRoles,
  createUser,
  updateUser,
} = useUserManagement()
const {
  timeZone,
  dateFormat,
  numberFormat,
  defaultPageSize,
  systemTimeZone,
  setTimeZone,
  setDateFormat,
  setNumberFormat,
  setDefaultPageSize,
  formatDate,
  formatNumber,
} = useDisplayPreferences()

const activeSection = ref<SettingsSection>('account')
const fullName = ref(auth.name)
const email = ref(auth.email)
const role = ref(auth.role)
const initialFullName = ref(auth.name)
const initialEmail = ref(auth.email)
const currentPassword = ref('')
const showCurrentPassword = ref(false)
const changePasswordOpen = ref(false)
const confirmLogoutAll = ref(false)
const loading = ref(true)
const saving = ref(false)
const revoking = ref(false)
const error = ref('')
const formError = ref('')
const securityError = ref('')
const notificationLoading = ref(true)
const notificationSaving = ref(false)
const notificationError = ref('')
const inventoryLoading = ref(false)
const inventorySaving = ref(false)
const inventoryError = ref('')
const companyLoading = ref(false)
const companySaving = ref(false)
const companyError = ref('')
const companyDraft = ref({
  name: 'StockFlow Demo',
  address: '',
  email: '',
  phone: '',
  currency: 'IDR',
  logoUrl: '',
})
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
const roleOptions = computed(() => managedRoles.value.length
  ? managedRoles.value
  : [{ name: 'Admin' }, { name: 'Manager' }, { name: 'Staff' }])
const inventoryDraft = ref({
  defaultReorderLevel: '5',
  defaultUnit: 'pcs',
  allowNegativeStock: false,
  globalLowStockThreshold: '0',
})
const previewNow = new Date()

const datePreview = computed(() => formatDate(previewNow, { includeTime: true }))
const numberPreview = computed(() => formatNumber(1234567.89, { minimumFractionDigits: 2, maximumFractionDigits: 2 }))
const currencyPreview = computed(() => formatNumber(1250000, { style: 'currency', currency: 'IDR', maximumFractionDigits: 0 }))
const enabledNotificationCategories = computed(() => [
  notificationPreferences.value.lowStockEnabled,
  notificationPreferences.value.reportReadyEnabled,
  notificationPreferences.value.systemEnabled,
].filter(Boolean).length)
const isAdmin = computed(() => auth.isAdmin)
const inventoryDirty = computed(() => {
  const reorder = inventoryDraft.value.defaultReorderLevel.trim()
  const threshold = inventoryDraft.value.globalLowStockThreshold.trim()
  return !reorder || Number(reorder) !== inventorySettings.value.defaultReorderLevel ||
    inventoryDraft.value.defaultUnit.trim() !== inventorySettings.value.defaultUnit ||
    inventoryDraft.value.allowNegativeStock !== inventorySettings.value.allowNegativeStock ||
    !threshold || Number(threshold) !== inventorySettings.value.globalLowStockThreshold
})
const companyDirty = computed(() =>
  companyDraft.value.name.trim() !== companyProfile.value.name ||
  companyDraft.value.address.trim() !== (companyProfile.value.address ?? '') ||
  companyDraft.value.email.trim().toLowerCase() !== (companyProfile.value.email ?? '') ||
  companyDraft.value.phone.trim() !== (companyProfile.value.phone ?? '') ||
  companyDraft.value.currency !== companyProfile.value.currency ||
  companyDraft.value.logoUrl.trim() !== (companyProfile.value.logoUrl ?? ''),
)

const normalizedName = computed(() => fullName.value.trim().replace(/\s+/g, ' '))
const normalizedEmail = computed(() => email.value.trim().toLowerCase())
const emailChanged = computed(() => normalizedEmail.value !== initialEmail.value.toLowerCase())
const dirty = computed(() =>
  normalizedName.value !== initialFullName.value || emailChanged.value,
)
const initials = computed(() =>
  (initialFullName.value || auth.name || t('app.defaultName'))
    .split(/\s+/)
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase(),
)

function applyProfile(profile: SessionProfile) {
  fullName.value = profile.fullName
  email.value = profile.email
  role.value = profile.role
  initialFullName.value = profile.fullName
  initialEmail.value = profile.email
  currentPassword.value = ''
  auth.setSession(profile)
}

async function loadProfile() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get<SessionProfile>('/auth/session')
    applyProfile(data)
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    loading.value = false
  }
}

function resetProfile() {
  fullName.value = initialFullName.value
  email.value = initialEmail.value
  currentPassword.value = ''
  formError.value = ''
}

async function saveProfile() {
  formError.value = ''
  if (!normalizedName.value || !normalizedEmail.value) {
    formError.value = t('settings.requiredFields')
    return
  }
  if (emailChanged.value && !currentPassword.value) {
    formError.value = t('settings.emailPasswordRequired')
    return
  }

  saving.value = true
  try {
    const { data } = await api.put<SessionProfile>('/auth/profile', {
      fullName: normalizedName.value,
      email: normalizedEmail.value,
      currentPassword: emailChanged.value ? currentPassword.value : null,
    })
    applyProfile(data)
    toast.success(t('settings.profileSaved'))
  } catch (requestError) {
    formError.value = (requestError as Error).message
  } finally {
    saving.value = false
  }
}

async function logoutAll() {
  securityError.value = ''
  revoking.value = true
  try {
    await auth.logoutAll()
  } catch (requestError) {
    securityError.value = (requestError as Error).message
    revoking.value = false
  }
}

async function loadNotificationSettings() {
  notificationLoading.value = true
  notificationError.value = ''
  try {
    await loadNotificationPreferences(true)
  } catch (requestError) {
    notificationError.value = (requestError as Error).message
  } finally {
    notificationLoading.value = false
  }
}

function applyInventorySettings(settings: InventorySettings) {
  inventoryDraft.value = {
    defaultReorderLevel: String(settings.defaultReorderLevel),
    defaultUnit: settings.defaultUnit,
    allowNegativeStock: settings.allowNegativeStock,
    globalLowStockThreshold: String(settings.globalLowStockThreshold),
  }
}

async function loadInventoryConfiguration() {
  if (!isAdmin.value) return
  inventoryLoading.value = true
  inventoryError.value = ''
  try {
    applyInventorySettings(await loadInventorySettings(true))
  } catch (requestError) {
    inventoryError.value = (requestError as Error).message
  } finally {
    inventoryLoading.value = false
  }
}

function resetInventoryConfiguration() {
  applyInventorySettings(inventorySettings.value)
  inventoryError.value = ''
}

async function saveInventoryConfiguration() {
  inventoryError.value = ''
  const reorderRaw = inventoryDraft.value.defaultReorderLevel.trim()
  const thresholdRaw = inventoryDraft.value.globalLowStockThreshold.trim()
  const defaultReorderLevel = Number(reorderRaw)
  const globalLowStockThreshold = Number(thresholdRaw)
  const defaultUnit = inventoryDraft.value.defaultUnit.trim()
  if (!defaultUnit || !isValidInventoryQuantity(reorderRaw) || !isValidInventoryQuantity(thresholdRaw)) {
    inventoryError.value = t('settings.inventoryValidation')
    return
  }

  inventorySaving.value = true
  try {
    const saved = await saveInventorySettings({
      defaultReorderLevel,
      defaultUnit,
      allowNegativeStock: inventoryDraft.value.allowNegativeStock,
      globalLowStockThreshold,
    })
    applyInventorySettings(saved)
    toast.success(t('settings.inventorySaved'))
  } catch (requestError) {
    inventoryError.value = (requestError as Error).message
  } finally {
    inventorySaving.value = false
  }
}

function applyCompanyProfile(profile: CompanyProfile) {
  companyDraft.value = {
    name: profile.name,
    address: profile.address ?? '',
    email: profile.email ?? '',
    phone: profile.phone ?? '',
    currency: profile.currency,
    logoUrl: profile.logoUrl ?? '',
  }
}

async function loadCompanyConfiguration() {
  if (!isAdmin.value) return
  companyLoading.value = true
  companyError.value = ''
  try {
    applyCompanyProfile(await loadCompanyProfile(true))
  } catch (requestError) {
    companyError.value = (requestError as Error).message
  } finally {
    companyLoading.value = false
  }
}

function resetCompanyConfiguration() {
  applyCompanyProfile(companyProfile.value)
  companyError.value = ''
}

async function saveCompanyConfiguration() {
  companyError.value = ''
  const name = companyDraft.value.name.trim()
  const email = companyDraft.value.email.trim().toLowerCase()
  const logoUrl = companyDraft.value.logoUrl.trim()
  if (!name || name.length > 160 || companyDraft.value.address.trim().length > 300 ||
      email.length > 254 || companyDraft.value.phone.trim().length > 40 || logoUrl.length > 1000) {
    companyError.value = t('settings.companyValidation')
    return
  }
  if (email && !/^\S+@\S+\.\S+$/.test(email)) {
    companyError.value = t('settings.companyEmailValidation')
    return
  }
  if (logoUrl && !/^https?:\/\//i.test(logoUrl)) {
    companyError.value = t('settings.companyLogoValidation')
    return
  }
  companySaving.value = true
  try {
    const saved = await saveCompanyProfile({
      name,
      address: companyDraft.value.address.trim() || null,
      email: email || null,
      phone: companyDraft.value.phone.trim() || null,
      currency: companyDraft.value.currency,
      logoUrl: logoUrl || null,
    })
    applyCompanyProfile(saved)
    toast.success(t('settings.companySaved'))
  } catch (requestError) {
    companyError.value = (requestError as Error).message
  } finally {
    companySaving.value = false
  }
}

const userEditorTitle = computed(() => editingUserId.value ? t('settings.editUserTitle') : t('settings.addUserTitle'))

async function loadUserConfiguration() {
  if (!isAdmin.value) return
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

function isValidInventoryQuantity(raw: string) {
  const value = Number(raw)
  return raw !== '' && Number.isFinite(value) && value >= 0 && value <= 9_999_999_999.99 &&
    Number(value.toFixed(2)) === value
}

async function updateNotificationPreferences(next: NotificationPreferences) {
  if (notificationSaving.value) return
  const previous = notificationPreferences.value
  notificationPreferences.value = next
  notificationSaving.value = true
  notificationError.value = ''
  try {
    await saveNotificationPreferences(next)
  } catch (requestError) {
    notificationPreferences.value = previous
    notificationError.value = (requestError as Error).message
  } finally {
    notificationSaving.value = false
  }
}

function toggleNotificationPreference(key: NotificationToggleKey) {
  void updateNotificationPreferences({
    ...notificationPreferences.value,
    [key]: !notificationPreferences.value[key],
  })
}

function changeNotificationInterval(event: Event) {
  void updateNotificationPreferences({
    ...notificationPreferences.value,
    pollingIntervalSeconds: Number((event.target as HTMLSelectElement).value) as NotificationPollingInterval,
  })
}

function changeLanguage(value: Language) {
  setLanguage(value)
}

function changeTheme(value: ThemePreference) {
  setTheme(value)
}

function changeTimeZone(event: Event) {
  setTimeZone((event.target as HTMLSelectElement).value as TimeZonePreference)
}

function changeDateFormat(event: Event) {
  setDateFormat((event.target as HTMLSelectElement).value as DateFormatPreference)
}

function changeNumberFormat(event: Event) {
  setNumberFormat((event.target as HTMLSelectElement).value as NumberFormatPreference)
}

function changeDefaultPageSize(event: Event) {
  setDefaultPageSize(Number((event.target as HTMLSelectElement).value))
}

onMounted(async () => {
  await Promise.all([loadProfile(), loadNotificationSettings()])
  await loadInventoryConfiguration()
  await loadCompanyConfiguration()
  await loadUserConfiguration()
})
</script>

<template>
  <div class="page settings-page">
    <div class="page-heading">
      <div>
        <p class="eyebrow">{{ t('settings.eyebrow') }}</p>
        <h1>{{ t('settings.title') }}</h1>
        <p class="subtitle">{{ t('settings.subtitle') }}</p>
      </div>
    </div>

    <p v-if="error" class="alert error-banner" role="alert">{{ error }}</p>

    <div v-if="loading" class="surface-card settings-loading">{{ t('settings.loading') }}</div>

    <div v-else class="settings-layout">
      <aside class="surface-card settings-nav" :aria-label="t('settings.navigation')">
        <button class="settings-nav-item" :class="{ active: activeSection === 'account' }" type="button" :aria-current="activeSection === 'account' ? 'page' : undefined" @click="activeSection = 'account'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="3.5" /><path d="M5 20c.7-4 3-6 7-6s6.3 2 7 6" /></svg>
          </span>
          <span><strong>{{ t('settings.accountSecurity') }}</strong><small>{{ t('settings.accountSecurityHint') }}</small></span>
        </button>
        <button class="settings-nav-item" :class="{ active: activeSection === 'display' }" type="button" :aria-current="activeSection === 'display' ? 'page' : undefined" @click="activeSection = 'display'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="13" rx="2" /><path d="M8 21h8M12 17v4" /></svg>
          </span>
          <span><strong>{{ t('settings.displayRegional') }}</strong><small>{{ t('settings.displayRegionalHint') }}</small></span>
        </button>
        <button class="settings-nav-item" :class="{ active: activeSection === 'notifications' }" type="button" :aria-current="activeSection === 'notifications' ? 'page' : undefined" @click="activeSection = 'notifications'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></svg>
          </span>
          <span><strong>{{ t('settings.notifications') }}</strong><small>{{ t('settings.notificationsHint') }}</small></span>
        </button>
        <button v-if="isAdmin" class="settings-nav-item" :class="{ active: activeSection === 'inventory' }" type="button" :aria-current="activeSection === 'inventory' ? 'page' : undefined" @click="activeSection = 'inventory'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h16v13H4z" /><path d="m4 7 3-4h10l3 4M9 11h6" /></svg>
          </span>
          <span><strong>{{ t('settings.inventory') }}</strong><small>{{ t('settings.inventoryHint') }}</small></span>
        </button>
        <button v-if="isAdmin" class="settings-nav-item" :class="{ active: activeSection === 'company' }" type="button" :aria-current="activeSection === 'company' ? 'page' : undefined" @click="activeSection = 'company'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 21V4h11v17M15 9h5v12M8 8h3M8 12h3M8 16h3M18 13h.01M18 17h.01" /></svg>
          </span>
          <span><strong>{{ t('settings.company') }}</strong><small>{{ t('settings.companyHint') }}</small></span>
        </button>
        <button v-if="isAdmin" class="settings-nav-item" :class="{ active: activeSection === 'users' }" type="button" :aria-current="activeSection === 'users' ? 'page' : undefined" @click="activeSection = 'users'">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3" /><path d="M3.5 20c.6-3.6 2.4-5.5 5.5-5.5s4.9 1.9 5.5 5.5M16 11a3 3 0 1 0 0-6M16 14.5c2.8 0 4.5 1.8 5 5.5" /></svg>
          </span>
          <span><strong>{{ t('settings.users') }}</strong><small>{{ t('settings.usersHint') }}</small></span>
        </button>
      </aside>

      <main v-if="activeSection === 'account'" class="settings-content">
        <section class="surface-card account-summary">
          <div class="account-avatar" aria-hidden="true">{{ initials }}</div>
          <div class="account-summary-copy">
            <span class="account-kicker">{{ t('settings.activeAccount') }}</span>
            <h2>{{ initialFullName || t('app.defaultName') }}</h2>
            <p>{{ initialEmail }}</p>
          </div>
          <span class="role-badge">{{ role }}</span>
        </section>

        <section class="surface-card settings-card">
          <div class="settings-card-head">
            <div>
              <h2>{{ t('settings.profileTitle') }}</h2>
              <p>{{ t('settings.profileDescription') }}</p>
            </div>
            <span class="settings-card-icon blue" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="3.5" /><path d="M5 20c.7-4 3-6 7-6s6.3 2 7 6" /></svg>
            </span>
          </div>

          <form class="profile-form" :aria-busy="saving" @submit.prevent="saveProfile">
            <div class="profile-fields">
              <label class="field-label">
                {{ t('settings.fullName') }}
                <input v-model="fullName" name="name" autocomplete="name" maxlength="160" required :disabled="saving">
                <small class="field-hint">{{ t('settings.fullNameHint') }}</small>
              </label>

              <label class="field-label">
                {{ t('settings.email') }}
                <input v-model="email" name="email" type="email" autocomplete="email" maxlength="254" required :disabled="saving">
                <small class="field-hint">{{ t('settings.emailHint') }}</small>
              </label>
            </div>

            <Transition name="email-password">
              <div v-if="emailChanged" class="email-verification">
                <span class="verification-icon" aria-hidden="true">i</span>
                <div class="verification-copy">
                  <strong>{{ t('settings.verifyEmailChange') }}</strong>
                  <p>{{ t('settings.verifyEmailChangeHint') }}</p>
                  <label class="field-label">
                    {{ t('changePassword.currentPassword') }}
                    <div class="settings-password-field">
                      <input v-model="currentPassword" :type="showCurrentPassword ? 'text' : 'password'" autocomplete="current-password" required :disabled="saving">
                      <button type="button" :aria-label="showCurrentPassword ? t('changePassword.hideCurrentPassword') : t('changePassword.showCurrentPassword')" :aria-pressed="showCurrentPassword" @click="showCurrentPassword = !showCurrentPassword">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showCurrentPassword" d="m3 3 18 18" /></svg>
                      </button>
                    </div>
                  </label>
                </div>
              </div>
            </Transition>

            <p v-if="formError" class="alert" role="alert">{{ formError }}</p>

            <div class="profile-actions">
              <button class="secondary" type="button" :disabled="saving || !dirty" @click="resetProfile">{{ t('settings.discard') }}</button>
              <button class="primary" type="submit" :disabled="saving || !dirty">
                <span v-if="saving" class="button-spinner" aria-hidden="true" />
                {{ saving ? t('settings.savingProfile') : t('settings.saveProfile') }}
              </button>
            </div>
          </form>
        </section>

        <section class="surface-card settings-card">
          <div class="settings-card-head">
            <div>
              <h2>{{ t('settings.accessTitle') }}</h2>
              <p>{{ t('settings.accessDescription') }}</p>
            </div>
            <span class="settings-card-icon teal" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3 5 6v5c0 4.6 2.7 8 7 10 4.3-2 7-5.4 7-10V6l-7-3Z" /><path d="m9.5 12 1.6 1.6 3.5-3.7" /></svg>
            </span>
          </div>
          <div class="access-row">
            <div><span>{{ t('settings.role') }}</span><strong>{{ role }}</strong><p>{{ t('settings.roleHint') }}</p></div>
            <span class="access-status"><i />{{ t('settings.active') }}</span>
          </div>
        </section>

        <section class="surface-card settings-card security-card">
          <div class="settings-card-head">
            <div>
              <h2>{{ t('settings.securityTitle') }}</h2>
              <p>{{ t('settings.securityDescription') }}</p>
            </div>
            <span class="settings-card-icon amber" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="10" width="14" height="10" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></svg>
            </span>
          </div>

          <div class="security-list">
            <div class="security-row">
              <span class="security-row-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M7 11h10v9H7z" /><path d="M9 11V8a3 3 0 0 1 6 0v3" /><path d="M12 15v2" /></svg>
              </span>
              <div><strong>{{ t('settings.passwordTitle') }}</strong><p>{{ t('settings.passwordDescription') }}</p></div>
              <button class="secondary" type="button" @click="changePasswordOpen = true">{{ t('settings.changePassword') }}</button>
            </div>

            <div class="security-row danger-row">
              <span class="security-row-icon danger" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M10 5H5v14h5" /><path d="M14 8l4 4-4 4" /><path d="M9 12h9" /></svg>
              </span>
              <div><strong>{{ t('settings.logoutAllTitle') }}</strong><p>{{ t('settings.logoutAllDescription') }}</p></div>
              <button v-if="!confirmLogoutAll" class="danger-button" type="button" @click="confirmLogoutAll = true">{{ t('settings.logoutAll') }}</button>
            </div>

            <div v-if="confirmLogoutAll" class="logout-confirmation" role="alert">
              <div><strong>{{ t('settings.confirmLogoutAllTitle') }}</strong><p>{{ t('settings.confirmLogoutAllDescription') }}</p></div>
              <div class="logout-confirmation-actions">
                <button class="secondary" type="button" :disabled="revoking" @click="confirmLogoutAll = false">{{ t('common.cancel') }}</button>
                <button class="danger-button solid" type="button" :disabled="revoking" @click="logoutAll">
                  <span v-if="revoking" class="danger-spinner" aria-hidden="true" />
                  {{ revoking ? t('settings.revokingSessions') : t('settings.confirmLogoutAll') }}
                </button>
              </div>
            </div>
          </div>
          <p v-if="securityError" class="alert security-error" role="alert">{{ securityError }}</p>
        </section>
      </main>

      <main v-else-if="activeSection === 'display'" class="settings-content">
        <section class="surface-card display-summary">
          <div class="display-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3a9 9 0 1 0 9 9c0-1.1-.9-2-2-2h-1.3a2 2 0 0 1-1.7-3l.2-.4A2.4 2.4 0 0 0 14.1 3H12Z" /><circle cx="7.5" cy="11" r=".7" fill="currentColor" /><circle cx="10" cy="7" r=".7" fill="currentColor" /><circle cx="8.5" cy="15" r=".7" fill="currentColor" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.displayRegional') }}</span>
            <h2>{{ t('settings.displaySummaryTitle') }}</h2>
            <p>{{ t('settings.displaySummaryDescription') }}</p>
          </div>
          <span class="autosave-badge"><i />{{ t('settings.savedAutomatically') }}</span>
        </section>

        <section class="surface-card settings-card">
          <div class="settings-card-head">
            <div>
              <h2>{{ t('settings.appearanceTitle') }}</h2>
              <p>{{ t('settings.appearanceDescription') }}</p>
            </div>
            <span class="settings-card-icon blue" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 5h16M8 3v4M16 3v4" /><path d="M6 10h12v10H6z" /></svg>
            </span>
          </div>

          <div class="preference-body">
            <div class="preference-group">
              <div class="preference-label"><strong>{{ t('settings.languageTitle') }}</strong><p>{{ t('settings.languageDescription') }}</p></div>
              <div class="language-options" role="group" :aria-label="t('settings.languageTitle')">
                <button type="button" :class="{ active: language === 'en' }" :aria-pressed="language === 'en'" @click="changeLanguage('en')"><span>EN</span><strong>English</strong></button>
                <button type="button" :class="{ active: language === 'id' }" :aria-pressed="language === 'id'" @click="changeLanguage('id')"><span>ID</span><strong>Bahasa Indonesia</strong></button>
              </div>
            </div>

            <div class="preference-group">
              <div class="preference-label"><strong>{{ t('settings.themeTitle') }}</strong><p>{{ t('settings.themeDescription') }}</p></div>
              <div class="theme-options" role="group" :aria-label="t('settings.themeTitle')">
                <button type="button" :class="{ active: themePreference === 'system' }" :aria-pressed="themePreference === 'system'" @click="changeTheme('system')">
                  <span class="theme-swatch system-swatch"><i /><i /></span><strong>{{ t('settings.themeSystem') }}</strong><small>{{ t('settings.themeSystemHint') }}</small>
                </button>
                <button type="button" :class="{ active: themePreference === 'light' }" :aria-pressed="themePreference === 'light'" @click="changeTheme('light')">
                  <span class="theme-swatch light-swatch"><i /><i /></span><strong>{{ t('settings.themeLight') }}</strong><small>{{ t('settings.themeLightHint') }}</small>
                </button>
                <button type="button" :class="{ active: themePreference === 'dark' }" :aria-pressed="themePreference === 'dark'" @click="changeTheme('dark')">
                  <span class="theme-swatch dark-swatch"><i /><i /></span><strong>{{ t('settings.themeDark') }}</strong><small>{{ t('settings.themeDarkHint') }}</small>
                </button>
              </div>
              <p class="resolved-theme">{{ t('settings.activeTheme', { theme: resolvedTheme === 'dark' ? t('settings.themeDark') : t('settings.themeLight') }) }}</p>
            </div>
          </div>
        </section>

        <section class="surface-card settings-card">
          <div class="settings-card-head">
            <div>
              <h2>{{ t('settings.regionalTitle') }}</h2>
              <p>{{ t('settings.regionalDescription') }}</p>
            </div>
            <span class="settings-card-icon teal" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9" /><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18" /></svg>
            </span>
          </div>

          <div class="regional-grid">
            <label class="field-label">
              {{ t('settings.timeZone') }}
              <select :value="timeZone" @change="changeTimeZone">
                <option value="system">{{ t('settings.timeZoneSystem', { zone: systemTimeZone }) }}</option>
                <option value="Asia/Jakarta">WIB · Asia/Jakarta (UTC+7)</option>
                <option value="Asia/Makassar">WITA · Asia/Makassar (UTC+8)</option>
                <option value="Asia/Jayapura">WIT · Asia/Jayapura (UTC+9)</option>
                <option value="UTC">UTC</option>
              </select>
              <small class="field-hint">{{ t('settings.timeZoneHint') }}</small>
            </label>

            <label class="field-label">
              {{ t('settings.dateFormat') }}
              <select :value="dateFormat" @change="changeDateFormat">
                <option value="regional">{{ t('settings.formatRegional') }}</option>
                <option value="dmy">DD MMM YYYY</option>
                <option value="mdy">MMM DD, YYYY</option>
                <option value="ymd">YYYY-MM-DD</option>
              </select>
              <small class="field-hint">{{ t('settings.dateFormatHint') }}</small>
            </label>

            <label class="field-label">
              {{ t('settings.numberFormat') }}
              <select :value="numberFormat" @change="changeNumberFormat">
                <option value="regional">{{ t('settings.formatRegional') }}</option>
                <option value="id-ID">Indonesia · 1.234.567,89</option>
                <option value="en-US">English (US) · 1,234,567.89</option>
              </select>
              <small class="field-hint">{{ t('settings.numberFormatHint') }}</small>
            </label>

            <label class="field-label">
              {{ t('settings.defaultRows') }}
              <select :value="defaultPageSize" @change="changeDefaultPageSize">
                <option v-for="size in [5, 10, 25, 50, 100]" :key="size" :value="size">{{ t('settings.rowsValue', { count: size }) }}</option>
              </select>
              <small class="field-hint">{{ t('settings.defaultRowsHint') }}</small>
            </label>
          </div>

          <div class="format-preview">
            <div class="format-preview-head"><span>{{ t('settings.livePreview') }}</span><small>{{ t('settings.livePreviewHint') }}</small></div>
            <dl>
              <div><dt>{{ t('settings.previewDateTime') }}</dt><dd>{{ datePreview }}</dd></div>
              <div><dt>{{ t('settings.previewNumber') }}</dt><dd>{{ numberPreview }}</dd></div>
              <div><dt>{{ t('settings.previewCurrency') }}</dt><dd>{{ currencyPreview }}</dd></div>
            </dl>
          </div>
        </section>
      </main>

      <main v-else-if="activeSection === 'notifications'" class="settings-content">
        <section class="surface-card notification-summary">
          <div class="notification-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.notifications') }}</span>
            <h2>{{ t('settings.notificationSummaryTitle') }}</h2>
            <p>{{ t('settings.notificationSummaryDescription') }}</p>
          </div>
          <span class="notification-status-badge" :class="{ paused: !notificationPreferences.inAppEnabled }">
            <i />{{ notificationPreferences.inAppEnabled ? t('settings.notificationsActive') : t('settings.notificationsPaused') }}
          </span>
        </section>

        <p v-if="notificationError" class="alert error-banner" role="alert">{{ notificationError }}</p>
        <div v-if="notificationLoading" class="surface-card settings-loading">{{ t('settings.loadingNotifications') }}</div>

        <template v-else>
          <section class="surface-card settings-card">
            <div class="settings-card-head">
              <div>
                <h2>{{ t('settings.deliveryTitle') }}</h2>
                <p>{{ t('settings.deliveryDescription') }}</p>
              </div>
              <span class="settings-card-icon blue" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="3" width="14" height="18" rx="2" /><path d="M9 6h6M10 18h4" /></svg>
              </span>
            </div>

            <div class="notification-settings-list">
              <div class="notification-setting-row primary-notification-row">
                <span class="notification-setting-icon blue" aria-hidden="true">◔</span>
                <div><strong>{{ t('settings.inAppTitle') }}</strong><p>{{ t('settings.inAppDescription') }}</p></div>
                <button class="preference-switch" :class="{ active: notificationPreferences.inAppEnabled }" type="button" role="switch" :aria-checked="notificationPreferences.inAppEnabled" :aria-label="t('settings.inAppTitle')" :disabled="notificationSaving" @click="toggleNotificationPreference('inAppEnabled')"><span /></button>
              </div>

              <div class="notification-setting-row" :class="{ muted: !notificationPreferences.inAppEnabled }">
                <span class="notification-setting-icon teal" aria-hidden="true">♪</span>
                <div><strong>{{ t('settings.soundTitle') }}</strong><p>{{ t('settings.soundDescription') }}</p></div>
                <button class="preference-switch" :class="{ active: notificationPreferences.soundEnabled }" type="button" role="switch" :aria-checked="notificationPreferences.soundEnabled" :aria-label="t('settings.soundTitle')" :disabled="notificationSaving || !notificationPreferences.inAppEnabled" @click="toggleNotificationPreference('soundEnabled')"><span /></button>
              </div>

              <div class="notification-setting-row" :class="{ muted: !notificationPreferences.inAppEnabled }">
                <span class="notification-setting-icon amber" aria-hidden="true">↻</span>
                <div><strong>{{ t('settings.refreshTitle') }}</strong><p>{{ t('settings.refreshDescription') }}</p></div>
                <select class="notification-interval" :value="notificationPreferences.pollingIntervalSeconds" :aria-label="t('settings.refreshTitle')" :disabled="notificationSaving || !notificationPreferences.inAppEnabled" @change="changeNotificationInterval">
                  <option :value="15">{{ t('settings.refreshSeconds', { count: 15 }) }}</option>
                  <option :value="30">{{ t('settings.refreshSeconds', { count: 30 }) }}</option>
                  <option :value="60">{{ t('settings.refreshMinute') }}</option>
                  <option :value="300">{{ t('settings.refreshMinutes', { count: 5 }) }}</option>
                </select>
              </div>
            </div>
          </section>

          <section class="surface-card settings-card">
            <div class="settings-card-head">
              <div>
                <h2>{{ t('settings.categoriesTitle') }}</h2>
                <p>{{ t('settings.categoriesDescription') }}</p>
              </div>
              <span class="settings-card-icon teal" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 6h16M4 12h16M4 18h16" /><circle cx="7" cy="6" r="1" fill="currentColor" /><circle cx="13" cy="12" r="1" fill="currentColor" /><circle cx="17" cy="18" r="1" fill="currentColor" /></svg>
              </span>
            </div>

            <div class="notification-category-grid" :class="{ muted: !notificationPreferences.inAppEnabled }">
              <article class="notification-category-card">
                <span class="category-icon amber" aria-hidden="true">!</span>
                <div><strong>{{ t('settings.lowStockTitle') }}</strong><p>{{ t('settings.lowStockDescription') }}</p><small>{{ t('settings.inventoryCategory') }}</small></div>
                <button class="preference-switch" :class="{ active: notificationPreferences.lowStockEnabled }" type="button" role="switch" :aria-checked="notificationPreferences.lowStockEnabled" :aria-label="t('settings.lowStockTitle')" :disabled="notificationSaving || !notificationPreferences.inAppEnabled" @click="toggleNotificationPreference('lowStockEnabled')"><span /></button>
              </article>

              <article class="notification-category-card">
                <span class="category-icon teal" aria-hidden="true">↓</span>
                <div><strong>{{ t('settings.reportReadyTitle') }}</strong><p>{{ t('settings.reportReadyDescription') }}</p><small>{{ t('settings.reportsCategory') }}</small></div>
                <button class="preference-switch" :class="{ active: notificationPreferences.reportReadyEnabled }" type="button" role="switch" :aria-checked="notificationPreferences.reportReadyEnabled" :aria-label="t('settings.reportReadyTitle')" :disabled="notificationSaving || !notificationPreferences.inAppEnabled" @click="toggleNotificationPreference('reportReadyEnabled')"><span /></button>
              </article>

              <article class="notification-category-card">
                <span class="category-icon blue" aria-hidden="true">i</span>
                <div><strong>{{ t('settings.systemTitle') }}</strong><p>{{ t('settings.systemDescription') }}</p><small>{{ t('settings.systemCategory') }}</small></div>
                <button class="preference-switch" :class="{ active: notificationPreferences.systemEnabled }" type="button" role="switch" :aria-checked="notificationPreferences.systemEnabled" :aria-label="t('settings.systemTitle')" :disabled="notificationSaving || !notificationPreferences.inAppEnabled" @click="toggleNotificationPreference('systemEnabled')"><span /></button>
              </article>
            </div>

            <div class="notification-footnote">
              <span aria-hidden="true">✓</span>
              <p><strong>{{ t('settings.accountSyncedTitle') }}</strong>{{ t('settings.accountSyncedDescription', { count: enabledNotificationCategories }) }}</p>
              <small>{{ notificationSaving ? t('settings.savingNotifications') : t('settings.savedNotifications') }}</small>
            </div>
          </section>
        </template>
      </main>

      <main v-else-if="activeSection === 'inventory' && isAdmin" class="settings-content">
        <section class="surface-card inventory-summary">
          <div class="inventory-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h16v13H4z" /><path d="m4 7 3-4h10l3 4M9 11h6" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.adminConfiguration') }}</span>
            <h2>{{ t('settings.inventorySummaryTitle') }}</h2>
            <p>{{ t('settings.inventorySummaryDescription') }}</p>
          </div>
          <span class="admin-badge">{{ t('settings.adminOnly') }}</span>
        </section>

        <p v-if="inventoryError" class="alert error-banner" role="alert">{{ inventoryError }}</p>
        <div v-if="inventoryLoading" class="surface-card settings-loading">{{ t('settings.loadingInventory') }}</div>

        <form v-else class="inventory-settings-form" :aria-busy="inventorySaving" @submit.prevent="saveInventoryConfiguration">
          <section class="surface-card settings-card">
            <div class="settings-card-head">
              <div>
                <h2>{{ t('settings.productDefaultsTitle') }}</h2>
                <p>{{ t('settings.productDefaultsDescription') }}</p>
              </div>
              <span class="settings-card-icon blue" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M5 7h14v13H5z" /><path d="m5 7 3-4h8l3 4M9 11h6" /></svg>
              </span>
            </div>

            <div class="inventory-fields">
              <label class="field-label">
                {{ t('settings.defaultReorderLevel') }}
                <FormattedNumberInput v-model="inventoryDraft.defaultReorderLevel" :decimal-scale="2" :disabled="inventorySaving" required />
                <small class="field-hint">{{ t('settings.defaultReorderLevelHint') }}</small>
              </label>
              <label class="field-label">
                {{ t('settings.defaultUnit') }}
                <input v-model.trim="inventoryDraft.defaultUnit" maxlength="24" :disabled="inventorySaving" required>
                <small class="field-hint">{{ t('settings.defaultUnitHint') }}</small>
              </label>
            </div>
          </section>

          <section class="surface-card settings-card">
            <div class="settings-card-head">
              <div>
                <h2>{{ t('settings.stockPolicyTitle') }}</h2>
                <p>{{ t('settings.stockPolicyDescription') }}</p>
              </div>
              <span class="settings-card-icon amber" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3 4 7v5c0 4.5 2.6 7.7 8 9 5.4-1.3 8-4.5 8-9V7l-8-4Z" /><path d="M12 8v5M12 17h.01" /></svg>
              </span>
            </div>

            <div class="inventory-policy-list">
              <div class="inventory-policy-row">
                <span class="notification-setting-icon amber" aria-hidden="true">−</span>
                <div><strong>{{ t('settings.allowNegativeStock') }}</strong><p>{{ t('settings.allowNegativeStockHint') }}</p></div>
                <button class="preference-switch" :class="{ active: inventoryDraft.allowNegativeStock }" type="button" role="switch" :aria-checked="inventoryDraft.allowNegativeStock" :aria-label="t('settings.allowNegativeStock')" :disabled="inventorySaving" @click="inventoryDraft.allowNegativeStock = !inventoryDraft.allowNegativeStock"><span /></button>
              </div>

              <label class="inventory-threshold-row field-label">
                <span class="notification-setting-icon teal" aria-hidden="true">!</span>
                <span><strong>{{ t('settings.globalLowStockThreshold') }}</strong><small>{{ t('settings.globalLowStockThresholdHint') }}</small></span>
                <FormattedNumberInput v-model="inventoryDraft.globalLowStockThreshold" class="inventory-number-input" :decimal-scale="2" :disabled="inventorySaving" required />
              </label>
            </div>

            <div v-if="inventoryDraft.allowNegativeStock" class="inventory-warning" role="status">
              <span aria-hidden="true">!</span>
              <p><strong>{{ t('settings.negativeStockWarningTitle') }}</strong>{{ t('settings.negativeStockWarningDescription') }}</p>
            </div>
          </section>

          <div class="inventory-actions">
            <p>{{ t('settings.inventorySaveHint') }}</p>
            <div>
              <button class="secondary" type="button" :disabled="inventorySaving || !inventoryDirty" @click="resetInventoryConfiguration">{{ t('settings.discard') }}</button>
              <button class="primary" type="submit" :disabled="inventorySaving || !inventoryDirty">
                <span v-if="inventorySaving" class="button-spinner" aria-hidden="true" />
                {{ inventorySaving ? t('settings.savingInventory') : t('settings.saveInventory') }}
              </button>
            </div>
          </div>
        </form>
      </main>

      <main v-else-if="activeSection === 'company' && isAdmin" class="settings-content">
        <section class="surface-card inventory-summary company-summary">
          <div class="inventory-summary-icon company-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 21V4h11v17M15 9h5v12M8 8h3M8 12h3M8 16h3M18 13h.01M18 17h.01" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.adminConfiguration') }}</span>
            <h2>{{ t('settings.companySummaryTitle') }}</h2>
            <p>{{ t('settings.companySummaryDescription') }}</p>
          </div>
          <span class="admin-badge">{{ t('settings.adminOnly') }}</span>
        </section>

        <p v-if="companyError" class="alert error-banner" role="alert">{{ companyError }}</p>
        <div v-if="companyLoading" class="surface-card settings-loading">{{ t('settings.loadingCompany') }}</div>
        <form v-else class="company-settings-form" :aria-busy="companySaving" @submit.prevent="saveCompanyConfiguration">
          <section class="surface-card settings-card">
            <div class="settings-card-head"><div><h2>{{ t('settings.companyIdentityTitle') }}</h2><p>{{ t('settings.companyIdentityDescription') }}</p></div><span class="settings-card-icon blue" aria-hidden="true">⌂</span></div>
            <div class="company-fields">
              <label class="field-label">{{ t('settings.companyName') }}<input v-model="companyDraft.name" maxlength="160" required :disabled="companySaving"><small class="field-hint">{{ t('settings.companyNameHint') }}</small></label>
              <label class="field-label">{{ t('settings.companyCurrency') }}<select v-model="companyDraft.currency" :disabled="companySaving"><option value="IDR">IDR — Rupiah Indonesia</option><option value="USD">USD — US Dollar</option><option value="SGD">SGD — Singapore Dollar</option><option value="MYR">MYR — Malaysian Ringgit</option><option value="EUR">EUR — Euro</option></select><small class="field-hint">{{ t('settings.companyCurrencyHint') }}</small></label>
              <label class="field-label company-full-field">{{ t('settings.companyAddress') }}<textarea v-model="companyDraft.address" maxlength="300" rows="3" :disabled="companySaving" /><small class="field-hint">{{ t('settings.companyAddressHint') }}</small></label>
            </div>
          </section>

          <section class="surface-card settings-card">
            <div class="settings-card-head"><div><h2>{{ t('settings.companyContactTitle') }}</h2><p>{{ t('settings.companyContactDescription') }}</p></div><span class="settings-card-icon teal" aria-hidden="true">@</span></div>
            <div class="company-fields">
              <label class="field-label">{{ t('settings.companyEmail') }}<input v-model="companyDraft.email" type="email" maxlength="254" :disabled="companySaving"><small class="field-hint">{{ t('settings.companyEmailHint') }}</small></label>
              <label class="field-label">{{ t('settings.companyPhone') }}<input v-model="companyDraft.phone" maxlength="40" :disabled="companySaving"><small class="field-hint">{{ t('settings.companyPhoneHint') }}</small></label>
              <label class="field-label company-full-field">{{ t('settings.companyLogoUrl') }}<input v-model="companyDraft.logoUrl" type="url" maxlength="1000" placeholder="https://..." :disabled="companySaving"><small class="field-hint">{{ t('settings.companyLogoHint') }}</small></label>
            </div>
            <div v-if="companyDraft.logoUrl" class="company-logo-preview"><img :src="companyDraft.logoUrl" :alt="companyDraft.name" @error="($event.target as HTMLImageElement).style.display = 'none'"><span>{{ t('settings.companyLogoPreview') }}</span></div>
          </section>

          <div class="inventory-actions"><p>{{ t('settings.companySaveHint') }}</p><div><button class="secondary" type="button" :disabled="companySaving || !companyDirty" @click="resetCompanyConfiguration">{{ t('settings.discard') }}</button><button class="primary" type="submit" :disabled="companySaving || !companyDirty"><span v-if="companySaving" class="button-spinner" aria-hidden="true" />{{ companySaving ? t('settings.savingCompany') : t('settings.saveCompany') }}</button></div></div>
        </form>
      </main>

      <main v-else-if="activeSection === 'users' && isAdmin" class="settings-content">
        <section class="surface-card inventory-summary users-summary">
          <div class="inventory-summary-icon users-summary-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="9" cy="8" r="3" /><path d="M3.5 20c.6-3.6 2.4-5.5 5.5-5.5s4.9 1.9 5.5 5.5M16 11a3 3 0 1 0 0-6M16 14.5c2.8 0 4.5 1.8 5 5.5" /></svg>
          </div>
          <div>
            <span class="account-kicker">{{ t('settings.adminConfiguration') }}</span>
            <h2>{{ t('settings.usersSummaryTitle') }}</h2>
            <p>{{ t('settings.usersSummaryDescription') }}</p>
          </div>
          <button class="primary users-add-button" type="button" @click="openUserEditor()"><span class="button-plus">+</span>{{ t('settings.addUser') }}</button>
        </section>

        <p v-if="usersError" class="alert error-banner" role="alert">{{ usersError }}</p>

        <section v-if="userEditorOpen" class="surface-card settings-card user-editor-card">
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
          <div v-else class="table-wrap users-table-wrap"><table><thead><tr><th>{{ t('settings.userIdentity') }}</th><th>{{ t('settings.userRole') }}</th><th>{{ t('settings.userStatus') }}</th><th>{{ t('settings.userCreated') }}</th><th>{{ t('settings.userActions') }}</th></tr></thead><tbody><tr v-for="user in managedUsers.items" :key="user.id"><td><strong>{{ user.fullName }}</strong><small>{{ user.email }}</small></td><td><span class="role-pill" :class="`role-${user.role.toLowerCase()}`">{{ user.role }}</span></td><td><span class="badge" :class="user.isActive ? 'ok' : 'neutral'">{{ user.isActive ? t('settings.active') : t('settings.inactive') }}</span></td><td class="date-cell">{{ formatDate(user.createdAt) }}</td><td><button class="secondary compact-action" type="button" @click="openUserEditor(user)">{{ t('settings.editUser') }}</button></td></tr></tbody></table></div>
          <PaginationControls v-if="!usersLoading && managedUsers.items.length" :page="managedUsers.page" :page-size="managedUsers.pageSize" :total-count="managedUsers.totalCount" :total-pages="managedUsers.totalPages" @page-change="changeUsersPage" @page-size-change="changeUsersPageSize" />
        </section>

        <section class="surface-card settings-card permissions-card">
          <div class="settings-card-head"><div><h2>{{ t('settings.permissionsTitle') }}</h2><p>{{ t('settings.permissionsDescription') }}</p></div><span class="settings-card-icon teal" aria-hidden="true">✓</span></div>
          <div class="permissions-grid"><article><span class="role-pill role-admin">Admin</span><strong>{{ t('settings.adminPermissionsTitle') }}</strong><p>{{ t('settings.adminPermissionsDescription') }}</p></article><article><span class="role-pill role-manager">Manager</span><strong>{{ t('settings.managerPermissionsTitle') }}</strong><p>{{ t('settings.managerPermissionsDescription') }}</p></article><article><span class="role-pill role-staff">Staff</span><strong>{{ t('settings.staffPermissionsTitle') }}</strong><p>{{ t('settings.staffPermissionsDescription') }}</p></article></div>
        </section>
      </main>
    </div>

    <ChangePasswordModal v-if="changePasswordOpen" @close="changePasswordOpen = false" />
  </div>
</template>

<style scoped>
.settings-page { max-width: 1260px; }
.settings-layout { display: grid; grid-template-columns: 235px minmax(0, 1fr); align-items: start; gap: 16px; }
.settings-nav { position: sticky; top: 94px; padding: 7px; }
.settings-nav-item { display: flex; width: 100%; align-items: center; gap: 10px; padding: 11px; border-radius: 9px; color: var(--control-text); background: transparent; text-align: left; }
.settings-nav-item + .settings-nav-item { margin-top: 3px; }
.settings-nav-item:hover { background: var(--surface-hover); }
.settings-nav-item.active { color: var(--blue); background: var(--blue-soft); }
.settings-nav-icon { display: grid; width: 30px; height: 30px; flex: 0 0 30px; place-items: center; border-radius: 8px; background: color-mix(in srgb, var(--blue) 10%, var(--surface-raised)); }
.settings-nav-icon svg { width: 17px; height: 17px; }
.settings-nav-item strong, .settings-nav-item small { display: block; }
.settings-nav-item strong { font-size: .67rem; }
.settings-nav-item small { margin-top: 3px; color: var(--muted); font-size: .56rem; line-height: 1.35; }
.settings-content { display: grid; gap: 14px; }
.settings-loading { padding: 46px 20px; color: var(--muted); text-align: center; }
.account-summary { display: flex; min-height: 106px; align-items: center; gap: 14px; padding: 20px; background: linear-gradient(125deg, color-mix(in srgb, var(--blue) 7%, var(--surface)) 0%, var(--surface) 48%, color-mix(in srgb, var(--teal) 6%, var(--surface)) 100%); }
.account-avatar { display: grid; width: 58px; height: 58px; flex: 0 0 58px; place-items: center; border: 1px solid color-mix(in srgb, var(--blue) 16%, var(--line)); border-radius: 16px; color: var(--blue); background: var(--blue-soft); font-size: 1rem; font-weight: 800; box-shadow: inset 0 0 0 4px color-mix(in srgb, var(--surface) 68%, transparent); }
.account-summary-copy { min-width: 0; flex: 1; }
.account-kicker { color: var(--muted-2); font-size: .55rem; font-weight: 800; letter-spacing: .1em; text-transform: uppercase; }
.account-summary-copy h2 { margin-top: 5px; font-size: 1.15rem; }
.account-summary-copy p { margin-top: 4px; overflow: hidden; color: var(--muted); font-size: .66rem; text-overflow: ellipsis; white-space: nowrap; }
.role-badge { padding: 6px 10px; border: 1px solid color-mix(in srgb, var(--teal) 20%, var(--line)); border-radius: 999px; color: var(--teal); background: var(--teal-soft); font-size: .59rem; font-weight: 800; }
.display-summary { display: flex; min-height: 106px; align-items: center; gap: 14px; padding: 20px; background: linear-gradient(125deg, color-mix(in srgb, var(--blue) 7%, var(--surface)) 0%, var(--surface) 48%, color-mix(in srgb, var(--teal) 6%, var(--surface)) 100%); }
.display-summary-icon { display: grid; width: 58px; height: 58px; flex: 0 0 58px; place-items: center; border: 1px solid color-mix(in srgb, var(--blue) 16%, var(--line)); border-radius: 16px; color: var(--blue); background: var(--blue-soft); }
.display-summary-icon svg { width: 28px; height: 28px; }
.display-summary > div:nth-child(2) { min-width: 0; flex: 1; }
.display-summary h2 { margin-top: 5px; font-size: 1.04rem; }
.display-summary p { max-width: 620px; margin-top: 5px; color: var(--muted); font-size: .64rem; line-height: 1.5; }
.autosave-badge { display: inline-flex; flex: 0 0 auto; align-items: center; gap: 7px; padding: 7px 10px; border: 1px solid color-mix(in srgb, var(--teal) 18%, var(--line)); border-radius: 999px; color: var(--teal); background: var(--teal-soft); font-size: .57rem; font-weight: 800; }
.autosave-badge i { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.notification-summary { display: flex; min-height: 106px; align-items: center; gap: 14px; padding: 20px; background: linear-gradient(125deg, color-mix(in srgb, var(--blue) 7%, var(--surface)) 0%, var(--surface) 48%, color-mix(in srgb, var(--amber) 6%, var(--surface)) 100%); }
.notification-summary-icon { display: grid; width: 58px; height: 58px; flex: 0 0 58px; place-items: center; border: 1px solid color-mix(in srgb, var(--blue) 16%, var(--line)); border-radius: 16px; color: var(--blue); background: var(--blue-soft); }
.notification-summary-icon svg { width: 27px; height: 27px; }
.notification-summary > div:nth-child(2) { min-width: 0; flex: 1; }
.notification-summary h2 { margin-top: 5px; font-size: 1.04rem; }
.notification-summary p { max-width: 620px; margin-top: 5px; color: var(--muted); font-size: .64rem; line-height: 1.5; }
.notification-status-badge { display: inline-flex; flex: 0 0 auto; align-items: center; gap: 7px; padding: 7px 10px; border: 1px solid color-mix(in srgb, var(--teal) 18%, var(--line)); border-radius: 999px; color: var(--teal); background: var(--teal-soft); font-size: .57rem; font-weight: 800; }
.notification-status-badge i { width: 6px; height: 6px; border-radius: 50%; background: currentColor; box-shadow: 0 0 0 3px color-mix(in srgb, var(--teal) 13%, transparent); }
.notification-status-badge.paused { border-color: var(--line); color: var(--muted); background: var(--surface-hover); }
.notification-status-badge.paused i { box-shadow: none; }
.inventory-summary { display: flex; min-height: 106px; align-items: center; gap: 14px; padding: 20px; background: linear-gradient(125deg, color-mix(in srgb, var(--blue) 7%, var(--surface)) 0%, var(--surface) 48%, color-mix(in srgb, var(--amber) 7%, var(--surface)) 100%); }
.inventory-summary-icon { display: grid; width: 58px; height: 58px; flex: 0 0 58px; place-items: center; border: 1px solid color-mix(in srgb, var(--amber) 18%, var(--line)); border-radius: 16px; color: var(--amber); background: var(--amber-soft); }
.inventory-summary-icon svg { width: 28px; height: 28px; }
.inventory-summary > div:nth-child(2) { min-width: 0; flex: 1; }
.inventory-summary h2 { margin-top: 5px; font-size: 1.04rem; }
.inventory-summary p { max-width: 620px; margin-top: 5px; color: var(--muted); font-size: .64rem; line-height: 1.5; }
.admin-badge { display: inline-flex; flex: 0 0 auto; align-items: center; padding: 7px 10px; border: 1px solid color-mix(in srgb, var(--amber) 20%, var(--line)); border-radius: 999px; color: var(--amber); background: var(--amber-soft); font-size: .57rem; font-weight: 800; }
.inventory-settings-form { display: grid; gap: 14px; }
.inventory-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; padding: 20px; }
.inventory-settings-form input { width: 100%; }
.inventory-policy-list { padding: 2px 20px; }
.inventory-policy-list > * + * { border-top: 1px solid var(--line); }
.inventory-policy-list > .field-label { margin: 0; }
.inventory-policy-row, .inventory-threshold-row { display: flex; min-height: 76px; align-items: center; gap: 12px; padding: 15px 0; }
.inventory-policy-row > div, .inventory-threshold-row > span:nth-child(2) { min-width: 0; flex: 1; }
.inventory-policy-row strong, .inventory-threshold-row strong { display: block; color: var(--ink); font-size: .69rem; }
.inventory-policy-row p, .inventory-threshold-row small { display: block; margin-top: 4px; color: var(--muted); font-size: .61rem; font-weight: 400; line-height: 1.5; }
.inventory-threshold-row .inventory-number-input { width: 145px; flex: 0 0 145px; }
.inventory-warning { display: flex; align-items: flex-start; gap: 10px; margin: 0 20px 20px; padding: 13px 14px; border: 1px solid color-mix(in srgb, var(--amber) 28%, var(--line)); border-radius: 10px; background: var(--amber-soft); }
.inventory-warning > span { display: grid; width: 22px; height: 22px; flex: 0 0 22px; place-items: center; border-radius: 50%; color: #fff; background: var(--amber); font-size: .62rem; font-weight: 900; }
.inventory-warning p { color: var(--control-text); font-size: .6rem; line-height: 1.5; }
.inventory-warning strong { display: block; margin-bottom: 2px; color: var(--ink); font-size: .65rem; }
.inventory-actions { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 4px 2px 0; }
.inventory-actions > p { color: var(--muted); font-size: .58rem; line-height: 1.45; }
.inventory-actions > div { display: flex; flex: 0 0 auto; gap: 9px; }
.company-settings-form { display: grid; gap: 14px; }
.company-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; padding: 20px; }
.company-fields input, .company-fields select, .company-fields textarea { width: 100%; }
.company-full-field { grid-column: 1 / -1; }
.company-logo-preview { display: flex; align-items: center; gap: 12px; margin: 0 20px 20px; padding: 12px; border: 1px solid var(--line); border-radius: 10px; color: var(--muted); background: var(--surface-hover); font-size: .6rem; }
.company-logo-preview img { width: 52px; height: 52px; object-fit: contain; border-radius: 8px; background: var(--surface-raised); }
.company-summary-icon { color: var(--blue); background: var(--blue-soft); }
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
.settings-card-icon svg { width: 18px; height: 18px; }
.settings-card-icon.blue { color: var(--blue); background: var(--blue-soft); }
.settings-card-icon.teal { color: var(--teal); background: var(--teal-soft); }
.settings-card-icon.amber { color: var(--amber); background: var(--amber-soft); }
.preference-body { padding: 0 20px; }
.preference-group { padding: 19px 0 21px; }
.preference-group + .preference-group { border-top: 1px solid var(--line); }
.preference-label { margin-bottom: 13px; }
.preference-label strong { color: var(--ink); font-size: .7rem; }
.preference-label p { margin-top: 4px; color: var(--muted); font-size: .61rem; line-height: 1.5; }
.language-options { display: grid; max-width: 520px; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 9px; }
.language-options button { display: flex; align-items: center; gap: 10px; min-height: 49px; padding: 8px 10px; border: 1px solid var(--line); border-radius: 10px; color: var(--control-text); background: var(--surface-raised); text-align: left; }
.language-options button:hover, .theme-options button:hover { border-color: color-mix(in srgb, var(--blue) 34%, var(--line)); }
.language-options button.active, .theme-options button.active { border-color: color-mix(in srgb, var(--blue) 56%, var(--line)); background: var(--blue-soft); box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--blue) 10%, transparent); }
.language-options button > span { display: grid; width: 30px; height: 30px; place-items: center; border-radius: 8px; color: var(--blue); background: color-mix(in srgb, var(--blue) 11%, var(--surface)); font-size: .59rem; font-weight: 900; }
.language-options button strong { font-size: .65rem; }
.theme-options { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 9px; }
.theme-options button { display: grid; grid-template-columns: 64px 1fr; grid-template-rows: auto auto; gap: 3px 10px; align-items: center; min-height: 72px; padding: 10px; border: 1px solid var(--line); border-radius: 10px; color: var(--control-text); background: var(--surface-raised); text-align: left; }
.theme-options button > strong { align-self: end; font-size: .65rem; }
.theme-options button > small { align-self: start; color: var(--muted); font-size: .55rem; }
.theme-swatch { position: relative; display: block; width: 64px; height: 48px; grid-row: 1 / 3; overflow: hidden; border: 1px solid color-mix(in srgb, var(--muted) 22%, var(--line)); border-radius: 7px; background: #f7f9fc; }
.theme-swatch::before { position: absolute; inset: 0 auto 0 0; width: 18px; content: ''; background: #e6ebf2; }
.theme-swatch i:first-child { position: absolute; top: 8px; right: 7px; width: 31px; height: 6px; border-radius: 2px; background: #d1d8e3; }
.theme-swatch i:last-child { position: absolute; top: 19px; right: 7px; width: 31px; height: 20px; border-radius: 3px; background: #e7ebf1; }
.dark-swatch { background: #172131; }
.dark-swatch::before { background: #101827; }
.dark-swatch i:first-child { background: #47617f; }
.dark-swatch i:last-child { background: #24344a; }
.system-swatch { background: linear-gradient(135deg, #f7f9fc 0 50%, #172131 50%); }
.system-swatch::before { background: linear-gradient(135deg, #e6ebf2 0 50%, #101827 50%); }
.resolved-theme { margin-top: 10px; color: var(--muted-2); font-size: .56rem; }
.regional-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; padding: 20px; }
.regional-grid select { width: 100%; }
.format-preview { margin: 0 20px 20px; padding: 15px; border: 1px solid color-mix(in srgb, var(--blue) 16%, var(--line)); border-radius: 11px; background: color-mix(in srgb, var(--blue) 4%, var(--surface-raised)); }
.format-preview-head { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; padding-bottom: 11px; border-bottom: 1px solid var(--line); }
.format-preview-head span { color: var(--ink); font-size: .66rem; font-weight: 800; }
.format-preview-head small { color: var(--muted-2); font-size: .55rem; }
.format-preview dl { display: grid; grid-template-columns: 1.4fr 1fr 1fr; gap: 12px; margin: 13px 0 0; }
.format-preview dl > div { min-width: 0; }
.format-preview dt { color: var(--muted-2); font-size: .52rem; font-weight: 800; letter-spacing: .07em; text-transform: uppercase; }
.format-preview dd { margin: 6px 0 0; overflow: hidden; color: var(--ink); font-size: .7rem; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
.notification-settings-list { padding: 2px 20px; }
.notification-setting-row { display: flex; min-height: 72px; align-items: center; gap: 12px; padding: 15px 0; transition: opacity .16s ease; }
.notification-setting-row + .notification-setting-row { border-top: 1px solid var(--line); }
.notification-setting-row.muted, .notification-category-grid.muted { opacity: .52; }
.notification-setting-icon, .category-icon { display: grid; width: 37px; height: 37px; flex: 0 0 37px; place-items: center; border-radius: 10px; font-size: .72rem; font-weight: 900; }
.notification-setting-icon.blue, .category-icon.blue { color: var(--blue); background: var(--blue-soft); }
.notification-setting-icon.teal, .category-icon.teal { color: var(--teal); background: var(--teal-soft); }
.notification-setting-icon.amber, .category-icon.amber { color: var(--amber); background: var(--amber-soft); }
.notification-setting-row > div { min-width: 0; flex: 1; }
.notification-setting-row strong, .notification-category-card strong { color: var(--ink); font-size: .69rem; }
.notification-setting-row p, .notification-category-card p { margin-top: 4px; color: var(--muted); font-size: .61rem; line-height: 1.5; }
.preference-switch { position: relative; width: 39px; height: 22px; flex: 0 0 39px; border: 1px solid color-mix(in srgb, var(--muted) 25%, var(--line)); border-radius: 999px; background: var(--surface-hover); transition: background .16s ease, border-color .16s ease; }
.preference-switch span { position: absolute; top: 3px; left: 3px; width: 14px; height: 14px; border-radius: 50%; background: var(--muted); box-shadow: 0 1px 3px rgba(0, 0, 0, .18); transition: transform .16s ease, background .16s ease; }
.preference-switch.active { border-color: var(--blue); background: var(--blue); }
.preference-switch.active span { background: #fff; transform: translateX(17px); }
.preference-switch:disabled { cursor: not-allowed; }
.notification-interval { width: auto; min-width: 132px; flex: 0 0 auto; }
.notification-category-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; padding: 20px; transition: opacity .16s ease; }
.notification-category-card { position: relative; display: grid; min-height: 156px; grid-template-columns: 37px minmax(0, 1fr); align-content: start; gap: 12px; padding: 14px; border: 1px solid var(--line); border-radius: 11px; background: var(--surface-raised); }
.notification-category-card .preference-switch { position: absolute; top: 14px; right: 14px; }
.notification-category-card > div { grid-column: 1 / -1; }
.notification-category-card p { min-height: 37px; }
.notification-category-card small { display: inline-flex; margin-top: 10px; padding: 4px 7px; border-radius: 6px; color: var(--muted-2); background: var(--surface-hover); font-size: .52rem; font-weight: 800; letter-spacing: .05em; text-transform: uppercase; }
.notification-footnote { display: flex; align-items: center; gap: 10px; margin: 0 20px 20px; padding: 13px 14px; border: 1px solid color-mix(in srgb, var(--teal) 18%, var(--line)); border-radius: 10px; background: color-mix(in srgb, var(--teal) 5%, var(--surface-raised)); }
.notification-footnote > span { display: grid; width: 24px; height: 24px; flex: 0 0 24px; place-items: center; border-radius: 50%; color: var(--teal); background: var(--teal-soft); font-size: .62rem; font-weight: 900; }
.notification-footnote p { min-width: 0; flex: 1; color: var(--muted); font-size: .58rem; line-height: 1.45; }
.notification-footnote p strong { display: block; margin-bottom: 2px; color: var(--ink); font-size: .63rem; }
.notification-footnote > small { flex: 0 0 auto; color: var(--teal); font-size: .55rem; font-weight: 800; }
.profile-form { padding: 20px; }
.profile-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
.email-verification { display: flex; align-items: flex-start; gap: 11px; margin-top: 17px; padding: 14px; border: 1px solid color-mix(in srgb, var(--amber) 24%, var(--line)); border-radius: 10px; background: var(--amber-soft); }
.verification-icon { display: grid; width: 21px; height: 21px; flex: 0 0 21px; place-items: center; border-radius: 50%; color: #fff; background: var(--amber); font-family: serif; font-size: .7rem; font-weight: 800; }
.verification-copy { min-width: 0; flex: 1; }
.verification-copy > strong { color: var(--ink); font-size: .68rem; }
.verification-copy > p { margin: 3px 0 11px; color: var(--control-text); font-size: .61rem; line-height: 1.5; }
.verification-copy .field-label { max-width: 390px; }
.settings-password-field { position: relative; }
.settings-password-field input { padding-right: 40px; }
.settings-password-field button { position: absolute; top: 7px; right: 0; display: grid; width: 38px; height: 38px; place-items: center; color: var(--control-icon); background: transparent; }
.settings-password-field button:hover { color: var(--blue); }
.settings-password-field svg { width: 16px; height: 16px; }
.profile-form > .alert { margin-top: 14px; }
.profile-actions { display: flex; justify-content: flex-end; gap: 9px; margin-top: 19px; padding-top: 17px; border-top: 1px solid var(--line); }
.access-row { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 18px 20px 20px; }
.access-row div > span { color: var(--muted-2); font-size: .55rem; font-weight: 800; letter-spacing: .08em; text-transform: uppercase; }
.access-row div > strong { display: block; margin-top: 5px; color: var(--ink); font-size: .76rem; }
.access-row p { margin-top: 4px; color: var(--muted); font-size: .62rem; line-height: 1.5; }
.access-status { display: inline-flex; align-items: center; gap: 6px; color: var(--teal); font-size: .61rem; font-weight: 800; }
.access-status i { width: 7px; height: 7px; border-radius: 50%; background: currentColor; box-shadow: 0 0 0 3px color-mix(in srgb, var(--teal) 15%, transparent); }
.security-list { padding: 2px 20px; }
.security-row { display: flex; align-items: center; gap: 12px; padding: 17px 0; }
.security-row + .security-row { border-top: 1px solid var(--line); }
.security-row-icon { display: grid; width: 35px; height: 35px; flex: 0 0 35px; place-items: center; border-radius: 9px; color: var(--control-text); background: var(--surface-hover); }
.security-row-icon.danger { color: var(--red); background: var(--red-soft); }
.security-row-icon svg { width: 18px; height: 18px; }
.security-row > div { min-width: 0; flex: 1; }
.security-row strong { color: var(--ink); font-size: .7rem; }
.security-row p { margin-top: 4px; color: var(--muted); font-size: .61rem; line-height: 1.5; }
.security-row button { flex: 0 0 auto; }
.danger-button { display: inline-flex; min-height: 38px; align-items: center; justify-content: center; gap: 7px; padding: 0 14px; border: 1px solid color-mix(in srgb, var(--red) 24%, var(--line)); border-radius: 9px; color: var(--red); background: var(--red-soft); font-size: .67rem; font-weight: 800; }
.danger-button:hover:not(:disabled) { border-color: var(--red); }
.danger-button.solid { border-color: var(--red); color: #fff; background: var(--red); }
.logout-confirmation { display: flex; align-items: center; justify-content: space-between; gap: 18px; margin: 0 0 18px; padding: 14px; border: 1px solid color-mix(in srgb, var(--red) 24%, var(--line)); border-radius: 10px; background: var(--red-soft); }
.logout-confirmation strong { color: var(--red); font-size: .68rem; }
.logout-confirmation p { margin-top: 4px; color: var(--control-text); font-size: .61rem; line-height: 1.5; }
.logout-confirmation-actions { display: flex; flex: 0 0 auto; gap: 8px; }
.danger-spinner { width: 14px; height: 14px; border: 2px solid rgba(255, 255, 255, .42); border-top-color: #fff; border-radius: 50%; animation: danger-spin .7s linear infinite; }
.security-error { margin: 0 20px 18px; }
.email-password-enter-active, .email-password-leave-active { transition: opacity .18s ease, transform .18s ease; }
.email-password-enter-from, .email-password-leave-to { opacity: 0; transform: translateY(-5px); }
@keyframes danger-spin { to { transform: rotate(360deg); } }

@media (max-width: 830px) {
  .settings-layout { grid-template-columns: 1fr; }
  .settings-nav { position: static; }
  .notification-category-grid { grid-template-columns: 1fr; }
  .notification-category-card { min-height: 126px; }
  .notification-category-card p { min-height: 0; }
}
@media (max-width: 600px) {
  .profile-fields, .inventory-fields, .company-fields { grid-template-columns: 1fr; }
  .account-summary, .display-summary, .notification-summary, .inventory-summary { align-items: flex-start; flex-wrap: wrap; }
  .role-badge { margin-left: 72px; }
  .display-summary > div:nth-child(2), .notification-summary > div:nth-child(2), .inventory-summary > div:nth-child(2) { flex: 0 0 calc(100% - 72px); }
  .autosave-badge, .notification-status-badge, .admin-badge { margin-left: 72px; }
  .theme-options, .regional-grid, .format-preview dl, .permissions-grid { grid-template-columns: 1fr; }
  .language-options { grid-template-columns: 1fr; }
  .theme-options button { grid-template-columns: 72px 1fr; }
  .format-preview-head { align-items: flex-start; flex-direction: column; }
  .notification-setting-row { align-items: flex-start; flex-wrap: wrap; }
  .notification-setting-row .preference-switch, .notification-setting-row .notification-interval { margin-left: 49px; }
  .notification-footnote { align-items: flex-start; flex-wrap: wrap; }
  .notification-footnote > small { width: 100%; margin-left: 34px; }
  .inventory-policy-row, .inventory-threshold-row { align-items: flex-start; flex-wrap: wrap; }
  .inventory-policy-row .preference-switch, .inventory-threshold-row .inventory-number-input { width: calc(100% - 49px); margin-left: 49px; flex-basis: auto; }
  .inventory-actions { align-items: stretch; flex-direction: column; }
  .inventory-actions > div, .inventory-actions button { flex: 1; }
  .user-fields { grid-template-columns: 1fr; }
  .users-summary .users-add-button { width: calc(100% - 72px); margin-left: 72px; }
  .users-toolbar { align-items: stretch; flex-direction: column; }
  .users-toolbar .search-input { max-width: none; }
  .users-toolbar .toolbar-actions { flex-wrap: wrap; }
  .users-toolbar .toolbar-actions > * { flex: 1; }
  .security-row, .logout-confirmation { align-items: flex-start; flex-wrap: wrap; }
  .security-row button { width: 100%; margin-left: 47px; }
  .logout-confirmation-actions { width: 100%; }
  .logout-confirmation-actions button { flex: 1; }
  .profile-actions button { flex: 1; }
}
@media (prefers-reduced-motion: reduce) {
  .danger-spinner { animation: none; }
  .email-password-enter-active, .email-password-leave-active { transition: none; }
}
</style>
