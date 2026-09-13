<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ChangePasswordModal from '../components/ChangePasswordModal.vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'

type SessionProfile = { fullName: string; email: string; role: string }

const auth = useAuthStore()
const toast = useToastStore()
const { t } = useI18n()

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

onMounted(loadProfile)
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
        <button class="settings-nav-item active" type="button" aria-current="page">
          <span class="settings-nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="3.5" /><path d="M5 20c.7-4 3-6 7-6s6.3 2 7 6" /></svg>
          </span>
          <span><strong>{{ t('settings.accountSecurity') }}</strong><small>{{ t('settings.accountSecurityHint') }}</small></span>
        </button>
      </aside>

      <main class="settings-content">
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
    </div>

    <ChangePasswordModal v-if="changePasswordOpen" @close="changePasswordOpen = false" />
  </div>
</template>

<style scoped>
.settings-page { max-width: 1260px; }
.settings-layout { display: grid; grid-template-columns: 235px minmax(0, 1fr); align-items: start; gap: 16px; }
.settings-nav { position: sticky; top: 94px; padding: 7px; }
.settings-nav-item { display: flex; width: 100%; align-items: center; gap: 10px; padding: 11px; border-radius: 9px; color: var(--control-text); background: transparent; text-align: left; }
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
.settings-card { overflow: hidden; }
.settings-card-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; padding: 20px 20px 17px; border-bottom: 1px solid var(--line); }
.settings-card-head p { max-width: 600px; margin-top: 5px; color: var(--muted); font-size: .66rem; line-height: 1.5; }
.settings-card-icon { display: grid; width: 34px; height: 34px; flex: 0 0 34px; place-items: center; border-radius: 9px; }
.settings-card-icon svg { width: 18px; height: 18px; }
.settings-card-icon.blue { color: var(--blue); background: var(--blue-soft); }
.settings-card-icon.teal { color: var(--teal); background: var(--teal-soft); }
.settings-card-icon.amber { color: var(--amber); background: var(--amber-soft); }
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
}
@media (max-width: 600px) {
  .profile-fields { grid-template-columns: 1fr; }
  .account-summary { align-items: flex-start; flex-wrap: wrap; }
  .role-badge { margin-left: 72px; }
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
