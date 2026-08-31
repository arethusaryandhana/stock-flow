<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '../infrastructure/api'
import { useAuthStore } from '../stores/auth'
import { useI18n } from '../i18n'
import ThemeSwitcher from '../components/ThemeSwitcher.vue'

type AuthMode = 'login' | 'forgot' | 'reset'

const email = ref('admin@stockflow.local')
const password = ref('StockFlow123!')
const forgotEmail = ref('admin@stockflow.local')
const resetToken = ref(new URLSearchParams(window.location.search).get('resetToken') ?? '')
const newPassword = ref('')
const confirmPassword = ref('')
const mode = ref<AuthMode>(resetToken.value ? 'reset' : 'login')
const showPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const error = ref('')
const feedback = ref('')
const busy = ref(false)
const auth = useAuthStore()
const router = useRouter()
const { language, t, toggleLanguage } = useI18n()

function clearMessages() {
  error.value = ''
  feedback.value = ''
}

function updateUrlWithoutToken() {
  window.history.replaceState({}, '', '/login')
}

function showLogin() {
  mode.value = 'login'
  resetToken.value = ''
  clearMessages()
  updateUrlWithoutToken()
}

function showForgot() {
  mode.value = 'forgot'
  clearMessages()
}

async function submitLogin() {
  busy.value = true; clearMessages()
  try { await auth.login(email.value.trim(), password.value); router.push('/') } catch (requestError) { error.value = (requestError as Error).message } finally { busy.value = false }
}

async function submitForgot() {
  busy.value = true; clearMessages()
  try {
    const { data } = await api.post<{ message: string; resetToken?: string }>('/auth/forgot-password', { email: forgotEmail.value.trim() })
    feedback.value = data.message
    if (data.resetToken) {
      resetToken.value = data.resetToken
      mode.value = 'reset'
      window.history.replaceState({}, '', `/login?resetToken=${encodeURIComponent(data.resetToken)}`)
    }
  } catch (requestError) { error.value = (requestError as Error).message } finally { busy.value = false }
}

async function submitReset() {
  clearMessages()
  if (newPassword.value.length < 8) { error.value = t('login.passwordMinLength'); return }
  if (newPassword.value !== confirmPassword.value) { error.value = t('login.passwordMismatch'); return }
  busy.value = true
  try {
    const { data } = await api.post<{ message: string }>('/auth/reset-password', { token: resetToken.value, newPassword: newPassword.value })
    feedback.value = data.message
    mode.value = 'login'
    password.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
    updateUrlWithoutToken()
  } catch (requestError) { error.value = (requestError as Error).message } finally { busy.value = false }
}
</script>

<template>
  <main class="login">
    <section class="login-visual">
      <div class="login-preferences">
        <ThemeSwitcher />
        <button class="language-switcher login-language" type="button" :aria-label="language === 'id' ? t('app.switchToEnglish') : t('app.switchToIndonesian')" @click="toggleLanguage"><span :class="{ active: language === 'en' }">EN</span><span :class="{ active: language === 'id' }">ID</span></button>
      </div>
      <div class="login-brand"><img class="brand-logo" src="/stockflow-logo.svg?v=20260827" alt="" aria-hidden="true"><div><strong>StockFlow</strong><small>Inventory OS</small></div></div>
      <div class="login-copy"><p class="eyebrow">{{ t('login.operationsEyebrow') }}</p><h2 v-html="t('login.visualTitle')" /><p>{{ t('login.visualDescription') }}</p><div class="login-points"><span>{{ t('login.realTime') }}</span><span>{{ t('login.auditReady') }}</span><span>{{ t('login.multiWorkspace') }}</span></div></div>
      <p class="login-footer">{{ t('login.footer') }}</p>
    </section>
    <section class="login-form-side">
      <div class="login-card">
        <template v-if="mode === 'login'">
          <p class="eyebrow">{{ t('login.welcome') }}</p><h1>{{ t('login.title') }}</h1><p class="subtitle">{{ t('login.subtitle') }}</p>
          <form class="login-form" @submit.prevent="submitLogin">
            <label class="login-label">{{ t('login.workEmail') }}<input v-model="email" type="email" autocomplete="username" required></label>
            <label class="login-label">{{ t('login.password') }}<div class="password-field"><input v-model="password" :type="showPassword ? 'text' : 'password'" autocomplete="current-password" required><button class="password-toggle" type="button" :aria-label="showPassword ? t('login.hidePassword') : t('login.showPassword')" :aria-pressed="showPassword" @click="showPassword = !showPassword"><svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showPassword" d="m3 3 18 18" /></svg></button></div></label>
            <div class="login-options"><label class="checkbox"><input type="checkbox"> {{ t('login.rememberMe') }}</label><button class="text-link" type="button" @click="showForgot">{{ t('login.forgotPassword') }}</button></div>
            <p v-if="error" class="alert">{{ error }}</p><p v-if="feedback" class="form-feedback">{{ feedback }}</p>
            <button class="primary login-submit" :disabled="busy">{{ busy ? t('login.checkingAccess') : t('login.submit') }}</button>
          </form>
          <div class="demo-note"><strong>{{ t('login.demoWorkspace') }}</strong> {{ t('login.demoDescription') }}</div>
        </template>

        <template v-else-if="mode === 'forgot'">
          <button class="back-link" type="button" @click="showLogin">{{ t('login.backToLogin') }}</button><p class="eyebrow flow-eyebrow">{{ t('login.recovery') }}</p><h1>{{ t('login.resetTitle') }}</h1><p class="subtitle">{{ t('login.resetSubtitle') }}</p>
          <form class="login-form" @submit.prevent="submitForgot">
            <label class="login-label">{{ t('login.workEmail') }}<input v-model="forgotEmail" type="email" autocomplete="email" required placeholder="nama@perusahaan.com"></label>
            <p v-if="error" class="alert">{{ error }}</p><p v-if="feedback" class="form-feedback">{{ feedback }}</p>
            <button class="primary login-submit" :disabled="busy">{{ busy ? t('login.preparingLink') : t('login.sendReset') }}</button>
          </form>
          <div class="demo-note"><strong>{{ t('login.localDemo') }}</strong> {{ t('login.localDemoDescription') }}</div>
        </template>

        <template v-else>
          <button class="back-link" type="button" @click="showLogin">{{ t('login.backToLogin') }}</button><p class="eyebrow flow-eyebrow">{{ t('login.recovery') }}</p><h1>{{ t('login.newPasswordTitle') }}</h1><p class="subtitle">{{ t('login.newPasswordSubtitle') }}</p>
          <form class="login-form" @submit.prevent="submitReset">
            <label class="login-label">{{ t('login.newPassword') }}<div class="password-field"><input v-model="newPassword" :type="showNewPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" required><button class="password-toggle" type="button" :aria-label="showNewPassword ? t('login.hideNewPassword') : t('login.showNewPassword')" :aria-pressed="showNewPassword" @click="showNewPassword = !showNewPassword"><svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showNewPassword" d="m3 3 18 18" /></svg></button></div></label>
            <label class="login-label">{{ t('login.confirmPassword') }}<div class="password-field"><input v-model="confirmPassword" :type="showConfirmPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" required><button class="password-toggle" type="button" :aria-label="showConfirmPassword ? t('login.hideConfirmPassword') : t('login.showConfirmPassword')" :aria-pressed="showConfirmPassword" @click="showConfirmPassword = !showConfirmPassword"><svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showConfirmPassword" d="m3 3 18 18" /></svg></button></div></label>
            <p v-if="error" class="alert">{{ error }}</p><p v-if="feedback" class="form-feedback">{{ feedback }}</p>
            <button class="primary login-submit" :disabled="busy">{{ busy ? t('login.savingPassword') : t('login.saveNewPassword') }}</button>
          </form>
        </template>
      </div>
    </section>
  </main>
</template>
